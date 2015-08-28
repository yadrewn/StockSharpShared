namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Преобразователь сообщений вида <see cref="QuoteChangeMessage"/> и <see cref="ExecutionMessage"/> (ассоциированный с тиковой сделкой)
	/// в единый поток <see cref="ExecutionMessage"/> (ассоциированный с логом заявок).
	/// </summary>
	class ExecutionLogConverter
	{
		private readonly Random _volumeRandom = new Random(TimeHelper.Now.Millisecond);
		private readonly Random _priceRandom = new Random(TimeHelper.Now.Millisecond);
		private readonly SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> _bids;
		private readonly SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> _asks;
		private decimal _currSpreadPrice;
		private readonly MarketEmulatorSettings _settings;
		private decimal _prevTickPrice;
		// указывает, есть ли реальные стаканы, чтобы своей псевдо генерацией не портить настоящую историю
		private DateTime _lastDepthDate;
		private DateTime _lastTradeDate;
		private SecurityMessage _securityDefinition = new SecurityMessage
		{
			PriceStep = 1,
			VolumeStep = 1,
		};
		private bool _stepsUpdated;
		private TimeZoneInfo _timeZoneInfo = TimeZoneInfo.Local;

		public ExecutionLogConverter(SecurityId securityId,
			SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> bids,
			SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> asks,
			MarketEmulatorSettings settings)
		{
			if (bids == null)
				throw new ArgumentNullException("bids");

			if (asks == null)
				throw new ArgumentNullException("asks");

			if (settings == null)
				throw new ArgumentNullException("settings");

			_bids = bids;
			_asks = asks;
			_settings = settings;
			SecurityId = securityId;
		}

		/// <summary>
		/// Идентификатор инструмента.
		/// </summary>
		public SecurityId SecurityId { get; private set; }

		/// <summary>
		/// Преобразовать котировки.
		/// </summary>
		/// <param name="message">Котировки.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(QuoteChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (!_stepsUpdated)
			{
				var quote = message.GetBestBid() ?? message.GetBestAsk();

				if (quote != null)
				{
					_securityDefinition.PriceStep = quote.Price.GetDecimalInfo().EffectiveScale.GetPriceStep();
					_securityDefinition.VolumeStep = quote.Volume.GetDecimalInfo().EffectiveScale.GetPriceStep();
					_stepsUpdated = true;	
				}
			}

			_lastDepthDate = message.LocalTime.Date;

			// чтобы склонировать внутренние котировки
			//message = (QuoteChangeMessage)message.Clone();
			// TODO для ускорения идет shallow copy котировок
			var newBids = message.IsSorted ? message.Bids : message.Bids.OrderByDescending(q => q.Price);
			var newAsks = message.IsSorted ? message.Asks : message.Asks.OrderBy(q => q.Price);

			return ProcessQuoteChange(message.LocalTime, newBids.ToArray(), newAsks.ToArray());
		}

		private IEnumerable<ExecutionMessage> ProcessQuoteChange(DateTime time, QuoteChange[] newBids, QuoteChange[] newAsks)
		{
			decimal bestBidPrice;
			decimal bestAskPrice;

			var retVal =
				GetDiff(time, _bids, newBids, Sides.Buy, out bestBidPrice)
				.Concat(GetDiff(time, _asks, newAsks, Sides.Sell, out bestAskPrice));

			var spreadPrice = bestAskPrice == 0
				? bestBidPrice
				: (bestBidPrice == 0
					? bestAskPrice
					: (bestAskPrice - bestBidPrice) / 2 + bestBidPrice);

			//при обновлении стакана необходимо учитывать направление сдвига, чтобы не было ложного исполнения при наложении бидов и асков.
			//т.е. если цена сдвинулась вниз, то обновление стакана необходимо начинать с минимального бида.
			retVal = (spreadPrice < _currSpreadPrice)
				? retVal.OrderBy(m => m.Price)
				: retVal.OrderByDescending(m => m.Price);

			_currSpreadPrice = spreadPrice;

			return retVal.ToArray();
		}

		private IEnumerable<ExecutionMessage> GetDiff(DateTime time, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> from, IEnumerable<QuoteChange> to, Sides side, out decimal newBestPrice)
		{
			newBestPrice = 0;

			var diff = new List<ExecutionMessage>();

			var canProcessFrom = true;
			var canProcessTo = true;

			QuoteChange currFrom = null;
			QuoteChange currTo = null;

			// TODO
			//List<ExecutionMessage> currOrders = null;

			var mult = side == Sides.Buy ? -1 : 1;
			bool? isSpread = null;

			using (var fromEnum = from.GetEnumerator())
			using (var toEnum = to.GetEnumerator())
			{
				while (true)
				{
					if (canProcessFrom && currFrom == null)
					{
						if (!fromEnum.MoveNext())
							canProcessFrom = false;
						else
						{
							currFrom = fromEnum.Current.Value.Second;
							isSpread = isSpread == null;
						}
					}

					if (canProcessTo && currTo == null)
					{
						if (!toEnum.MoveNext())
							canProcessTo = false;
						else
						{
							currTo = toEnum.Current;

							if (newBestPrice == 0)
								newBestPrice = currTo.Price;
						}
					}

					if (currFrom == null)
					{
						if (currTo == null)
							break;
						else
						{
							AddExecMsg(diff, time, currTo, currTo.Volume, false);
							currTo = null;
						}
					}
					else
					{
						if (currTo == null)
						{
							AddExecMsg(diff, time, currFrom, -currFrom.Volume, isSpread.Value);
							currFrom = null;
						}
						else
						{
							if (currFrom.Price == currTo.Price)
							{
								if (currFrom.Volume != currTo.Volume)
								{
									AddExecMsg(diff, time, currTo, currTo.Volume - currFrom.Volume, isSpread.Value);
								}

								currFrom = currTo = null;
							}
							else if (currFrom.Price * mult > currTo.Price * mult)
							{
								AddExecMsg(diff, time, currTo, currTo.Volume, isSpread.Value);
								currTo = null;
							}
							else
							{
								AddExecMsg(diff, time, currFrom, -currFrom.Volume, isSpread.Value);
								currFrom = null;
							}
						}
					}
				}
			}

			return diff;
		}

		private readonly RandomArray<bool> _isMatch = new RandomArray<bool>(100);

		private void AddExecMsg(List<ExecutionMessage> diff, DateTime time, QuoteChange quote, decimal volume, bool isSpread)
		{
			if (volume > 0)
				diff.Add(CreateMessage(time, quote.Side, quote.Price, volume));
			else
			{
				volume = volume.Abs();

				// matching only top orders (spread)
				if (isSpread && volume > 1 && _isMatch.Next())
				{
					var tradeVolume = (int)volume / 2;

					diff.Add(new ExecutionMessage
					{
						Side = quote.Side,
						Volume = tradeVolume,
						ExecutionType = ExecutionTypes.Tick,
						SecurityId = SecurityId,
						LocalTime = time,
						TradePrice = quote.Price,
					});

					// that tick will not affect on order book
					//volume -= tradeVolume;
				}

				diff.Add(CreateMessage(time, quote.Side, quote.Price, volume, true));
			}
		}

		/// <summary>
		/// Преобразовать тиковую сделку.
		/// </summary>
		/// <param name="message">Тиковая сделка.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(ExecutionMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			if (!_stepsUpdated)
			{
				_securityDefinition.PriceStep = message.GetTradePrice().GetDecimalInfo().EffectiveScale.GetPriceStep();
				_securityDefinition.VolumeStep = message.SafeGetVolume().GetDecimalInfo().EffectiveScale.GetPriceStep();
				_stepsUpdated = true;
			}

			//if (message.DataType != ExecutionDataTypes.Trade)
			//	throw new ArgumentOutOfRangeException("Тип данных не может быть {0}.".Put(message.DataType), "message");

			_lastTradeDate = message.LocalTime.Date;

			return ProcessExecution(message);
		}

		private IEnumerable<ExecutionMessage> ProcessExecution(ExecutionMessage message)
		{
			var retVal = new List<ExecutionMessage>();

			var bestBid = _bids.FirstOrDefault();
			var bestAsk = _asks.FirstOrDefault();

			var tradePrice = message.GetTradePrice();
			var volume = message.SafeGetVolume();
			var time = message.LocalTime;

			if (bestBid.Value != null && tradePrice <= bestBid.Key)
			{
				// тик попал в биды, значит была крупная заявка по рынку на продажу,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(retVal, _bids, message, Sides.Sell);

				// подтягиваем противоположные котировки и снимаем лишние заявки
				TryCreateOppositeOrder(retVal, _asks, time, tradePrice, volume, Sides.Buy);
			}
			else if (bestAsk.Value != null && tradePrice >= bestAsk.Key)
			{
				// тик попал в аски, значит была крупная заявка по рынку на покупку,
				// которая возможна исполнила наши заявки

				ProcessMarketOrder(retVal, _asks, message, Sides.Buy);

				TryCreateOppositeOrder(retVal, _bids, time, tradePrice, volume, Sides.Sell);
			}
			else if (bestBid.Value != null && bestAsk.Value != null && bestBid.Key < tradePrice && tradePrice < bestAsk.Key)
			{
				// тик попал в спред, значит в спреде до сделки была заявка.
				// создаем две лимитки с разных сторон, но одинаковой ценой.
				// если в эмуляторе есть наша заявка на этом уровне, то она исполниться.
				// если нет, то эмулятор взаимно исполнит эти заявки друг об друга

				var originSide = GetOrderSide(message);

				retVal.Add(CreateMessage(time, originSide, tradePrice, volume + (_securityDefinition.VolumeStep ?? 1 * _settings.VolumeMultiplier), tif: TimeInForce.MatchOrCancel));

				var spreadStep = _settings.SpreadSize * GetPriceStep();

				// try to fill depth gaps

				var newBestPrice = tradePrice + spreadStep;

				var depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					var diff = bestAsk.Key - newBestPrice;

					if (diff > 0)
					{
						retVal.Add(CreateMessage(time, Sides.Sell, newBestPrice, 0));
						newBestPrice += spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				newBestPrice = tradePrice - spreadStep;

				depth = _settings.MaxDepth;
				while (--depth > 0)
				{
					var diff = newBestPrice - bestBid.Key;

					if (diff > 0)
					{
						retVal.Add(CreateMessage(time, Sides.Buy, newBestPrice, 0));
						newBestPrice -= spreadStep * _priceRandom.Next(1, _settings.SpreadSize);
					}
					else
						break;
				}

				retVal.Add(CreateMessage(time, originSide.Invert(), tradePrice, volume, tif: TimeInForce.MatchOrCancel));
			}
			else
			{
				// если у нас стакан был полу пустой, то тик формирует некий ценовой уровень в стакана,
				// так как прошедщая заявка должна была обо что-то удариться. допускаем, что после
				// прохождения сделки на этом ценовом уровне остался объем равный тиковой сделки

				var hasOpposite = true;

				Sides originSide;

				// определяем направление псевдо-ранее существовавшей заявки, из которой получился тик
				if (bestBid.Value != null)
					originSide = Sides.Sell;
				else if (bestAsk.Value != null)
					originSide = Sides.Buy;
				else
				{
					originSide = GetOrderSide(message);
					hasOpposite = false;
				}

				retVal.Add(CreateMessage(time, originSide, tradePrice, volume));

				// если стакан был полностью пустой, то формируем сразу уровень с противоположной стороны
				if (!hasOpposite)
				{
					var oppositePrice = tradePrice + _settings.SpreadSize * GetPriceStep() * (originSide == Sides.Buy ? 1 : -1);

					if (oppositePrice > 0)
						retVal.Add(CreateMessage(time, originSide.Invert(), oppositePrice, volume));
				}
			}

			if (!HasDepth(time))
			{
				// если стакан слишком разросся, то удаляем его хвосты (не удаляя пользовательские заявки)
				CancelWorstQuote(retVal, time, Sides.Buy, _bids);
				CancelWorstQuote(retVal, time, Sides.Sell, _asks);	
			}

			_prevTickPrice = tradePrice;

			return retVal;
		}

		private Sides GetOrderSide(ExecutionMessage message)
		{
			if (message.OriginSide == null)
				return message.TradePrice > _prevTickPrice ? Sides.Sell : Sides.Buy;
			else
				return message.OriginSide.Value.Invert();
		}

		/// <summary>
		/// Преобразовать первый уровень маркет-данных.
		/// </summary>
		/// <param name="message">Первый уровень маркет-данных.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(Level1ChangeMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			var retVal = new List<ExecutionMessage>();

			var bestBidPrice = 0m;
			var bestBidVolume = 0m;
			var bestAskPrice = 0m;
			var bestAskVolume = 0m;
			var lastTradePrice = 0m;
			var lastTradeVolume = 0m;

			foreach (var change in message.Changes)
			{
				switch (change.Key)
				{
					case Level1Fields.LastTradePrice:
						lastTradePrice = (decimal)change.Value;
						break;
					case Level1Fields.LastTradeVolume:
						lastTradeVolume = (decimal)change.Value;
						break;
					case Level1Fields.BestBidPrice:
						bestBidPrice = (decimal)change.Value;
						break;
					case Level1Fields.BestBidVolume:
						bestBidVolume = (decimal)change.Value;
						break;
					case Level1Fields.BestAskPrice:
						bestAskPrice = (decimal)change.Value;
						break;
					case Level1Fields.BestAskVolume:
						bestAskVolume = (decimal)change.Value;
						break;
				}
			}

			ProcessLevel1Depth(message, bestBidPrice, bestBidVolume, bestAskPrice, bestAskVolume, retVal);
			ProcessLevel1Trade(message, lastTradePrice, lastTradeVolume, retVal);

			return retVal;
		}

		private void ProcessLevel1Depth(Level1ChangeMessage message, decimal bestBidPrice, decimal bestBidVolume, decimal bestAskPrice, decimal bestAskVolume, List<ExecutionMessage> retVal)
		{
			if (message.LocalTime.Date == _lastDepthDate)
				return;

			QuoteChange ask = null;
			QuoteChange bid = null;

			if (bestAskPrice != 0 && bestAskVolume != 0)
				ask = new QuoteChange(Sides.Sell, bestAskPrice, bestAskVolume);

			if (bestBidPrice != 0 && bestBidVolume != 0)
				bid = new QuoteChange(Sides.Buy, bestBidPrice, bestBidVolume);

			if (ask == null && bid == null)
				return;

			retVal.AddRange(ProcessQuoteChange(message.LocalTime,
				bid != null ? new[] { bid } : ArrayHelper.Empty<QuoteChange>(),
				ask != null ? new[] { ask } : ArrayHelper.Empty<QuoteChange>()));
		}

		private void ProcessLevel1Trade(Level1ChangeMessage message, decimal lastTradePrice, decimal lastTradeVolume, List<ExecutionMessage> retVal)
		{
			if (message.LocalTime.Date == _lastTradeDate)
				return;

			if (lastTradePrice == 0 || lastTradeVolume == 0)
				return;

			var exec = new ExecutionMessage
			{
				LocalTime = message.LocalTime,
				ServerTime = message.ServerTime,
				SecurityId = message.SecurityId,
				ExecutionType = ExecutionTypes.Tick,
				TradePrice = lastTradePrice,
				Volume = lastTradeVolume,
			};

			retVal.AddRange(ProcessExecution(exec));
		}

		private void ProcessMarketOrder(List<ExecutionMessage> retVal, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes, ExecutionMessage tradeMessage, Sides orderSide)
		{
			// вычисляем объем заявки по рынку, который смог бы пробить текущие котировки.

			var tradePrice = tradeMessage.GetTradePrice();
			// bigOrder - это наша большая рыночная заявка, которая способствовала появлению tradeMessage
			var bigOrder = CreateMessage(tradeMessage.LocalTime, orderSide, tradePrice, 0, tif: TimeInForce.MatchOrCancel);
			var sign = orderSide == Sides.Buy ? -1 : 1;
			var hasQuotes = false;

			foreach (var pair in quotes)
			{
				var quote = pair.Value.Second;

				if (quote.Price * sign > tradeMessage.TradePrice * sign)
				{
					bigOrder.Volume += quote.Volume;
				}
				else
				{
					if (quote.Price == tradeMessage.TradePrice)
					{
						bigOrder.Volume += tradeMessage.Volume;

						//var diff = tradeMessage.Volume - quote.Volume;

						//// если объем котиовки был меньше объема сделки
						//if (diff > 0)
						//	retVal.Add(CreateMessage(tradeMessage.LocalTime, quote.Side, quote.Price, diff));
					}
					else
					{
						if ((tradePrice - quote.Price).Abs() == _securityDefinition.PriceStep)
						{
							// если на один шаг цены выше/ниже есть котировка, то не выполняем никаких действий
							// иначе добавляем новый уровень в стакан, чтобы не было большого расхождения цен.
							hasQuotes = true;
						}
					
						break;
					}

					//// если котировки с ценой сделки вообще не было в стакане
					//else if (quote.Price * sign < tradeMessage.TradePrice * sign)
					//{
					//	retVal.Add(CreateMessage(tradeMessage.LocalTime, quote.Side, tradeMessage.Price, tradeMessage.Volume));
					//}
				}
			}

			retVal.Add(bigOrder);

			// если собрали все котировки, то оставляем заявку в стакане по цене сделки
			if (!hasQuotes)
				retVal.Add(CreateMessage(tradeMessage.LocalTime, orderSide.Invert(), tradePrice, tradeMessage.SafeGetVolume()));
		}

		private void TryCreateOppositeOrder(List<ExecutionMessage> retVal, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes, DateTime localTime, decimal tradePrice, decimal volume, Sides originSide)
		{
			if (HasDepth(localTime))
				return;

			var oppositePrice = tradePrice + _settings.SpreadSize * GetPriceStep() * (originSide == Sides.Buy ? 1 : -1);

			var bestQuote = quotes.FirstOrDefault();

			if (bestQuote.Value == null || ((originSide == Sides.Buy && oppositePrice < bestQuote.Key) || (originSide == Sides.Sell && oppositePrice > bestQuote.Key)))
				retVal.Add(CreateMessage(localTime, originSide.Invert(), oppositePrice, volume));
		}

		private void CancelWorstQuote(List<ExecutionMessage> retVal, DateTime time, Sides side, SortedDictionary<decimal, RefPair<List<ExecutionMessage>, QuoteChange>> quotes)
		{
			if (quotes.Count <= _settings.MaxDepth)
				return;

			var worst = quotes.Last();
			var volume = worst.Value.First.Where(e => e.PortfolioName == null).Sum(e => e.Volume.Value);

			if (volume == 0)
				return;

			retVal.Add(CreateMessage(time, side, worst.Key, volume, true));
		}

		private ExecutionMessage CreateMessage(DateTime time, Sides side, decimal price, decimal volume, bool isCancelling = false, TimeInForce tif = TimeInForce.PutInQueue)
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str1144);

			//if (volume <= 0)
			//	throw new ArgumentOutOfRangeException("volume", volume, "Объем задан не верно.");

			if (volume == 0)
				volume = _volumeRandom.Next(10, 100);

			return new ExecutionMessage
			{
				Side = side,
				Price = price,
				Volume = volume,
				ExecutionType = ExecutionTypes.OrderLog,
				IsCancelled = isCancelling,
				SecurityId = SecurityId,
				LocalTime = time,
				TimeInForce = tif,
			};
		}

		private bool HasDepth(DateTime time)
		{
			return _lastDepthDate == time.Date;
		}

		/// <summary>
		/// Преобразовать транзакцию.
		/// </summary>
		/// <param name="message">Транзакция.</param>
		/// <param name="quotesVolume">Объем в стакане.</param>
		/// <returns>Поток <see cref="ExecutionMessage"/>.</returns>
		public IEnumerable<ExecutionMessage> ToExecutionLog(OrderMessage message, decimal quotesVolume)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			switch (message.Type)
			{
				case MessageTypes.OrderRegister:
				{
					var regMsg = (OrderRegisterMessage)message;

					if (_settings.IncreaseDepthVolume && NeedCheckVolume(regMsg.Side, regMsg.Price) && quotesVolume < regMsg.Volume)
					{
						foreach (var executionMessage in IncreaseDepthVolume(regMsg.LocalTime, regMsg.Side, regMsg.Volume - quotesVolume))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = regMsg.LocalTime,
						ServerTime = regMsg.LocalTime.ApplyTimeZone(TimeZoneInfo.Local).Convert(_timeZoneInfo),
						SecurityId = regMsg.SecurityId,
						ExecutionType = ExecutionTypes.Order,
						TransactionId = regMsg.TransactionId,
						Price = regMsg.Price,
						Volume = regMsg.Volume,
						Side = regMsg.Side,
						PortfolioName = regMsg.PortfolioName,
						OrderType = regMsg.OrderType,
						UserOrderId = regMsg.UserOrderId
					};

					yield break;
				}
				case MessageTypes.OrderReplace:
				{
					var replaceMsg = (OrderReplaceMessage)message;

					if (_settings.IncreaseDepthVolume && NeedCheckVolume(replaceMsg.Side, replaceMsg.Price) && quotesVolume < replaceMsg.Volume)
					{
						foreach (var executionMessage in IncreaseDepthVolume(replaceMsg.LocalTime, replaceMsg.Side, replaceMsg.Volume - quotesVolume))
							yield return executionMessage;
					}

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = replaceMsg.LocalTime.ApplyTimeZone(TimeZoneInfo.Local).Convert(_timeZoneInfo),
						SecurityId = replaceMsg.SecurityId,
						ExecutionType = ExecutionTypes.Order,
						IsCancelled = true,
						OrderId = replaceMsg.OldOrderId,
						OriginalTransactionId = replaceMsg.OldTransactionId,
						TransactionId = replaceMsg.TransactionId,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						// для старой заявки пользовательский идентификатор менять не надо
						//UserOrderId = replaceMsg.UserOrderId
					};

					yield return new ExecutionMessage
					{
						LocalTime = replaceMsg.LocalTime,
						ServerTime = replaceMsg.LocalTime.ApplyTimeZone(TimeZoneInfo.Local).Convert(_timeZoneInfo),
						SecurityId = replaceMsg.SecurityId,
						ExecutionType = ExecutionTypes.Order,
						TransactionId = replaceMsg.TransactionId,
						Price = replaceMsg.Price,
						Volume = replaceMsg.Volume,
						Side = replaceMsg.Side,
						PortfolioName = replaceMsg.PortfolioName,
						OrderType = replaceMsg.OrderType,
						UserOrderId = replaceMsg.UserOrderId
					};

					yield break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancelMsg = (OrderCancelMessage)message;

					yield return new ExecutionMessage
					{
						ExecutionType = ExecutionTypes.Order,
						IsCancelled = true,
						OrderId = cancelMsg.OrderId,
						TransactionId = cancelMsg.TransactionId,
						OriginalTransactionId = cancelMsg.OrderTransactionId,
						PortfolioName = cancelMsg.PortfolioName,
						SecurityId = cancelMsg.SecurityId,
						LocalTime = cancelMsg.LocalTime,
						ServerTime = cancelMsg.LocalTime.ApplyTimeZone(TimeZoneInfo.Local).Convert(_timeZoneInfo),
						OrderType = cancelMsg.OrderType,
						// при отмене заявки пользовательский идентификатор не меняется
						//UserOrderId = cancelMsg.UserOrderId
					};

					yield break;
				}

				case MessageTypes.OrderPairReplace:
				case MessageTypes.OrderGroupCancel:
					throw new NotSupportedException();

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private decimal? GetBestPrice(Sides orderSide)
		{
			var quotes = orderSide == Sides.Buy ? _asks : _bids;

			var quote = quotes.FirstOrDefault();

			if (quote.Value != null)
				return quote.Key;

			return null;
		}

		private bool NeedCheckVolume(Sides orderSide, decimal price)
		{
			var bestPrice = GetBestPrice(orderSide);

			if (bestPrice == null)
				return false;

			return orderSide == Sides.Buy ? price >= bestPrice.Value : price <= bestPrice.Value;
		}

		private IEnumerable<ExecutionMessage> IncreaseDepthVolume(DateTime time, Sides orderSide, decimal leftVolume)
		{
			var quotes = orderSide == Sides.Buy ? _asks : _bids;
			var quote = quotes.LastOrDefault();

			if(quote.Value == null)
				yield break;

			var side = orderSide.Invert();

			var lastVolume = quote.Value.Second.Volume;
			var lastPrice = quote.Value.Second.Price;

			while (leftVolume > 0 && lastPrice != 0)
			{
				lastVolume *= 2;
				lastPrice += GetPriceStep() * (side == Sides.Buy ? -1 : 1);

				leftVolume -= lastVolume;

				yield return CreateMessage(time, side, lastPrice, lastVolume);
			}
		}

		private decimal GetPriceStep()
		{
			return _securityDefinition.PriceStep ?? 0.01m;
		}

		public void UpdateSecurityDefinition(SecurityMessage securityDefinition)
		{
			if (securityDefinition == null)
				throw new ArgumentNullException("securityDefinition");

			_securityDefinition = securityDefinition;
			_stepsUpdated = true;
		}

		public void UpdateBoardDefinition(BoardMessage message)
		{
			if (message == null)
				throw new ArgumentNullException("message");

			_timeZoneInfo = message.TimeZoneInfo;
		}
	}
}