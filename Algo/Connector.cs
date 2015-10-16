using System.Windows;

namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Algo.Slippage;
	using StockSharp.Localization;

	using Wintellect.PowerCollections;

	/// <summary>
	/// Класс для создания подключений к торговым системам.
	/// </summary>
	public partial class Connector : BaseLogReceiver, IConnector
	{
		private static readonly MemoryStatisticsValue<Trade> _tradeStat = new MemoryStatisticsValue<Trade>(LocalizedStrings.Ticks);
		private static readonly MemoryStatisticsValue<Connector> _connectorStat = new MemoryStatisticsValue<Connector>(LocalizedStrings.Str1093);
		private static readonly MemoryStatisticsValue<Message> _messageStat = new MemoryStatisticsValue<Message>(LocalizedStrings.Str1094);

		static Connector()
		{
			MemoryStatistics.Instance.Values.Add(_tradeStat);
			MemoryStatistics.Instance.Values.Add(_connectorStat);
			MemoryStatistics.Instance.Values.Add(_messageStat);
		}

		private sealed class FilteredMarketDepthInfo
		{
			private readonly Dictionary<Tuple<Sides, decimal>, Dictionary<long, decimal>> _executions = new Dictionary<Tuple<Sides, decimal>, Dictionary<long, decimal>>();
			private readonly Dictionary<Tuple<Sides, decimal>, decimal> _ownVolumes = new Dictionary<Tuple<Sides, decimal>, decimal>();

			private readonly MarketDepth _depth;

			private QuoteChangeMessage _quote;
			private bool _needUpdate;

			public FilteredMarketDepthInfo(MarketDepth depth)
			{
				if (depth == null)
					throw new ArgumentNullException("depth");

				_depth = depth;
			}

			public void Init(MarketDepth source, IEnumerable<Order> currentOrders)
			{
				if (source == null)
					throw new ArgumentNullException("source");

				if (currentOrders == null)
					throw new ArgumentNullException("currentOrders");

				currentOrders.Select(o => o.ToMessage()).ForEach(Process);

				_depth.Update(Filter(source.Bids), Filter(source.Asks), true, source.LastChangeTime);
			}

			private IEnumerable<Quote> Filter(IEnumerable<Quote> quotes)
			{
				return quotes
					.Select(quote =>
					{
						var res = quote.Clone();
						var key = Tuple.Create(res.OrderDirection, res.Price);

						var own = _ownVolumes.TryGetValue2(key);
						if (own != null)
							res.Volume -= own.Value;

						return res.Volume <= 0 ? null : res;
					})
					.Where(q => q != null);
			}

			public void Process(QuoteChangeMessage message)
			{
				if (message == null)
					throw new ArgumentNullException("message");

				_quote = new QuoteChangeMessage
				{
					LocalTime = message.LocalTime,
					ServerTime = message.ServerTime,
					ExtensionInfo = message.ExtensionInfo,
					Bids = message.Bids,
					Asks = message.Asks,
					IsSorted = message.IsSorted,
				};
				_needUpdate = true;
			}

			public void Process(ExecutionMessage message)
			{
				if (message.ExecutionType != ExecutionTypes.Order)
					return;

				var key = Tuple.Create(message.Side, message.Price);

				switch (message.OrderState)
				{
					case OrderStates.Done:
					case OrderStates.Failed:
					{
						var items = _executions.TryGetValue(key);

						if (items == null)
							break;

						items.Remove(message.OriginalTransactionId);

						if (items.Count == 0)
							_executions.Remove(key);

						break;
					}

					case OrderStates.Active:
					{
						if (message.Balance != null)
							_executions.SafeAdd(key, k => new Dictionary<long, decimal>())[message.OriginalTransactionId] = message.Balance.Value;

						break;
					}
				}

				if (_executions.ContainsKey(key))
					_ownVolumes[key] = _executions[key].Sum(o => o.Value);
				else
					_ownVolumes.Remove(key);
			}

			public MarketDepth GetDepth()
			{
				if (!_needUpdate)
					return _depth;

				_needUpdate = false;
				_depth.Update(Filter(_quote.Bids.Select(c => c.ToQuote(_depth.Security))), Filter(_quote.Asks.Select(c => c.ToQuote(_depth.Security))), _quote.IsSorted, _quote.ServerTime);
				_depth.LocalTime = _quote.LocalTime;

				return _depth;
			}
		}

		private class MarketDepthInfo : RefTriple<MarketDepth, IEnumerable<QuoteChange>, IEnumerable<QuoteChange>>
		{
			public MarketDepthInfo(MarketDepth depth)
				: base(depth, Enumerable.Empty<QuoteChange>(), Enumerable.Empty<QuoteChange>())
			{
			}

			public bool HasChanges
			{
				get { return Second != null; }
			}
		}

		private readonly EntityCache _entityCache = new EntityCache();

		private readonly SynchronizedDictionary<Security, MarketDepthInfo> _marketDepths = new SynchronizedDictionary<Security, MarketDepthInfo>();
		private readonly SynchronizedDictionary<Security, FilteredMarketDepthInfo> _filteredMarketDepths = new SynchronizedDictionary<Security, FilteredMarketDepthInfo>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonOrderedByIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<long, List<ExecutionMessage>> _nonOrderedByTransactionIdMyTrades = new Dictionary<long, List<ExecutionMessage>>();
		private readonly Dictionary<string, List<ExecutionMessage>> _nonOrderedByStringIdMyTrades = new Dictionary<string, List<ExecutionMessage>>();
		private readonly MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>> _orderStopOrderAssociations = new MultiDictionary<Tuple<long?, string>, RefPair<Order, Action<Order, Order>>>(false);

		private readonly Dictionary<object, Security> _nativeIdSecurities = new Dictionary<object, Security>();
		private readonly Dictionary<SecurityId, List<Message>> _suspendedSecurityMessages = new Dictionary<SecurityId, List<Message>>();
		private readonly object _suspendSync = new object();
		private readonly List<Security> _lookupResult = new List<Security>();
		private readonly SynchronizedQueue<SecurityLookupMessage> _lookupQueue = new SynchronizedQueue<SecurityLookupMessage>();
		private readonly SynchronizedDictionary<long, SecurityLookupMessage> _securityLookups = new SynchronizedDictionary<long, SecurityLookupMessage>();
		private readonly SynchronizedDictionary<long, PortfolioLookupMessage> _portfolioLookups = new SynchronizedDictionary<long, PortfolioLookupMessage>();
		
		private readonly SubscriptionManager _subscriptionManager;

		private readonly SynchronizedDictionary<ExchangeBoard, SessionStates> _sessionStates = new SynchronizedDictionary<ExchangeBoard, SessionStates>();
		private readonly SynchronizedDictionary<Security, object[]> _securityValues = new SynchronizedDictionary<Security, object[]>();

		private readonly ISecurityProvider _securityProvider;

		private readonly SyncObject _marketTimerSync = new SyncObject();
		private Timer _marketTimer;
		private readonly TimeMessage _marketTimeMessage = new TimeMessage();
		private bool _isMarketTimeHandled;

		private bool _isDisposing;

		/// <summary>
		/// Создать <see cref="Connector"/>.
		/// </summary>
		public Connector()
		{
			ReConnectionSettings = new ReConnectionSettings();

			_subscriptionManager = new SubscriptionManager(this);

			UpdateSecurityLastQuotes = UpdateSecurityByLevel1 = true;

			CreateDepthFromLevel1 = true;

			_connectorStat.Add(this);

			_securityProvider = new ConnectorSecurityProvider(this);
			SlippageManager = new SlippageManager();

			OutMessageChannel = new InMemoryMessageChannel("Connector Out", RaiseError);

			Adapter = new BasketMessageAdapter(new MillisecondIncrementalIdGenerator());
		}

		/// <summary>
		/// Настройки контроля подключения <see cref="IConnector"/> к торговой системе.
		/// </summary>
		public ReConnectionSettings ReConnectionSettings { get; private set; }

		private IEntityFactory _entityFactory = Algo.EntityFactory.Instance;

		/// <summary>
		/// Фабрика бизнес-сущностей (<see cref="Security"/>, <see cref="Order"/> и т.д.).
		/// </summary>
		public IEntityFactory EntityFactory
		{
			get { return _entityFactory; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_entityFactory = value;
			}
		}

		/// <summary>
		/// Количество тиковых сделок для хранения. 
		/// По умолчанию равно 100000. Если значение установлено в -1, то сделки не будут удаляться.
		/// Если значение установлено в 0, то сделки не будут сохраняться.
		/// </summary>
		public int TradesKeepCount
		{
			get { return _entityCache.TradesKeepCount; }
			set { _entityCache.TradesKeepCount = value; }
		}

		/// <summary>
		/// Количество заявок для хранения. 
		/// По умолчанию равно 1000. Если значение установлено в -1, то заявки не будут удаляться.
		/// Если значение установлено в 0, то заявки не будут сохраняться.
		/// </summary>
		public int OrdersKeepCount
		{
			get { return _entityCache.OrdersKeepCount; }
			set { _entityCache.OrdersKeepCount = value; }
		}

		/// <summary>
		/// Генератор идентификаторов транзакций.
		/// </summary>
		public IdGenerator TransactionIdGenerator
		{
			get { return Adapter.TransactionIdGenerator; }
			set { Adapter.TransactionIdGenerator = value; }
		}

		private SecurityIdGenerator _securityIdGenerator = new SecurityIdGenerator();

		/// <summary>
		/// Генератор идентификаторов инструментов <see cref="Security.Id"/>.
		/// </summary>
		public SecurityIdGenerator SecurityIdGenerator
		{
			get { return _securityIdGenerator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_securityIdGenerator = value;
			}
		}

		private readonly CachedSynchronizedSet<ExchangeBoard> _exchangeBoards = new CachedSynchronizedSet<ExchangeBoard>();

		/// <summary>
		/// Список всех биржевых площадок, для которых загружены инструменты <see cref="IConnector.Securities"/>.
		/// </summary>
		public IEnumerable<ExchangeBoard> ExchangeBoards
		{
			get { return _exchangeBoards.Cache; }
		}

		private readonly CachedSynchronizedDictionary<string, Security> _securities = new CachedSynchronizedDictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Список всех загруженных инструментов.
		/// Вызывать необходимо после того, как пришло событие <see cref="IConnector.NewSecurities" />. Иначе будет возвращено пустое множество.
		/// </summary>
		public virtual IEnumerable<Security> Securities
		{
			get { return _securities.CachedValues; }
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		/// <returns>Найденные инструменты.</returns>
		public virtual IEnumerable<Security> Lookup(Security criteria)
		{
			return _securityProvider.Lookup(criteria);
		}

		/// <summary>
		/// Получить внутренний идентификатор торговой системы.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Внутренний идентификатор торговой системы.</returns>
		public object GetNativeId(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return _nativeIdSecurities.LastOrDefault(p => p.Value == security).Key;
		}

		private DateTimeOffset _currentTime;

		/// <summary>
		/// Текущее время, которое будет передано в <see cref="LogMessage.Time"/>.
		/// </summary>
		public override DateTimeOffset CurrentTime
		{
			get { return _currentTime; }
		}

		/// <summary>
		/// Получить состояние сессии для заданной площадки.
		/// </summary>
		/// <param name="board">Биржевая площадка электронных торгов.</param>
		/// <returns>Состояние сессии. Если информация о состоянии сессии отсутствует, то будет возвращено <see langword="null"/>.</returns>
		public SessionStates? GetSessionState(ExchangeBoard board)
		{
			return _sessionStates.TryGetValue2(board);
		}

		/// <summary>
		/// Получить все заявки.
		/// </summary>
		public IEnumerable<Order> Orders
		{
			get { return _entityCache.Orders; }
		}

		/// <summary>
		/// Получить все стоп-заявки.
		/// </summary>
		public IEnumerable<Order> StopOrders
		{
			get { return Orders.Where(o => o.Type == OrderTypes.Conditional); }
		}

		private readonly SynchronizedList<OrderFail> _orderRegisterFails = new SynchronizedList<OrderFail>();

		/// <summary>
		/// Получить все ошибки при регистрации заявок.
		/// </summary>
		public IEnumerable<OrderFail> OrderRegisterFails
		{
			get { return _orderRegisterFails.SyncGet(c => c.ToArray()); }
		}

		private readonly SynchronizedList<OrderFail> _orderCancelFails = new SynchronizedList<OrderFail>();

		/// <summary>
		/// Получить все ошибки при снятии заявок.
		/// </summary>
		public IEnumerable<OrderFail> OrderCancelFails
		{
			get { return _orderCancelFails.SyncGet(c => c.ToArray()); }
		}

		/// <summary>
		/// Получить все сделки.
		/// </summary>
		public IEnumerable<Trade> Trades
		{
			get { return _entityCache.Trades; }
		}

		/// <summary>
		/// Получить все собственные сделки.
		/// </summary>
		public IEnumerable<MyTrade> MyTrades
		{
			get { return _entityCache.MyTrades; }
		}

		/// <summary>
		/// Получить все портфели.
		/// </summary>
		public virtual IEnumerable<Portfolio> Portfolios
		{
			get { return _entityCache.Portfolios; }
		}

		private readonly CachedSynchronizedDictionary<Tuple<Portfolio, Security, string, TPlusLimits?>, Position> _positions = new CachedSynchronizedDictionary<Tuple<Portfolio, Security, string, TPlusLimits?>, Position>();

		/// <summary>
		/// Получить все позиции.
		/// </summary>
		public IEnumerable<Position> Positions
		{
			get { return _positions.CachedValues; }
		}

		/// <summary>
		/// Все новости.
		/// </summary>
		public IEnumerable<News> News
		{
			get { return _entityCache.News; }
		}

		/// <summary>
		/// Производить расчет данных на основе <see cref="ManagedMessageAdapter"/>. По-умолчанию включено.
		/// </summary>
		public virtual bool CalculateMessages
		{
			get { return true; }
		}

		/// <summary>
		/// Менеджер расчета проскальзывания.
		/// </summary>
		public ISlippageManager SlippageManager { get; private set; }

		/// <summary>
		/// Состояние соединения.
		/// </summary>
		public ConnectionStates ConnectionState { get; private set; }

		private bool _isSupportAtomicReRegister = true;

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции. По-умолчанию включено.
		/// </summary>
		public virtual bool IsSupportAtomicReRegister
		{
			get { return _isSupportAtomicReRegister; }
			protected set { _isSupportAtomicReRegister = value; }
		}

		/// <summary>
		/// Использовать лог заявок (orders log) для создания стаканов. По-умолчанию выключено.
		/// </summary>
		public virtual bool CreateDepthFromOrdersLog { get; set; }

		/// <summary>
		/// Использовать лог заявок (orders log) для создания тиковых сделок. По-умолчанию выключено.
		/// </summary>
		public virtual bool CreateTradesFromOrdersLog { get; set; }

		/// <summary>
		/// Обновлять <see cref="Security.LastTrade"/>, <see cref="Security.BestBid"/>, <see cref="Security.BestAsk"/> на каждом обновлении стакана и/или сделок.
		/// По умолчанию включено.
		/// </summary>
		public bool UpdateSecurityLastQuotes { get; set; }

		/// <summary>
		/// Обновлять поля <see cref="Security"/> при появлении сообщения <see cref="Level1ChangeMessage"/>.
		/// По умолчанию включено.
		/// </summary>
		public bool UpdateSecurityByLevel1 { get; set; }

		/// <summary>
		/// Обновлять стакан для инструмента при появлении сообщения <see cref="Level1ChangeMessage"/>.
		/// По умолчанию включено.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str200Key)]
		[DescriptionLoc(LocalizedStrings.Str201Key)]
		public bool CreateDepthFromLevel1 { get; set; }

		/// <summary>
		/// Создавать объединенный инструмент для инструментов с разных торговых площадок.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str197Key)]
		[DescriptionLoc(LocalizedStrings.Str198Key)]
		public bool CreateAssociatedSecurity { get; set; }

		/// <summary>
		/// Число ошибок, переданное через событие <see cref="Error"/>.
		/// </summary>
		public int ErrorCount { get; private set; }

		///// <summary>
		///// Временной сдвиг от текущего времени. Используется в случае, если сервер брокера самостоятельно
		///// указывает сдвиг во времени.
		///// </summary>
		//public TimeSpan? TimeShift { get; private set; }

		private TimeSpan _marketTimeChangedInterval = TimeSpan.FromMilliseconds(10);

		/// <summary>
		/// Интервал генерации сообщения <see cref="TimeMessage"/>. По-умолчанию равно 10 миллисекундам.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str186Key)]
		[DisplayNameLoc(LocalizedStrings.TimeIntervalKey)]
		[DescriptionLoc(LocalizedStrings.Str195Key)]
		public virtual TimeSpan MarketTimeChangedInterval
		{
			get { return _marketTimeChangedInterval; }
			set
			{
				if (value <= TimeSpan.Zero)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str196);

				_marketTimeChangedInterval = value;
			}
		}

		private void TryOpenChannel()
		{
			if (OutMessageChannel.IsOpened)
				return;

			OutMessageChannel.Open();
		}

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		public void Connect()
		{
			this.AddInfoLog("Connect");

			try
			{
				if (ConnectionState != ConnectionStates.Disconnected && ConnectionState != ConnectionStates.Failed)
				{
					this.AddWarningLog(LocalizedStrings.Str1095Params, ConnectionState);
					return;
				}

				ConnectionState = ConnectionStates.Connecting;

				foreach (var adapter in Adapter.InnerAdapters.SortedAdapters)
				{
					_adapterStates[adapter] = ConnectionStates.Connecting;
				}

				TryOpenChannel();

				StartMarketTimer();
				OnConnect();
			}
			catch (Exception ex)
			{
				RaiseConnectionError(ex);
			}
		}

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		protected virtual void OnConnect()
		{
			SendInMessage(new ConnectMessage());
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		public void Disconnect()
		{
			this.AddInfoLog("Disconnect");

			if (ConnectionState != ConnectionStates.Connected)
			{
				this.AddWarningLog(LocalizedStrings.Str1096Params, ConnectionState);
				return;
			}

			ConnectionState = ConnectionStates.Disconnecting;

			foreach (var adapter in Adapter.InnerAdapters.SortedAdapters)
			{
				var prevState = _adapterStates.TryGetValue2(adapter);

				if (prevState != ConnectionStates.Failed)
					_adapterStates[adapter] = ConnectionStates.Disconnecting;
			}

			_subscriptionManager.Stop();

			try
			{
				OnDisconnect();
			}
			catch (Exception ex)
			{
				RaiseConnectionError(ex);
			}
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		protected virtual void OnDisconnect()
		{
			SendInMessage(new DisconnectMessage());
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Инструмент, поля которого будут использоваться в качестве фильтра.</param>
		public void LookupSecurities(Security criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			var boardCode = criteria.Board != null ? criteria.Board.Code : string.Empty;
			var securityCode = criteria.Code ?? string.Empty;

			if (!criteria.Id.IsEmpty())
			{
				var info = SecurityIdGenerator.Split(criteria.Id);

				if (boardCode.IsEmpty())
					boardCode = GetBoardCode(info.Item2);

				if (securityCode.IsEmpty())
					securityCode = info.Item1;
			}

			var message = new SecurityLookupMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				//LocalTime = CurrentTime,
				SecurityId = criteria.ExternalId.ToSecurityId(securityCode, boardCode, criteria.Type),
				Name = criteria.Name,
				Class = criteria.Class,
				SecurityType = criteria.Type,
				ExpiryDate = criteria.ExpiryDate,
				ShortName = criteria.ShortName,
				VolumeStep = criteria.VolumeStep,
				Multiplier = criteria.Multiplier,
				PriceStep = criteria.PriceStep,
				Decimals = criteria.Decimals,
				Currency = criteria.Currency,
				SettlementDate = criteria.SettlementDate,
				OptionType = criteria.OptionType,
				Strike = criteria.Strike,
				BinaryOptionType = criteria.BinaryOptionType,
				UnderlyingSecurityCode = criteria.UnderlyingSecurityId.IsEmpty() ? null : SecurityIdGenerator.Split(criteria.UnderlyingSecurityId).Item1
			};

			LookupSecurities(message);
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		public virtual void LookupSecurities(SecurityLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			//если для критерия указаны код биржи и код инструмента, то сначала смотрим нет ли такого инструмента
			if (!NeedLookupSecurities(criteria.SecurityId))
			{
				_securityLookups.Add(criteria.TransactionId, (SecurityLookupMessage)criteria.Clone());
				SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = criteria.TransactionId });
				return;
			}

			lock (_lookupQueue.SyncRoot)
			{
				_lookupQueue.Enqueue(criteria);

				if (_lookupQueue.Count == 1)
					SendInMessage(criteria);
			}
		}

		private bool NeedLookupSecurities(SecurityId securityId)
		{
			if (securityId.SecurityCode.IsEmpty() || securityId.BoardCode.IsEmpty())
				return true;

			var id = SecurityIdGenerator.GenerateId(securityId.SecurityCode, securityId.BoardCode);

			var security = Securities.FirstOrDefault(s => s.Id.CompareIgnoreCase(id));

			return security == null;
		}

		/// <summary>
		/// Найти портфели, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные портфели будут переданы через событие <see cref="IConnector.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">Портфель, поля которого будут использоваться в качестве фильтра.</param>
		public virtual void LookupPortfolios(Portfolio criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException("criteria");

			var msg = new PortfolioLookupMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				BoardCode = criteria.Board == null ? null : criteria.Board.Code,
				Currency = criteria.Currency,
				PortfolioName = criteria.Name,
			};

			_portfolioLookups.Add(msg.TransactionId, msg);

			SendInMessage(msg);
		}

		/// <summary>
		/// Получить позицию по портфелю и инструменту.
		/// </summary>
		/// <param name="portfolio">Портфель, по которому нужно найти позицию.</param>
		/// <param name="security">Инструмент, по которому нужно найти позицию.</param>
		/// <param name="depoName">Название депозитария, где находится физически ценная бумага.
		/// По-умолчанию передается пустая строка, что означает суммарную позицию по всем депозитариям.</param>
		/// <returns>Позиция.</returns>
		public Position GetPosition(Portfolio portfolio, Security security, string depoName = "")
		{
			return GetPosition(portfolio, security, depoName, TPlusLimits.T0, string.Empty);
		}

		private Position GetPosition(Portfolio portfolio, Security security, string depoName, TPlusLimits? limitType, string description)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			if (security == null)
				throw new ArgumentNullException("security");

			Position position;

			var isNew = false;

			lock (_positions.SyncRoot)
			{
				if (depoName == null)
					depoName = string.Empty;

				var key = Tuple.Create(portfolio, security, depoName, limitType);

				if (!_positions.TryGetValue(key, out position))
				{
					isNew = true;

					position = EntityFactory.CreatePosition(portfolio, security);
					position.DepoName = depoName;
					position.LimitType = limitType;
					position.Description = description;
					_positions.Add(key, position);
				}
			}

			if (isNew)
				RaiseNewPositions(new[] { position });

			return position;
		}

		/// <summary>
		/// Получить стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Стакан котировок.</returns>
		public MarketDepth GetMarketDepth(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			MarketDepthInfo info;

			var isNew = false;

			lock (_marketDepths.SyncRoot)
			{
				if (!_marketDepths.TryGetValue(security, out info))
				{
					isNew = true;

					info = new MarketDepthInfo(EntityFactory.CreateMarketDepth(security));

					// стакан из лога заявок бесконечен
					if (CreateDepthFromOrdersLog)
						info.First.MaxDepth = int.MaxValue;

					_marketDepths.Add(security, info);
				}
				else
				{
					if (info.HasChanges)
					{
						new QuoteChangeMessage
						{
							LocalTime = info.First.LocalTime,
							ServerTime = info.First.LastChangeTime,
							Bids = info.Second,
							Asks = info.Third
						}.ToMarketDepth(info.First, GetSecurity);

						info.Second = null;
						info.Third = null;
					}
				}
			}

			if (isNew)
				RaiseNewMarketDepths(new[] { info.First });

			return info.First;
		}

		/// <summary>
		/// Получить отфильтрованный стакан котировок.
		/// </summary>
		/// <param name="security">Инструмент, по которому нужно получить стакан.</param>
		/// <returns>Отфильтрованный стакан котировок.</returns>
		public MarketDepth GetFilteredMarketDepth(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (!_subscriptionManager.IsFilteredMarketDepthRegistered(security))
				throw new InvalidOperationException(LocalizedStrings.Str1097Params.Put(security.Id));

			return GetFilteredMarketDepthInfo(security).GetDepth();
		}

		private FilteredMarketDepthInfo GetFilteredMarketDepthInfo(Security security)
		{
			return _filteredMarketDepths.SafeAdd(security, s => new FilteredMarketDepthInfo(EntityFactory.CreateMarketDepth(s)));
		}

		/// <summary>
		/// Зарегистрировать заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
		public void RegisterOrder(Order order)
		{
			RegisterOrder(order, true);
		}

		private void RegisterOrder(Order order, bool initOrder)
		{
			try
			{
				this.AddOrderInfoLog(order, "RegisterOrder");

				CheckOnNew(order, order.Type != OrderTypes.Conditional, initOrder);

				var cs = order.Security as ContinuousSecurity;

				while (cs != null)
				{
					order.Security = cs.GetSecurity(order.Security.ToExchangeTime(CurrentTime));
					cs = order.Security as ContinuousSecurity;
				}

				if (initOrder)
					InitNewOrder(order);

				OnRegisterOrder(order);
			}
			catch (Exception ex)
			{
				SendOrderFailed(order, ex);
			}
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять и на основе нее зарегистрировать новую.</param>
		/// <param name="price">Цена новой заявки.</param>
		/// <param name="volume">Объем новой заявки.</param>
		/// <returns>Новая заявка.</returns>
		/// <remarks>
		/// Если объём не задан, меняется только цена.
		/// </remarks>
		public Order ReRegisterOrder(Order oldOrder, decimal price, decimal volume = 0)
		{
			if (oldOrder == null)
				throw new ArgumentNullException("oldOrder");

			var newOrder = oldOrder.ReRegisterClone(price, volume);
			ReRegisterOrder(oldOrder, newOrder);
			return newOrder;
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		public virtual void ReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (oldOrder == null)
				throw new ArgumentNullException("oldOrder");

			if (newOrder == null)
				throw new ArgumentNullException("newOrder");

			try
			{
				if (oldOrder.Security != newOrder.Security)
					throw new ArgumentException(LocalizedStrings.Str1098Params.Put(newOrder.Security.Id, oldOrder.Security.Id), "newOrder");

				if (oldOrder.Type == OrderTypes.Conditional)
				{
					CancelOrder(oldOrder);
					RegisterOrder(newOrder);
				}
				else
				{
					CheckOnOld(oldOrder);
					CheckOnNew(newOrder, false);

					if (oldOrder.Comment.IsEmpty())
						oldOrder.Comment = newOrder.Comment;

					InitNewOrder(newOrder);
					_entityCache.AddOrderByCancelTransaction(newOrder.TransactionId, oldOrder);

					OnReRegisterOrder(oldOrder, newOrder);
				}
			}
			catch (Exception ex)
			{
				SendOrderFailed(oldOrder, ex);
				SendOrderFailed(newOrder, ex);
			}
		}

		/// <summary>
		/// Перерегистрировать пару заявок на бирже.
		/// </summary>
		/// <param name="oldOrder1">Первая заявка, которую нужно снять.</param>
		/// <param name="newOrder1">Первая новая заявка, которую нужно зарегистрировать.</param>
		/// <param name="oldOrder2">Вторая заявка, которую нужно снять.</param>
		/// <param name="newOrder2">Вторая новая заявка, которую нужно зарегистрировать.</param>
		public void ReRegisterOrderPair(Order oldOrder1, Order newOrder1, Order oldOrder2, Order newOrder2)
		{
			if (oldOrder1 == null)
				throw new ArgumentNullException("oldOrder1");

			if (newOrder1 == null)
				throw new ArgumentNullException("newOrder1");

			if (oldOrder2 == null)
				throw new ArgumentNullException("oldOrder2");

			if (newOrder2 == null)
				throw new ArgumentNullException("newOrder2");

			try
			{
				if (oldOrder1.Security != newOrder1.Security)
					throw new ArgumentException(LocalizedStrings.Str1099Params.Put(newOrder1.Security.Id, oldOrder1.Security.Id), "newOrder1");

				if (oldOrder2.Security != newOrder2.Security)
					throw new ArgumentException(LocalizedStrings.Str1100Params.Put(newOrder2.Security.Id, oldOrder2.Security.Id), "newOrder2");

				if (oldOrder1.Type == OrderTypes.Conditional || oldOrder2.Type == OrderTypes.Conditional)
				{
					CancelOrder(oldOrder1);
					RegisterOrder(newOrder1);

					CancelOrder(oldOrder2);
					RegisterOrder(newOrder2);
				}
				else
				{
					CheckOnOld(oldOrder1);
					CheckOnNew(newOrder1, false);

					CheckOnOld(oldOrder2);
					CheckOnNew(newOrder2, false);

					if (oldOrder1.Comment.IsEmpty())
						oldOrder1.Comment = newOrder1.Comment;

					if (oldOrder2.Comment.IsEmpty())
						oldOrder2.Comment = newOrder2.Comment;

					InitNewOrder(newOrder1);
					InitNewOrder(newOrder2);

					_entityCache.AddOrderByCancelTransaction(newOrder1.TransactionId, oldOrder1);
					_entityCache.AddOrderByCancelTransaction(newOrder2.TransactionId, oldOrder2);

					OnReRegisterOrderPair(oldOrder1, newOrder1, oldOrder2, newOrder2);
				}
			}
			catch (Exception ex)
			{
				SendOrderFailed(oldOrder1, ex);
				SendOrderFailed(newOrder1, ex);

				SendOrderFailed(oldOrder2, ex);
				SendOrderFailed(newOrder2, ex);
			}
		}

		/// <summary>
		/// Отменить заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую нужно отменять.</param>
		public void CancelOrder(Order order)
		{
			try
			{
				this.AddOrderInfoLog(order, "CancelOrder");

				CheckOnOld(order);

				var transactionId = TransactionIdGenerator.GetNextId();
				_entityCache.AddOrderByCancelTransaction(transactionId, order);

				//order.InitLatencyMonitoring(false);
				OnCancelOrder(order, transactionId);
			}
			catch (Exception ex)
			{
				SendOrderFailed(order, ex);
			}
		}

		private void SendOrderFailed(Order order, Exception error)
		{
			SendOutMessage(new OrderFail
			{
				Order = order,
				Error = error,
				ServerTime = CurrentTime,
			}.ToMessage());
		}

		private static void CheckOnNew(Order order, bool checkVolume = true, bool checkTransactionId = true)
		{
			ChechOrderState(order);

			if (checkVolume)
			{
				if (order.Volume == 0)
					throw new ArgumentException(LocalizedStrings.Str894, "order");

				if (order.Volume < 0)
					throw new ArgumentOutOfRangeException("order", order.Volume, LocalizedStrings.Str895);
			}

			if (order.Id != null || !order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str896Params.Put(order.Id == null ? order.StringId : order.Id.To<string>()), "order");

			if (!checkTransactionId)
				return;

			if (order.TransactionId != 0)
				throw new ArgumentException(LocalizedStrings.Str897Params.Put(order.TransactionId), "order");

			if (order.State != OrderStates.None)
				throw new ArgumentException(LocalizedStrings.Str898Params.Put(order.State), "order");
		}

		private static void CheckOnOld(Order order)
		{
			ChechOrderState(order);

			if (order.TransactionId == 0 && order.Id == null && order.StringId.IsEmpty())
				throw new ArgumentException(LocalizedStrings.Str899, "order");
		}

		private static void ChechOrderState(Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			if (order.Type == OrderTypes.Conditional && order.Condition == null)
				throw new ArgumentException(LocalizedStrings.Str889, "order");

			if (order.Security == null)
				throw new ArgumentException(LocalizedStrings.Str890, "order");

			if (order.Portfolio == null)
				throw new ArgumentException(LocalizedStrings.Str891, "order");

			if (order.Price < 0)
				throw new ArgumentOutOfRangeException("order", order.Price, LocalizedStrings.Str892);

			if (order.Price == 0 && (order.Type == OrderTypes.Limit || order.Type == OrderTypes.ExtRepo || order.Type == OrderTypes.Repo || order.Type == OrderTypes.Rps))
				throw new ArgumentException(LocalizedStrings.Str893, "order");
		}

		/// <summary>
		/// Инициализировать новую заявку идентификатором транзакции, информацией о подключении и т.д.
		/// </summary>
		/// <param name="order">Новая заявка.</param>
		private void InitNewOrder(Order order)
		{
			order.Balance = order.Volume;

			if (order.ExtensionInfo == null)
				order.ExtensionInfo = new Dictionary<object, object>();

			//order.InitializationTime = trader.MarketTime;
			if (order.TransactionId == 0)
				order.TransactionId = TransactionIdGenerator.GetNextId();

			order.Connector = this;

			if (order.Security is ContinuousSecurity)
				order.Security = ((ContinuousSecurity)order.Security).GetSecurity(order.Security.ToExchangeTime(CurrentTime));

			order.LocalTime = CurrentTime.LocalDateTime;
			order.State = OrderStates.Pending;

			if (!_entityCache.TryAddOrder(order))
				throw new ArgumentException(LocalizedStrings.Str1101Params.Put(order.TransactionId));

			//RaiseNewOrder(order);
			SendOutMessage(order.ToMessage());
		}

		/// <summary>
		/// Зарегистрировать заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, содержащая информацию для регистрации.</param>
		protected virtual void OnRegisterOrder(Order order)
		{
			var regMsg = order.CreateRegisterMessage(GetSecurityId(order.Security));

			var depoName = order.Portfolio.GetValue<string>(PositionChangeTypes.DepoName);
			if (depoName != null)
				regMsg.AddValue(PositionChangeTypes.DepoName, depoName);

			if (CalculateMessages)
				SlippageManager.ProcessMessage(regMsg);

			SendInMessage(regMsg);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		protected virtual void OnReRegisterOrder(Order oldOrder, Order newOrder)
		{
			if (IsSupportAtomicReRegister && oldOrder.Security.Board.IsSupportAtomicReRegister)
			{
				var replaceMsg = oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security));
				SendInMessage(replaceMsg);
			}
			else
			{
				CancelOrder(oldOrder);
				RegisterOrder(newOrder, false);
			}
		}

		/// <summary>
		/// Перерегистрировать пару заявок на бирже.
		/// </summary>
		/// <param name="oldOrder1">Первая заявка, которую нужно снять.</param>
		/// <param name="newOrder1">Первая новая заявка, которую нужно зарегистрировать.</param>
		/// <param name="oldOrder2">Вторая заявка, которую нужно снять.</param>
		/// <param name="newOrder2">Вторая новая заявка, которую нужно зарегистрировать.</param>
		protected virtual void OnReRegisterOrderPair(Order oldOrder1, Order newOrder1, Order oldOrder2, Order newOrder2)
		{
			CancelOrder(oldOrder1);
			RegisterOrder(newOrder1, false);

			CancelOrder(oldOrder2);
			RegisterOrder(newOrder2, false);
		}

		/// <summary>
		/// Отменить заявку на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую нужно отменять.</param>
		/// <param name="transactionId">Идентификатор транзакции отмены.</param>
		protected virtual void OnCancelOrder(Order order, long transactionId)
		{
			var cancelMsg = order.CreateCancelMessage(GetSecurityId(order.Security), transactionId);
			SendInMessage(cancelMsg);
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, <see langword="false"/> - если только обычный и <see langword="null"/> - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно <see langword="null"/>, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно <see langword="null"/>, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно <see langword="null"/>, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно <see langword="null"/>, то инструмент не попадает в фильтр снятия заявок.</param>
		public void CancelOrders(bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			var transactionId = TransactionIdGenerator.GetNextId();
			_entityCache.AddOrderByCancelTransaction(transactionId, null);
			OnCancelOrders(transactionId, isStopOrder, portfolio, direction, board, security);
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="transactionId">Идентификатор транзакции отмены.</param>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, <see langword="false"/> - если только обычный и <see langword="null"/> - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно <see langword="null"/>, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно <see langword="null"/>, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно <see langword="null"/>, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно <see langword="null"/>, то инструмент не попадает в фильтр снятия заявок.</param>
		protected virtual void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			var cancelMsg = MessageConverterHelper.CreateGroupCancelMessage(transactionId, isStopOrder, portfolio, direction, board, security == null ? default(SecurityId) : GetSecurityId(security), security);
			SendInMessage(cancelMsg);
		}

		private DateTimeOffset _prevTime;

		private void ProcessTimeInterval(Message message)
		{
			if (message == _marketTimeMessage)
			{
				lock (_marketTimerSync)
					_isMarketTimeHandled = true;
			}

			// output messages from adapters goes asynchronously
			if (_currentTime > message.LocalTime)
				return;

			_currentTime = message.LocalTime;

			if (_prevTime.IsDefault())
			{
				_prevTime = _currentTime;
				return;
			}

			var diff = _currentTime - _prevTime;

			if (diff >= MarketTimeChangedInterval)
			{
				_prevTime = _currentTime;
				RaiseMarketTimeChanged(diff);
			}
		}

		/// <summary>
		/// Получить инструмент по коду.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента.</param>
		/// <returns>Инструмент.</returns>
		protected Security GetSecurity(SecurityId securityId)
		{
			return GetSecurity(CreateSecurityId(securityId.SecurityCode, securityId.BoardCode), s => false);
		}

		/// <summary>
		/// Получить инструмент по коду. Если инструмент не найден, то для создания инструмента вызывается <see cref="IEntityFactory.CreateSecurity"/>.
		/// </summary>
		/// <param name="id">Идентификатор инструмента.</param>
		/// <param name="changeSecurity">Обработчик, изменяющий инструмент. Возвращает <see langword="true"/>, если инструмент был изменен, и необходимо вызвать <see cref="IConnector.SecuritiesChanged"/>.</param>
		/// <returns>Инструмент.</returns>
		private Security GetSecurity(string id, Func<Security, bool> changeSecurity)
		{
			if (id.IsEmpty())
				throw new ArgumentNullException("id");

			if (changeSecurity == null)
				throw new ArgumentNullException("changeSecurity");

			bool isNew;

			var security = _securities.SafeAdd(id, key =>
			{
				var s = EntityFactory.CreateSecurity(key);

				if (s == null)
					throw new InvalidOperationException(LocalizedStrings.Str1102Params.Put(key));

				if (s.ExtensionInfo == null)
					s.ExtensionInfo = new Dictionary<object, object>();

				var info = SecurityIdGenerator.Split(key);

				if (s.Board == null)
					s.Board = ExchangeBoard.GetOrCreateBoard(GetBoardCode(info.Item2));

				if (s.Code.IsEmpty())
					s.Code = info.Item1;

				if (s.Name.IsEmpty())
					s.Name = info.Item1;

				if (s.Class.IsEmpty())
					s.Class = info.Item2;

				return s;
			}, out isNew);

			var isChanged = changeSecurity(security);

			if (isNew)
			{
				if (security.Board == null)
					throw new InvalidOperationException(LocalizedStrings.Str903Params.Put(id));

				_exchangeBoards.TryAdd(security.Board);
				RaiseNewSecurities(new[] { security });
			}
			else if (isChanged)
				RaiseSecurityChanged(security);

			return security;
		}

		/// <summary>
		/// Получить <see cref="SecurityId"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Идентификатор инструмента.</returns>
		public SecurityId GetSecurityId(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var secId = security.ToSecurityId(SecurityIdGenerator);

			lock (_suspendSync)
				secId.Native = GetNativeId(security);

			return secId;
		}

		private void ProcessMyTrades<T>(Order order, T id, Dictionary<T, List<ExecutionMessage>> nonOrderedMyTrades)
		{
			var value = nonOrderedMyTrades.TryGetValue(id);

			if (value == null)
				return;

			var retVal = new List<ExecutionMessage>();

			foreach (var message in value.ToArray())
			{
				// проверяем совпадение по дате, исключая ситуация сопоставления сделки с заявкой, имеющая неуникальный идентификатор
				if (message.ServerTime.Date != order.Time.Date)
					continue;

				retVal.Add(message);
				value.Remove(message);
			}

			if (value.IsEmpty())
				nonOrderedMyTrades.Remove(id);

			var trades = retVal
				.Select(t => _entityCache.ProcessMyTradeMessage(order.Security, t))
				.Where(t => t != null && t.Item2)
				.Select(t => t.Item1)
				.ToArray();

			if (trades.Length > 0)
				RaiseNewMyTrades(trades);
		}

		/// <summary>
		/// Получить портфель по названию. Если портфель не зарегистрирован, то он создается через <see cref="IEntityFactory.CreatePortfolio"/>.
		/// </summary>
		/// <param name="name">Название портфеля.</param>
		/// <param name="changePortfolio">Обработчик, изменяющий портфель.</param>
		/// <returns>Портфель.</returns>
		private Portfolio GetPortfolio(string name, Func<Portfolio, bool> changePortfolio = null)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			var result = _entityCache.ProcessPortfolio(name, changePortfolio);

			var portfolio = result.Item1;
			var isNew = result.Item2;
			var isChanged = result.Item3;

			if (isNew)
			{
				this.AddInfoLog(LocalizedStrings.Str1105Params, portfolio.Name);
				RaiseNewPortfolios(new[] { portfolio });
			}
			else if (isChanged)
				RaisePortfoliosChanged(new[] { portfolio });

			return portfolio;
		}

		private string GetBoardCode(string secClass)
		{
			return MarketDataAdapter.GetBoardCode(secClass);
		}

		/// <summary>
		/// Сгенерировать <see cref="Security.Id"/> инструмента.
		/// </summary>
		/// <param name="secCode">Код инструмента.</param>
		/// <param name="secClass">Класс инструмента.</param>
		/// <returns><see cref="Security.Id"/> инструмента.</returns>
		protected string CreateSecurityId(string secCode, string secClass)
		{
			return SecurityIdGenerator.GenerateId(secCode, GetBoardCode(secClass));
		}

		/// <summary>
		/// Получить значение маркет-данных для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public object GetSecurityValue(Security security, Level1Fields field)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var values = _securityValues.TryGetValue(security);
			return values == null ? null : values[(int)field];
		}

		/// <summary>
		/// Получить набор доступных полей <see cref="Level1Fields"/>, для которых есть маркет-данные для инструмента.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <returns>Набор доступных полей.</returns>
		public IEnumerable<Level1Fields> GetLevel1Fields(Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			var values = _securityValues.TryGetValue(security);

			if (values == null)
				return Enumerable.Empty<Level1Fields>();

			var fields = new List<Level1Fields>(30);

			for (var i = 0; i < values.Length; i++)
			{
				if (values[i] != null)
					fields.Add((Level1Fields)i);
			}

			return fields;
		}

		private object[] GetSecurityValues(Security security)
		{
			return _securityValues.SafeAdd(security, key => new object[Enumerator.GetValues<Level1Fields>().Count()]);
		}

		/// <summary>
		/// Очистить кэш данных.
		/// </summary>
		public virtual void ClearCache()
		{
			_entityCache.Clear();
			_prevTime = default(DateTimeOffset);

			_exchangeBoards.Clear();
			_securities.Clear();

			_securityLookups.Clear();
			_portfolioLookups.Clear();

			_positions.Clear();
			_filteredMarketDepths.Clear();

			_lookupQueue.Clear();
			_lookupResult.Clear();

			_marketDepths.Clear();

			_nativeIdSecurities.Clear();

			_nonOrderedByIdMyTrades.Clear();
			_nonOrderedByStringIdMyTrades.Clear();
			_nonOrderedByTransactionIdMyTrades.Clear();

			_orderCancelFails.Clear();
			_orderRegisterFails.Clear();

			ConnectionState = ConnectionStates.Disconnected;

			_adapterStates.Clear();

			_suspendedSecurityMessages.Clear();

			_subscriptionManager.ClearCache();

			_securityValues.Clear();
			_sessionStates.Clear();
			_filteredMarketDepths.Clear();
			_olBuilders.Clear();

			SendInMessage(new ResetMessage());
		}

		/// <summary>
		/// Запустить таймер генерации сообщений <see cref="TimeMessage"/> с интервалом <see cref="MarketTimeChangedInterval"/>.
		/// </summary>
		protected virtual void StartMarketTimer()
		{
			if (null != _marketTimer)
				return;

			_isMarketTimeHandled = true;

			_marketTimer = ThreadingHelper
				.Timer(() =>
				{
					// TimeMsg required for notify invoke MarketTimeChanged event (and active time based IMarketRule-s)
					// No need to put _marketTimeMessage again, if it still in queue.

					lock (_marketTimerSync)
					{
						if (!_isMarketTimeHandled)
							return;

						_isMarketTimeHandled = false;
					}

					_marketTimeMessage.LocalTime = TimeHelper.Now;
					SendOutMessage(_marketTimeMessage);
				})
				.Interval(MarketTimeChangedInterval);
		}

		/// <summary>
		/// Остановить таймер, запущенный ранее через <see cref="StartMarketTimer"/>.
		/// </summary>
		protected void StopMarketTimer()
		{
			if (null == _marketTimer)
				return;

			_marketTimer.Dispose();
			_marketTimer = null;
		}

		/// <summary>
		/// Освободить занятые ресурсы. В частности, отключиться от торговой системы через <see cref="Disconnect"/>, а также закрыть поток Connector Out
		/// </summary>
		protected override void DisposeManaged()
		{
			_isDisposing = true;

			if (ConnectionState == ConnectionStates.Connected)
			{
				try
				{
					Disconnect();
				}
				catch (Exception ex)
				{
					RaiseConnectionError(ex);
				}
				int nWaitCycle = 50;	//yaa_b_11.10.2015_17:03
				int nWaitPeriod = 5000;
				for (int nSecs = 0; nSecs < nWaitPeriod; nSecs += nWaitCycle)
				{
					if (ConnectionState == ConnectionStates.Disconnected)
						break;
					Thread.Sleep(nWaitCycle);
				}	//yaa_e
			}

			if (ConnectionState == ConnectionStates.Disconnecting)	//yaa_11.10.2015_17:03
				MessageBox.Show("Отключение соединения не было корректно произведено! Необходимо перезапустить программу и сервер Quik.");

			base.DisposeManaged();

			_connectorStat.Remove(this);

			//if (ConnectionState == ConnectionStates.Disconnected || ConnectionState == ConnectionStates.Failed)
			//	TransactionAdapter = null;

			//if (ExportState == ConnectionStates.Disconnected || ExportState == ConnectionStates.Failed)
			//	MarketDataAdapter = null;

			StopMarketTimer();	//yaa_b_01.10.2015_22:36
			if (!_outMessageChannel.IsNull() && _outMessageChannel.IsOpened)
				_outMessageChannel.Close();
			Adapter.Dispose();

			//Adapter = null;	//yaa_e_01.10.2015_23:01
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			TradesKeepCount = storage.GetValue("TradesKeepCount", TradesKeepCount);
			OrdersKeepCount = storage.GetValue("OrdersKeepCount", OrdersKeepCount);
			UpdateSecurityLastQuotes = storage.GetValue("UpdateSecurityLastQuotes", true);
			UpdateSecurityByLevel1 = storage.GetValue("UpdateSecurityByLevel1", true);
			ReConnectionSettings.Load(storage.GetValue<SettingsStorage>("ReConnectionSettings"));

			Adapter.Load(storage.GetValue<SettingsStorage>("Adapter"));

			CreateDepthFromOrdersLog = storage.GetValue<bool>("CreateDepthFromOrdersLog");
			CreateTradesFromOrdersLog = storage.GetValue<bool>("CreateTradesFromOrdersLog");
			CreateDepthFromLevel1 = storage.GetValue("CreateDepthFromLevel1", CreateDepthFromLevel1);

			MarketTimeChangedInterval = storage.GetValue<TimeSpan>("MarketTimeChangedInterval");

			CreateAssociatedSecurity = storage.GetValue("CreateAssociatedSecurity", CreateAssociatedSecurity);

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			if (storage == null)
				throw new ArgumentNullException("storage");

			storage.SetValue("TradesKeepCount", TradesKeepCount);
			storage.SetValue("OrdersKeepCount", OrdersKeepCount);
			storage.SetValue("UpdateSecurityLastQuotes", UpdateSecurityLastQuotes);
			storage.SetValue("UpdateSecurityByLevel1", UpdateSecurityByLevel1);
			storage.SetValue("ReConnectionSettings", ReConnectionSettings.Save());

			storage.SetValue("Adapter", Adapter.Save());

			storage.SetValue("CreateDepthFromOrdersLog", CreateDepthFromOrdersLog);
			storage.SetValue("CreateTradesFromOrdersLog", CreateTradesFromOrdersLog);
			storage.SetValue("CreateDepthFromLevel1", CreateDepthFromLevel1);

			storage.SetValue("MarketTimeChangedInterval", MarketTimeChangedInterval);

			storage.SetValue("CreateAssociatedSecurity", CreateAssociatedSecurity);

			base.Save(storage);
		}
	}
}