namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The base class describes the cash position and the position on the instrument.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public abstract class BasePosition : NotifiableObject, IExtendableEntity
	{
		/// <summary>
		/// Initialize <see cref="BasePosition"/>.
		/// </summary>
		protected BasePosition()
		{
		}

		private decimal _beginValue;

		/// <summary>
		/// Position size at the beginning of the trading session.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str253Key)]
		[DescriptionLoc(LocalizedStrings.Str424Key)]
		[StatisticsCategory]
		public decimal BeginValue
		{
			get { return _beginValue; }
			set
			{
				if (_beginValue == value)
					return;

				_beginValue = value;
				NotifyChanged("BeginValue");
			}
		}

		private decimal _currentValue;

		/// <summary>
		/// Current position size.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str254Key)]
		[DescriptionLoc(LocalizedStrings.Str425Key)]
		[StatisticsCategory]
		public decimal CurrentValue
		{
			get { return _currentValue; }
			set
			{
				if (_currentValue == value)
					return;

				_currentValue = value;
				NotifyChanged("CurrentValue");
			}
		}

		private decimal _blockedValue;

		/// <summary>
		/// Position size, registered for active orders.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str255Key)]
		[DescriptionLoc(LocalizedStrings.Str426Key)]
		[StatisticsCategory]
		public decimal BlockedValue
		{
			get { return _blockedValue; }
			set
			{
				if (_blockedValue == value)
					return;

				_blockedValue = value;
				NotifyChanged("BlockedValue");
			}
		}

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Required if additional information is stored in the program. For example, the amount of commission paid.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set
			{
				_extensionInfo = value;
				NotifyChanged("ExtensionInfo");
			}
		}

		private decimal _currentPrice;

		/// <summary>
		/// Position price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str256Key)]
		[DescriptionLoc(LocalizedStrings.Str428Key)]
		[StatisticsCategory]
		public decimal CurrentPrice
		{
			get { return _currentPrice; }
			set
			{
				if (_currentPrice == value)
					return;

				_currentPrice = value;
				NotifyChanged("CurrentPrice");
			}
		}

		private decimal _averagePrice;

		/// <summary>
		/// Average price.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str257Key)]
		[DescriptionLoc(LocalizedStrings.Str429Key)]
		[StatisticsCategory]
		public decimal AveragePrice
		{
			get { return _averagePrice; }
			set
			{
				if (_averagePrice == value)
					return;

				_averagePrice = value;
				NotifyChanged("AveragePrice");
			}
		}

		private decimal _unrealizedPnL;

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str258Key)]
		[DescriptionLoc(LocalizedStrings.Str430Key)]
		[StatisticsCategory]
		public decimal UnrealizedPnL
		{
			get { return _unrealizedPnL; }
			set
			{
				if (_unrealizedPnL == value)
					return;

				_unrealizedPnL = value;
				NotifyChanged("UnrealizedPnL");
			}
		}

		private decimal _realizedPnL;

		/// <summary>
		/// Realized profit.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.Str259Key)]
		[DescriptionLoc(LocalizedStrings.Str431Key)]
		[StatisticsCategory]
		public decimal RealizedPnL
		{
			get { return _realizedPnL; }
			set
			{
				if (_realizedPnL == value)
					return;

				_realizedPnL = value;
				NotifyChanged("RealizedPnL");
			}
		}

		private decimal _variationMargin;

		/// <summary>
		/// Variation margin.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str260Key)]
		[DescriptionLoc(LocalizedStrings.Str432Key)]
		[StatisticsCategory]
		public decimal VariationMargin
		{
			get { return _variationMargin; }
			set
			{
				if (_variationMargin == value)
					return;

				_variationMargin = value;
				NotifyChanged("VariationMargin");
			}
		}

		private decimal _commission;

		/// <summary>
		/// Total commission.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str433Key)]
		[StatisticsCategory]
		public decimal Commission
		{
			get { return _commission; }
			set
			{
				if (_commission == value)
					return;

				_commission = value;
				NotifyChanged("Commission");
			}
		}

		private DateTimeOffset _lastChangeTime;

		/// <summary>
		/// Time of last position change.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str434Key)]
		[DescriptionLoc(LocalizedStrings.Str435Key)]
		[StatisticsCategory]
		public DateTimeOffset LastChangeTime
		{
			get { return _lastChangeTime; }
			set
			{
				_lastChangeTime = value;
				NotifyChanged("LastChangeTime");
			}
		}

		private DateTime _localTime;

		/// <summary>
		/// Local time of the last position change.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str530Key)]
		[DescriptionLoc(LocalizedStrings.Str530Key, true)]
		[StatisticsCategory]
		public DateTime LocalTime
		{
			get { return _localTime; }
			set
			{
				_localTime = value;
				NotifyChanged("LocalTime");
			}
		}

		private string _description;

		/// <summary>
		/// Text position description.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description
		{
			get { return _description; }
			set
			{
				_description = value;
				NotifyChanged("Description");
			}
		}

		/// <summary>
		/// To copy fields of the current position to <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The position in which you should to copy fields.</param>
		public void CopyTo(BasePosition destination)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			destination.CurrentValue = CurrentValue;
			destination.BeginValue = BeginValue;
			destination.BlockedValue = BlockedValue;
			destination.Commission = Commission;
			destination.VariationMargin = VariationMargin;
			destination.RealizedPnL = RealizedPnL;
			destination.UnrealizedPnL = UnrealizedPnL;
			destination.AveragePrice = AveragePrice;
			destination.CurrentPrice = CurrentPrice;
			destination.Description = Description;
			destination.LastChangeTime = LastChangeTime;
			destination.LocalTime = LocalTime;
		}
	}
}