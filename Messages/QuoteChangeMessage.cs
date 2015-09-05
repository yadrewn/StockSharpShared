namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Messages containing quotes.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public sealed class QuoteChangeMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		private IEnumerable<QuoteChange> _bids = Enumerable.Empty<QuoteChange>();

		/// <summary>
		/// Quotes to buy.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str281Key)]
		[DescriptionLoc(LocalizedStrings.Str282Key)]
		[MainCategory]
		public IEnumerable<QuoteChange> Bids
		{
			get { return _bids; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_bids = value;
			}
		}

		private IEnumerable<QuoteChange> _asks = Enumerable.Empty<QuoteChange>();

		/// <summary>
		/// Quotes to sell.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str283Key)]
		[DescriptionLoc(LocalizedStrings.Str284Key)]
		[MainCategory]
		public IEnumerable<QuoteChange> Asks
		{
			get { return _asks; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_asks = value;
			}
		}

		/// <summary>
		/// Change server time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ServerTimeKey)]
		[DescriptionLoc(LocalizedStrings.Str168Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Flag sorted by price quotes (<see cref="QuoteChangeMessage.Bids"/> by descending, <see cref="QuoteChangeMessage.Asks"/> by ascending).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str285Key)]
		[DescriptionLoc(LocalizedStrings.Str285Key, true)]
		[MainCategory]
		public bool IsSorted { get; set; }

		/// <summary>
		/// Trading security currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str382Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="QuoteChangeMessage"/>.
		/// </summary>
		public QuoteChangeMessage()
			: base(MessageTypes.QuoteChange)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="QuoteChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new QuoteChangeMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				Bids = Bids.Select(q => q.Clone()).ToArray(),
				Asks = Asks.Select(q => q.Clone()).ToArray(),
				ServerTime = ServerTime,
				IsSorted = IsSorted,
				Currency = Currency,
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",T(S)={0:yyyy/MM/dd HH:mm:ss.fff}".Put(ServerTime);
		}
	}
}