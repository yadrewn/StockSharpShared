namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Candle states.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum CandleStates
	{
		/// <summary>
		/// Empty state (candle doesn't exist).
		/// </summary>
		[EnumMember]
		None,

		/// <summary>
		/// Candle active.
		/// </summary>
		[EnumMember]
		Active,

		/// <summary>
		/// Candle finished.
		/// </summary>
		[EnumMember]
		Finished,
	}

	/// <summary>
	/// The message contains information about the candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class CandleMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Open time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleOpenTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleOpenTimeKey, true)]
		[MainCategory]
		public DateTimeOffset OpenTime { get; set; }

		/// <summary>
		/// Time of candle high.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleHighTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleHighTimeKey, true)]
		[MainCategory]
		public DateTimeOffset HighTime { get; set; }

		/// <summary>
		/// Time of candle low.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleLowTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleLowTimeKey, true)]
		[MainCategory]
		public DateTimeOffset LowTime { get; set; }

		/// <summary>
		/// Close time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CandleCloseTimeKey)]
		[DescriptionLoc(LocalizedStrings.CandleCloseTimeKey, true)]
		[MainCategory]
		public DateTimeOffset CloseTime { get; set; }

		/// <summary>
		/// Opening price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str79Key)]
		[DescriptionLoc(LocalizedStrings.Str80Key)]
		[MainCategory]
		public decimal OpenPrice { get; set; }

		/// <summary>
		/// Maximum price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str81Key)]
		[DescriptionLoc(LocalizedStrings.Str82Key)]
		[MainCategory]
		public decimal HighPrice { get; set; }

		/// <summary>
		/// Minimum price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str83Key)]
		[DescriptionLoc(LocalizedStrings.Str84Key)]
		[MainCategory]
		public decimal LowPrice { get; set; }

		/// <summary>
		/// Closing price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		[DescriptionLoc(LocalizedStrings.Str86Key)]
		[MainCategory]
		public decimal ClosePrice { get; set; }

		/// <summary>
		/// Volume at open.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? OpenVolume { get; set; }

		/// <summary>
		/// Volume at close.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? CloseVolume { get; set; }

		/// <summary>
		/// Volume at high.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? HighVolume { get; set; }

		/// <summary>
		/// Minimum volume.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? LowVolume { get; set; }

		/// <summary>
		/// Relative colume.
		/// </summary>
		[DataMember]
		public decimal? RelativeVolume { get; set; }

		/// <summary>
		/// Total volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.TotalCandleVolumeKey)]
		[MainCategory]
		public decimal TotalVolume { get; set; }

		/// <summary>
		/// Open interest.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OIKey)]
		[DescriptionLoc(LocalizedStrings.OpenInterestKey)]
		[MainCategory]
		public decimal? OpenInterest { get; set; }

		/// <summary>
		/// Number of ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TicksKey)]
		[DescriptionLoc(LocalizedStrings.TickCountKey)]
		[MainCategory]
		public int? TotalTicks { get; set; }

		/// <summary>
		/// Number of uptrending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickUpKey)]
		[DescriptionLoc(LocalizedStrings.TickUpCountKey)]
		[MainCategory]
		public int? UpTicks { get; set; }

		/// <summary>
		/// Number of downtrending ticks.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TickDownKey)]
		[DescriptionLoc(LocalizedStrings.TickDownCountKey)]
		[MainCategory]
		public int? DownTicks { get; set; }

		/// <summary>
		/// State.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.CandleStateKey)]
		[MainCategory]
		public CandleStates State { get; set; }

		/// <summary>
		/// ID of the original message <see cref="MarketDataMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// It is the last message in the requested batch of candles.
		/// </summary>
		[DataMember]
		public bool IsFinished { get; set; }

		/// <summary>
		/// Candle arg.
		/// </summary>
		public abstract object Arg { get; set; }

		/// <summary>
		/// Initialize <see cref="CandleMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected CandleMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Copy parameters.
		/// </summary>
		/// <param name="copy">Copy.</param>
		/// <returns>Copy.</returns>
		protected CandleMessage CopyTo(CandleMessage copy)
		{
			if (copy == null)
				throw new ArgumentNullException("copy");

			copy.LocalTime = LocalTime;
			copy.ClosePrice = ClosePrice;
			copy.CloseTime = CloseTime;
			copy.CloseVolume = CloseVolume;
			copy.HighPrice = HighPrice;
			copy.HighVolume = HighVolume;
			copy.LowPrice = LowPrice;
			copy.LowVolume = LowVolume;
			copy.OpenInterest = OpenInterest;
			copy.OpenPrice = OpenPrice;
			copy.OpenTime = OpenTime;
			copy.OpenVolume = OpenVolume;
			copy.SecurityId = SecurityId;
			copy.TotalVolume = TotalVolume;
			copy.RelativeVolume = RelativeVolume;
			copy.OriginalTransactionId = OriginalTransactionId;
			copy.DownTicks = DownTicks;
			copy.UpTicks = UpTicks;
			copy.TotalTicks = TotalTicks;
			copy.IsFinished = IsFinished;

			return copy;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0},T={1:yyyy/MM/dd HH:mm:ss.fff},O={2},H={3},L={4},C={5},V={6}".Put(Type, OpenTime, OpenPrice, HighPrice, LowPrice, ClosePrice, TotalVolume);
		}
	}

	/// <summary>
	/// The message contains information about the time-frame candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class TimeFrameCandleMessage : CandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		public TimeFrameCandleMessage()
			: base(MessageTypes.CandleTimeFrame)
		{
		}

		/// <summary>
		/// Time-frame.
		/// </summary>
		[DataMember]
		public TimeSpan TimeFrame { get; set; }

		/// <summary>
		/// Create a copy of <see cref="TimeFrameCandleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new TimeFrameCandleMessage
			{
				TimeFrame = TimeFrame
			});
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return TimeFrame; }
			set { TimeFrame = (TimeSpan)value; }
		}
	}

	/// <summary>
	/// The message contains information about the tick candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class TickCandleMessage : CandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TickCandleMessage"/>.
		/// </summary>
		public TickCandleMessage()
			: base(MessageTypes.CandleTick)
		{
		}

		/// <summary>
		/// Maximum tick count.
		/// </summary>
		[DataMember]
		public int MaxTradeCount { get; set; }

		/// <summary>
		/// Create a copy of <see cref="TickCandleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new TickCandleMessage
			{
				MaxTradeCount = MaxTradeCount
			});
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return MaxTradeCount; }
			set { MaxTradeCount = (int)value; }
		}
	}

	/// <summary>
	/// The message contains information about the volume candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class VolumeCandleMessage : CandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeCandleMessage"/>.
		/// </summary>
		public VolumeCandleMessage()
			: base(MessageTypes.CandleVolume)
		{
		}

		/// <summary>
		/// Maximum volume.
		/// </summary>
		[DataMember]
		public decimal Volume { get; set; }

		/// <summary>
		/// Create a copy of <see cref="VolumeCandleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new VolumeCandleMessage
			{
				Volume = Volume
			});
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return Volume; }
			set { Volume = (decimal)value; }
		}
	}

	/// <summary>
	/// The message contains information about the range candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class RangeCandleMessage : CandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RangeCandleMessage"/>.
		/// </summary>
		public RangeCandleMessage()
			: base(MessageTypes.CandleRange)
		{
		}

		/// <summary>
		/// Range of price.
		/// </summary>
		[DataMember]
		public Unit PriceRange { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RangeCandleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new RangeCandleMessage
			{
				PriceRange = PriceRange
			});
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return PriceRange; }
			set { PriceRange = (Unit)value; }
		}
	}

	/// <summary>
	/// Symbol types.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum PnFTypes
	{
		/// <summary>
		/// X (price up).
		/// </summary>
		[EnumMember]
		X,

		/// <summary>
		/// 0 (price down).
		/// </summary>
		[EnumMember]
		O,
	}

	/// <summary>
	/// Point in fugure (X0) candle arg.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class PnFArg : Equatable<PnFArg>
	{
		private Unit _boxSize = new Unit();

		/// <summary>
		/// Range of price above which create a new <see cref="PnFTypes.X"/> or <see cref="PnFTypes.O"/>.
		/// </summary>
		[DataMember]
		public Unit BoxSize
		{
			get { return _boxSize; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_boxSize = value;
			}
		}

		/// <summary>
		/// The number of boxes (an <see cref="PnFTypes.X"/> or an <see cref="PnFTypes.O"/>) required to cause a reversal.
		/// </summary>
		[DataMember]
		public int ReversalAmount { get; set; }

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "Box = {0} RA = {1}".Put(BoxSize, ReversalAmount);
		}

		/// <summary>
		/// Create a copy of <see cref="PnFArg"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override PnFArg Clone()
		{
			return new PnFArg
			{
				BoxSize = BoxSize.Clone(),
				ReversalAmount = ReversalAmount,
			};
		}

		/// <summary>
		/// Compare <see cref="PnFArg"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(PnFArg other)
		{
			return other.BoxSize == BoxSize && other.ReversalAmount == ReversalAmount;
		}

		/// <summary>
		/// Get the hash code of the object <see cref="PnFArg"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return BoxSize.GetHashCode() ^ ReversalAmount.GetHashCode();
		}
	}

	/// <summary>
	/// The message contains information about the XO candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class PnFCandleMessage : CandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PnFCandleMessage"/>.
		/// </summary>
		public PnFCandleMessage()
			: base(MessageTypes.CandlePnF)
		{
		}

		/// <summary>
		/// Value of arguments.
		/// </summary>
		[DataMember]
		public PnFArg PnFArg { get; set; }

		/// <summary>
		/// Type of symbols.
		/// </summary>
		[DataMember]
		public PnFTypes PnFType { get; set; }

		/// <summary>
		/// Create a copy of <see cref="PnFCandleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new PnFCandleMessage
			{
				PnFArg = PnFArg,
				PnFType = PnFType
			});
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return PnFArg; }
			set { PnFArg = (PnFArg)value; }
		}
	}

	/// <summary>
	/// The message contains information about the renko candle.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class RenkoCandleMessage : CandleMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RenkoCandleMessage"/>.
		/// </summary>
		public RenkoCandleMessage()
			: base(MessageTypes.CandleRenko)
		{
		}

		/// <summary>
		/// Possible price change range.
		/// </summary>
		[DataMember]
		public Unit BoxSize { get; set; }

		/// <summary>
		/// Create a copy of <see cref="RenkoCandleMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new RenkoCandleMessage
			{
				BoxSize = BoxSize
			});
		}

		/// <summary>
		/// Candle arg.
		/// </summary>
		public override object Arg
		{
			get { return BoxSize; }
			set { BoxSize = (Unit)value; }
		}
	}
}