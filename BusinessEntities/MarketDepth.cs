namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.Linq;
	using System.Xml.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Order book.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	//[EntityFactory(typeof(UnitializedEntityFactory<MarketDepth>))]
	public class MarketDepth : Cloneable<MarketDepth>, IEnumerable<Quote>, ISynchronizedCollection
	{
		private readonly SyncObject _syncRoot = new SyncObject();

		/// <summary>
		/// Create order book.
		/// </summary>
		/// <param name="security">Security.</param>
		public MarketDepth(Security security)
		{
			if (ReferenceEquals(security, null))
				throw new ArgumentNullException("security");

			Security = security;
			_bids = _asks = ArrayHelper.Empty<Quote>();
		}

		private int _maxDepth = 100;

		/// <summary>
		/// The maximum depth of order book.
		/// </summary>
		/// <remarks>
		/// The default value is 100. If the exceeded the maximum depth the event <see cref="MarketDepth.QuoteOutOfDepth"/> will triggered.
		/// </remarks>
		public int MaxDepth
		{
			get { return _maxDepth; }
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException("value", value, LocalizedStrings.Str480);

				_maxDepth = value;

				Truncate(Bids, Asks, default(DateTimeOffset));
			}
		}

		/// <summary>
		/// Security.
		/// </summary>
		public Security Security { get; private set; }

		[field: NonSerialized]
		private IConnector _connector;

		/// <summary>
		/// Connection to the trading system.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Obsolete("Security.Connector устарел и всегда равен null.")]
		public IConnector Connector
		{
			get { return _connector; }
			set { _connector = value; }
		}

		/// <summary>
		/// Automatically check for quotes by <see cref="Verify()"/>.
		/// </summary>
		/// <remarks>
		/// The default is disabled for performance.
		/// </remarks>
		public bool AutoVerify { get; set; }

		/// <summary>
		/// Whether to use aggregated quotes <see cref="AggregatedQuote"/> at the join of the volumes with the same price.
		/// </summary>
		/// <remarks>
		/// The default is disabled for performance.
		/// </remarks>
		public bool UseAggregatedQuotes { get; set; }

		/// <summary>
		/// Last change time.
		/// </summary>
		public DateTimeOffset LastChangeTime { get; set; }

		/// <summary>
		/// The order book local time stamp.
		/// </summary>
		public DateTime LocalTime { get; set; }

		// TODO
		//private Quote[] _bidsCache;
		private Quote[] _bids;

		/// <summary>
		/// Get the array of bids sorted by descending price. The first (best) bid will be the maximum price.
		/// </summary>
		public Quote[] Bids 
		{
			get
			{
				return _bids;
				//lock (_syncRoot)
				//{
				//    return _bidsCache ?? (_bidsCache = _bids.Select(q => q.Clone()).ToArray());
				//}
			}
			private set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_bids = value;
				//_bidsCache = null;
			}
		}

		//private Quote[] _asksCache;
		private Quote[] _asks;

		/// <summary>
		/// Get the array of asks sorted by ascending price. The first (best) ask will be the minimum price.
		/// </summary>
		public Quote[] Asks 
		{ 
			get
			{
				return _asks;
				//lock (_syncRoot)
				//{
				//    return _asksCache ?? (_asksCache = _asks.Select(q => q.Clone()).ToArray());
				//}
			}
			private set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_asks = value;
				//_asksCache = null;
			}
		}

		/// <summary>
		/// Trading security currency.
		/// </summary>
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// The best bid. If the order book does not contain bids, will be returned <see langword="null" />.
		/// </summary>
		public Quote BestBid { get; private set; }

		/// <summary>
		/// The best ask. If the order book does not contain asks, will be returned <see langword="null" />.
		/// </summary>
		public Quote BestAsk { get; private set; }

		/// <summary>
		/// The best pair. If the order book is empty, will be returned <see langword="null" />.
		/// </summary>
		public MarketDepthPair BestPair
		{
			get { return GetPair(0); }
		}

		/// <summary>
		/// To get the total price size by bids.
		/// </summary>
		public decimal TotalBidsPrice
		{
			get
			{
				lock (_syncRoot)
					return _bids.Length > 0 ? Security.ShrinkPrice(_bids.Sum(b => b.Price)) : 0;
			}
		}

		/// <summary>
		/// To get the total price size by offers.
		/// </summary>
		public decimal TotalAsksPrice
		{
			get
			{
				lock (_syncRoot)
					return _asks.Length > 0 ? Security.ShrinkPrice(_asks.Sum(a => a.Price)) : 0;
			}
		}

		/// <summary>
		/// Get bids total volume.
		/// </summary>
		public decimal TotalBidsVolume
		{
			get
			{
				lock (_syncRoot)
					return _bids.Sum(b => b.Volume);
			}
		}

		/// <summary>
		/// Get asks total volume.
		/// </summary>
		public decimal TotalAsksVolume
		{
			get
			{
				lock (_syncRoot)
					return _asks.Sum(a => a.Volume);
			}
		}

		/// <summary>
		/// Get total volume.
		/// </summary>
		public decimal TotalVolume
		{
			get
			{
				lock (_syncRoot)
					return TotalBidsVolume + TotalAsksVolume;
			}
		}

		/// <summary>
		/// To get the total price size.
		/// </summary>
		public decimal TotalPrice
		{
			get
			{
				lock (_syncRoot)
					return TotalBidsPrice + TotalAsksPrice;
			}
		}

		/// <summary>
		/// Total quotes count (bids + asks).
		/// </summary>
		public int Count
		{
			get
			{
				lock (_syncRoot)
					return _bids.Length + _asks.Length;
			}
		}

		private int _depth;

		/// <summary>
		/// Depth of book.
		/// </summary>
		public int Depth
		{
			get { return _depth; }
			private set
			{
				if (_depth == value)
					return;

				_depth = value;
				DepthChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Event on exceeding the maximum allowable depth of quotes.
		/// </summary>
		public event Action<Quote> QuoteOutOfDepth;

		/// <summary>
		/// Depth <see cref="MarketDepth.Depth"/> changed.
		/// </summary>
		public event Action DepthChanged;

		/// <summary>
		/// Quotes changed.
		/// </summary>
		public event Action QuotesChanged;

		/// <summary>
		/// To reduce the order book to the required depth.
		/// </summary>
		/// <param name="newDepth">New order book depth.</param>
		public void Decrease(int newDepth)
		{
			var currentDepth = Depth;

			if (newDepth < 0)
				throw new ArgumentOutOfRangeException("newDepth", newDepth, LocalizedStrings.Str481);
			else if (newDepth > currentDepth)
				throw new ArgumentOutOfRangeException("newDepth", newDepth, LocalizedStrings.Str482Params.Put(currentDepth));

			lock (_syncRoot)
			{
				Bids = Decrease(_bids, newDepth);
				Asks = Decrease(_asks, newDepth);

				UpdateDepthAndTime();
			}

			RaiseQuotesChanged();
		}

		private static Quote[] Decrease(Quote[] quotes, int newDepth)
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			if (newDepth <= quotes.Length)
				Array.Resize(ref quotes, newDepth);

			return quotes;
		}

		/// <summary>
		/// To get a quote by the direction <see cref="Sides"/> and the depth index.
		/// </summary>
		/// <param name="orderDirection">Orders side.</param>
		/// <param name="depthIndex">Depth index. Zero index means the best quote.</param>
		/// <returns>Quote. If a quote does not exist for specified depth, then the <see langword="null" /> will be returned.</returns>
		public Quote GetQuote(Sides orderDirection, int depthIndex)
		{
			lock (_syncRoot)
				return GetQuotesInternal(orderDirection).ElementAtOrDefault(depthIndex);
		}

		/// <summary>
		/// To get a quote by the price.
		/// </summary>
		/// <param name="price">Quote price.</param>
		/// <returns>Found quote. If there is no quote in the order book for the passed price, then the <see langword="null" /> will be returned.</returns>
		public Quote GetQuote(decimal price)
		{
			var quotes = GetQuotes(price);
			var i = GetQuoteIndex(quotes, price);
			return i < 0 ? null : quotes[i];
		}

		/// <summary>
		/// To get quotes by the direction <see cref="Sides"/>.
		/// </summary>
		/// <param name="orderDirection">Orders side.</param>
		/// <returns>Quotes.</returns>
		public Quote[] GetQuotes(Sides orderDirection)
		{
			return orderDirection == Sides.Buy ? Bids : Asks;
		}

		/// <summary>
		/// To get the best quote by the direction <see cref="Sides"/>.
		/// </summary>
		/// <param name="orderDirection">Order side.</param>
		/// <returns>The best quote. If the order book is empty, then the <see langword="null" /> will be returned.</returns>
		public Quote GetBestQuote(Sides orderDirection)
		{
			return orderDirection == Sides.Buy ? BestBid : BestAsk;
		}

		/// <summary>
		/// To get a pair of quotes (bid + offer) by the depth index.
		/// </summary>
		/// <param name="depthIndex">Depth index. Zero index means the best pair of quotes.</param>
		/// <returns>The pair of quotes. If the index is larger than book order depth <see cref="MarketDepth.Depth"/>, then the <see langword="null" /> is returned.</returns>
		public MarketDepthPair GetPair(int depthIndex)
		{
			if (depthIndex < 0)
				throw new ArgumentOutOfRangeException("depthIndex", depthIndex, LocalizedStrings.Str483);

			lock (_syncRoot)
			{
				var bid = GetQuote(Sides.Buy, depthIndex);
				var ask = GetQuote(Sides.Sell, depthIndex);

				if (bid == null && ask == null)
					return null;
				
				return new MarketDepthPair(Security, bid, ask);
			}
		}

		/// <summary>
		/// To get a pair of quotes for a given book depth.
		/// </summary>
		/// <param name="depth">Book depth. The counting is from the best quotes.</param>
		/// <returns>Spread.</returns>
		public IEnumerable<MarketDepthPair> GetTopPairs(int depth)
		{
			if (depth < 0)
				throw new ArgumentOutOfRangeException("depth", depth, LocalizedStrings.Str484);

			var retVal = new List<MarketDepthPair>();

			lock (_syncRoot)
			{
				for (var i = 0; i < depth; i++)
				{
					var single = GetPair(i);

					if (single != null)
						retVal.Add(single);
					else
						break;
				}
			}

			return retVal;
		}

		/// <summary>
		/// To get quotes for a given book depth.
		/// </summary>
		/// <param name="depth">Book depth. Quotes are in order of price increasing from bids to offers.</param>
		/// <returns>Spread.</returns>
		public IEnumerable<Quote> GetTopQuotes(int depth)
		{
			if (depth < 0)
				throw new ArgumentOutOfRangeException("depth", depth, LocalizedStrings.Str484);

			var retVal = new List<Quote>();

			lock (_syncRoot)
			{
				for (var i = depth - 1; i >= 0; i--)
				{
					var single = GetQuote(Sides.Buy, i);

					if (single != null)
						retVal.Add(single);
				}

				for (var i = 0; i < depth; i++)
				{
					var single = GetQuote(Sides.Sell, i);

					if (single != null)
						retVal.Add(single);
					else
						break;
				}
			}

			return retVal;
		}

		/// <summary>
		/// Update the order book by new quotes.
		/// </summary>
		/// <param name="quotes">The new quotes.</param>
		/// <param name="lastChangeTime">Last change time.</param>
		/// <returns>Market depth.</returns>
		/// <remarks>
		/// The old quotes will be removed from the book.
		/// </remarks>
		public MarketDepth Update(IEnumerable<Quote> quotes, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (quotes == null)
				throw new ArgumentNullException("quotes");

			var bids = Enumerable.Empty<Quote>();
			var asks = Enumerable.Empty<Quote>();

			foreach (var group in quotes.GroupBy(q => q.OrderDirection))
			{
				if (group.Key == Sides.Buy)
					bids = group;
				else
					asks = group;
			}

			return Update(bids, asks, false, lastChangeTime);
		}

		/// <summary>
		/// Update the order book by new bids and asks.
		/// </summary>
		/// <param name="bids">The new bids.</param>
		/// <param name="asks">The new asks.</param>
		/// <param name="isSorted">Are quotes sorted. This parameter is used for optimization in order to prevent re-sorting.</param>
		/// <param name="lastChangeTime">Last change time.</param>
		/// <returns>Market depth.</returns>
		/// <remarks>
		/// The old quotes will be removed from the book.
		/// </remarks>
		public MarketDepth Update(IEnumerable<Quote> bids, IEnumerable<Quote> asks, bool isSorted = false, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (bids == null)
				throw new ArgumentNullException("bids");

			if (asks == null)
				throw new ArgumentNullException("asks");

			if (!isSorted)
			{
				bids = bids.OrderBy(q => 0 - q.Price);
				asks = asks.OrderBy(q => q.Price);
			}

			bids = bids.ToArray();
			asks = asks.ToArray();

			if (AutoVerify)
			{
				if (!Verify(bids, asks))
					throw new ArgumentException(LocalizedStrings.Str485);
			}

			Truncate((Quote[])bids, (Quote[])asks, lastChangeTime);
			return this;
		}

		private void Truncate(Quote[] bids, Quote[] asks, DateTimeOffset lastChangeTime)
		{
			Quote[] outOfRangeBids;
			Quote[] outOfRangeAsks;

			lock (_syncRoot)
			{
				Update(Truncate(bids, out outOfRangeBids), Truncate(asks, out outOfRangeAsks), lastChangeTime);
			}

			var evt = QuoteOutOfDepth;

			if (evt != null)
			{
				if (outOfRangeBids != null)
					outOfRangeBids.ForEach(evt);

				if (outOfRangeAsks != null)
					outOfRangeAsks.ForEach(evt);
			}
		}

		private Quote[] Truncate(Quote[] quotes, out Quote[] outOfRangeQuotes)
		{
			if (quotes.Length > MaxDepth)
			{
				outOfRangeQuotes = new Quote[quotes.Length - MaxDepth];
				Array.Copy(quotes, MaxDepth, outOfRangeQuotes, 0, outOfRangeQuotes.Length);

				Array.Resize(ref quotes, MaxDepth);
			}
			else
			{
				outOfRangeQuotes = null;
			}

			return quotes;
		}

		/// <summary>
		/// To update the order book. The version without checks and blockings.
		/// </summary>
		/// <param name="bids">Sorted bids.</param>
		/// <param name="asks">Sorted asks.</param>
		/// <param name="lastChangeTime">Change time.</param>
		public void Update(Quote[] bids, Quote[] asks, DateTimeOffset lastChangeTime)
		{
			//_bidsCache = null;
			//_asksCache = null;

			_bids = bids;
			_asks = asks;

			UpdateDepthAndTime(lastChangeTime, false);

			if (null != QuotesChanged)
				QuotesChanged();
			//RaiseQuotesChanged();
		}

		/// <summary>
		/// To refresh the quote. If a quote with the same price is already in the order book, it is updated as passed. Otherwise, it automatically rebuilds the order book.
		/// </summary>
		/// <param name="quote">The new quote.</param>
		public void UpdateQuote(Quote quote)
		{
			SetQuote(quote, false);
		}

		/// <summary>
		/// Add buy quote.
		/// </summary>
		/// <param name="price">Buy price.</param>
		/// <param name="volume">Buy volume.</param>
		public void AddBid(decimal price, decimal volume)
		{
			AddQuote(new Quote
			{
				Security = Security,
				Price = price,
				Volume = volume,
				OrderDirection = Sides.Buy,
			});
		}

		/// <summary>
		/// Add sell quote.
		/// </summary>
		/// <param name="price">Sell price.</param>
		/// <param name="volume">Sell volume.</param>
		public void AddAsk(decimal price, decimal volume)
		{
			AddQuote(new Quote
			{
				Security = Security,
				Price = price,
				Volume = volume,
				OrderDirection = Sides.Sell,
			});
		}

		/// <summary>
		/// To add the quote. If a quote with the same price is already in the order book, they are combined into the <see cref="AggregatedQuote"/>.
		/// </summary>
		/// <param name="quote">The new quote.</param>
		public void AddQuote(Quote quote)
		{
			SetQuote(quote, true);
		}

		private void SetQuote(Quote quote, bool isAggregate)
		{
			CheckQuote(quote);

			Quote outOfDepthQuote = null;

			lock (_syncRoot)
			{
				var quotes = GetQuotes(quote.OrderDirection);

				var index = GetQuoteIndex(quotes, quote.Price);

				if (index != -1)
				{
					if (isAggregate)
					{
						var existedQuote = quotes[index];

						if (UseAggregatedQuotes)
						{
							var aggQuote = existedQuote as AggregatedQuote;

							if (aggQuote == null)
							{
								aggQuote = new AggregatedQuote
								{
									Price = quote.Price,
									Security = quote.Security,
									OrderDirection = quote.OrderDirection
								};

								aggQuote.InnerQuotes.Add(existedQuote);

								quotes[index] = aggQuote;
							}

							aggQuote.InnerQuotes.Add(quote);
						}
						else
							existedQuote.Volume += quote.Volume;
					}
					else
					{
						quotes[index] = quote;
					}
				}
				else
				{
					for (index = 0; index < quotes.Length; index++)
					{
						var currentPrice = quotes[index].Price;

						if (quote.OrderDirection == Sides.Buy)
						{
							if (quote.Price > currentPrice)
								break;
						}
						else
						{
							if (quote.Price < currentPrice)
								break;
						}
					}

					Array.Resize(ref quotes, quotes.Length + 1);

					if (index < (quotes.Length - 1))
						Array.Copy(quotes, index, quotes, index + 1, quotes.Length - 1 - index);

					quotes[index] = quote;

					if (quotes.Length > MaxDepth)
					{
						outOfDepthQuote = quotes[quotes.Length - 1];
						quotes = RemoveAt(quotes, quotes.Length - 1);
					}

					if (quote.OrderDirection == Sides.Buy)
						Bids = quotes;
					else
						Asks = quotes;
				}

				UpdateDepthAndTime();

				if (quotes.Length > MaxDepth)
					throw new InvalidOperationException(LocalizedStrings.Str486Params.Put(MaxDepth, quotes.Length));
			}

			RaiseQuotesChanged();

			if (outOfDepthQuote != null)
				QuoteOutOfDepth.SafeInvoke(outOfDepthQuote);
		}

		#region IEnumerable<Quote>

		/// <summary>
		/// To get the enumerator object.
		/// </summary>
		/// <returns>The enumerator object.</returns>
		public IEnumerator<Quote> GetEnumerator()
		{
			return this.SyncGet(c => Bids.Reverse().Concat(Asks)).Cast<Quote>().GetEnumerator();
		}

		/// <summary>
		/// To get the enumerator object.
		/// </summary>
		/// <returns>The enumerator object.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		/// <summary>
		/// To get all pairs from the order book.
		/// </summary>
		/// <returns>Pairs from which the order book is composed.</returns>
		public IEnumerable<MarketDepthPair> ToPairs()
		{
			return GetTopPairs(Depth);
		}

		/// <summary>
		/// Remove the quote.
		/// </summary>
		/// <param name="quote">The quote to remove.</param>
		/// <param name="lastChangeTime">Order book change time.</param>
		public void Remove(Quote quote, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			Remove(quote.OrderDirection, quote.Price, quote.Volume, lastChangeTime);
		}

		/// <summary>
		/// Remove the volume for the price.
		/// </summary>
		/// <param name="price">Remove the quote for the price.</param>
		/// <param name="volume">The volume to be deleted. If it is not specified, then all the quote is removed.</param>
		/// <param name="lastChangeTime">Order book change time.</param>
		public void Remove(decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			lock (_syncRoot)
			{
				var dir = GetDirection(price);

				if (dir == null)
					throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str487);

				Remove((Sides)dir, price, volume, lastChangeTime);
			}
		}

		/// <summary>
		/// Remove the volume for the price.
		/// </summary>
		/// <param name="direction">Order side.</param>
		/// <param name="price">Remove the quote for the price.</param>
		/// <param name="volume">The volume to be deleted. If it is not specified, then all the quote is removed.</param>
		/// <param name="lastChangeTime">Order book change time.</param>
		public void Remove(Sides direction, decimal price, decimal volume = 0, DateTimeOffset lastChangeTime = default(DateTimeOffset))
		{
			if (price <= 0)
				throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str488);

			if (volume < 0)
				throw new ArgumentOutOfRangeException("volume", volume, LocalizedStrings.Str489);

			lock (_syncRoot)
			{
				var quotes = GetQuotesInternal(direction);
				var index = GetQuoteIndex(quotes, price);

				if (index == -1)
					throw new ArgumentOutOfRangeException("price", price, LocalizedStrings.Str487);

				var quote = quotes[index];

				decimal leftVolume;

				if (volume > 0)
				{
					if (quote.Volume < volume)
						throw new ArgumentOutOfRangeException("volume", volume, LocalizedStrings.Str490Params.Put(quote));

					leftVolume = quote.Volume - volume;

					if (UseAggregatedQuotes)
					{
						var aggQuote = quote as AggregatedQuote;

						if (aggQuote != null)
						{
							while (volume > 0)
							{
								var innerQuote = aggQuote.InnerQuotes.First();

								if (innerQuote.Volume > volume)
								{
									innerQuote.Volume -= volume;
									break;
								}
								else
								{
									aggQuote.InnerQuotes.Remove(innerQuote);
									volume -= innerQuote.Volume;
								}
							}
						}
					}
				}
				else
					leftVolume = 0;

				if (leftVolume == 0)
				{
					quotes = RemoveAt(quotes, index);

					if (quote.OrderDirection == Sides.Buy)
						Bids = quotes;
					else
						Asks = quotes;

					UpdateDepthAndTime(lastChangeTime);
				}
				else
				{
					quote.Volume = leftVolume;
					UpdateTime(lastChangeTime);
				}
			}

			RaiseQuotesChanged();
		}

		private static Quote[] RemoveAt(Quote[] quotes, int index)
		{
			var newQuotes = new Quote[quotes.Length - 1];

			if (index > 0)
				Array.Copy(quotes, 0, newQuotes, 0, index);

			if (index < (quotes.Length - 1))
				Array.Copy(quotes, index + 1, newQuotes, index, quotes.Length - index - 1);

			return newQuotes;
		}

		private static int GetQuoteIndex(Quote[] quotes, decimal price)
		{
			var stop = quotes.Length - 1;
			if (stop < 0)
				return -1;

			var first = quotes[0];

			var cmp = decimal.Compare(price, first.Price);
			if (cmp == 0)
				return 0;

			var last = quotes[stop];
			var desc = first.Price - last.Price > 0m;

			if (desc)
				cmp = -cmp;

			if (cmp < 0)
				return -1;

			cmp = decimal.Compare(price, last.Price);

			if (desc)
				cmp = -cmp;

			if (cmp > 0)
				return -1;

			if (cmp == 0)
				return stop;

			var start = 0;

			while (stop - start >= 0)
			{
				var mid = (start + stop) >> 1;

				cmp = decimal.Compare(price, quotes[mid].Price);

				if (desc)
					cmp = -cmp;
				if (cmp > 0)
					start = mid + 1;
				else if (cmp < 0)
					stop = mid - 1;
				else
					return mid;
			}

			return -1;
		}

		private Quote[] GetQuotesInternal(Sides direction)
		{
			return direction == Sides.Buy ? _bids : _asks;
		}

		private Quote[] GetQuotes(decimal price)
		{
			var dir = GetDirection(price);

			if (dir == null)
				return ArrayHelper.Empty<Quote>();
			else
				return dir == Sides.Buy ? _bids : _asks;
		}

		private Sides? GetDirection(decimal price)
		{
			if (!ReferenceEquals(BestBid, null) && BestBid.Price >= price)
				return Sides.Buy;
			else if (!ReferenceEquals(BestAsk, null) && BestAsk.Price <= price)
				return Sides.Sell;
			else
				return null;
		}

		private void CheckQuote(Quote quote)
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			if (quote.Security != null && quote.Security != Security)
				throw new ArgumentException(LocalizedStrings.Str491Params.Put(quote.Security.Id, Security.Id), "quote");

			if (quote.Security == null)
				quote.Security = Security;

			if (quote.Price <= 0)
				throw new ArgumentOutOfRangeException("quote", quote.Price, LocalizedStrings.Str488);

			if (quote.Volume < 0)
				throw new ArgumentOutOfRangeException("quote", quote.Volume, LocalizedStrings.Str489);
		}

		private void UpdateDepthAndTime(DateTimeOffset lastChangeTime = default(DateTimeOffset), bool depthChangedEventNeeded = true)
		{
			if (depthChangedEventNeeded)
			{
				Depth = _bids.Length > _asks.Length ? _bids.Length : _asks.Length;
			}
			else
			{
				_depth = _bids.Length > _asks.Length ? _bids.Length : _asks.Length;
			}

			BestBid = _bids.Length > 0 ? _bids[0] : null;
			BestAsk = _asks.Length > 0 ? _asks[0] : null;

			UpdateTime(lastChangeTime);
		}

		private void UpdateTime(DateTimeOffset lastChangeTime)
		{
			if (lastChangeTime != default(DateTimeOffset))
			{
				LastChangeTime = lastChangeTime;
			}
		}

		private void RaiseQuotesChanged()
		{
			QuotesChanged.SafeInvoke();
		}

		/// <summary>
		/// Create a copy of <see cref="MarketDepth"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override MarketDepth Clone()
		{
			var clone = new MarketDepth(Security)
			{
				MaxDepth = MaxDepth,
				UseAggregatedQuotes = UseAggregatedQuotes,
				AutoVerify = AutoVerify,
				Currency = Currency,
			};

			lock (_syncRoot)
			{
				clone.Update(_bids.Select(q => q.Clone()), _asks.Select(q => q.Clone()), true, LastChangeTime);
				clone.LocalTime = LocalTime;
			}

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return this.Select(q => q.ToString()).Join(Environment.NewLine);
		}

		/// <summary>
		/// To determine whether the order book is in the right state.
		/// </summary>
		/// <returns><see langword="true" />, if the order book contains correct data, otherwise <see langword="false" />.</returns>
		/// <remarks>
		/// It is used in cases when the trading system by mistake sends the wrong quotes.
		/// </remarks>
		public bool Verify()
		{
			lock (_syncRoot)
				return Verify(_bids, _asks);
		}

		private bool Verify(IEnumerable<Quote> bids, IEnumerable<Quote> asks)
		{
			var bestBid = bids.FirstOrDefault();
			var bestAsk = asks.FirstOrDefault();

			if (bestBid != null && bestAsk != null)
			{
				return bids.All(b => b.Price < bestAsk.Price) && asks.All(a => a.Price > bestBid.Price) && Verify(bids, true) && Verify(asks, false);
			}
			else
			{
				return Verify(bids, true) && Verify(asks, false);
			}
		}

		private bool Verify(IEnumerable<Quote> quotes, bool isBids)
		{
			if (quotes.IsEmpty())
				return true;

			if (quotes.Any(q => !Verify(q, isBids)))
				return false;

			if (quotes.GroupBy(q => q.Price).Any(g => g.Count() > 1))
				return false;

			var prev = quotes.First();

			foreach (var current in quotes.Skip(1))
			{
				if (isBids)
				{
					if (current.Price > prev.Price)
						return false;
				}
				else
				{
					if (current.Price < prev.Price)
						return false;
				}

				prev = current;
			}

			return true;
		}

		private bool Verify(Quote quote, bool isBids)
		{
			if (quote == null)
				throw new ArgumentNullException("quote");

			return
				quote.Price > 0 &&
				quote.Volume > 0 &&
				quote.OrderDirection == (isBids ? Sides.Buy : Sides.Sell) &&
				quote.Security == Security;
		}

		SyncObject ISynchronizedCollection.SyncRoot
		{
			get { return _syncRoot; }
		}
	}
}