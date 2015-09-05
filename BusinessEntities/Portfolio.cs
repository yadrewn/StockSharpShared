namespace StockSharp.BusinessEntities
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Portfolio, describing the trading account and the size of its generated commission.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
	[DescriptionLoc(LocalizedStrings.Str541Key)]
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 1)]
	public class Portfolio : BasePosition
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Portfolio"/>.
		/// </summary>
		public Portfolio()
		{
		}

		private string _name;

		/// <summary>
		/// Portfolio code name.
		/// </summary>
		[DataMember]
		[Identity]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string Name
		{
			get { return _name; }
			set
			{
				if (_name == value)
					return;

				_name = value;
				NotifyChanged("Name");
			}
		}

		private decimal _leverage;

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str542Key)]
		[DescriptionLoc(LocalizedStrings.Str261Key, true)]
		[MainCategory]
		public decimal Leverage
		{
			get { return _leverage; }
			set
			{
				if (_leverage == value)
					return;

				_leverage = value;
				NotifyChanged("Leverage");
			}
		}

		private CurrencyTypes? _currency;

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		[Nullable]
		public CurrencyTypes? Currency
		{
			get { return _currency; }
			set
			{
				_currency = value;
				NotifyChanged("Currency");
			}
		}

		[field: NonSerialized]
		private IConnector _connector;

		/// <summary>
		/// Connection to the trading system through which this portfolio has been loaded.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public IConnector Connector
		{
			get { return _connector; }
			set { _connector = value; }
		}

		/// <summary>
		/// Exchange board, for which the current portfolio is active.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.Str544Key)]
		[MainCategory]
		public ExchangeBoard Board { get; set; }

		private PortfolioStates? _state;

		/// <summary>
		/// Portfolio state.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str252Key)]
		[MainCategory]
		[Nullable]
		public PortfolioStates? State
		{
			get { return _state; }
			set
			{
				if (_state == value)
					return;

				_state = value;
				NotifyChanged("State");
			}
		}

		private static readonly Portfolio _anonymousPortfolio = new Portfolio { Name = LocalizedStrings.Str545 };

		/// <summary>
		/// Portfolio associated with the orders received through the orders log.
		/// </summary>
		public static Portfolio AnonymousPortfolio
		{
			get { return _anonymousPortfolio; }
		}

		/// <summary>
		/// Create a copy of <see cref="Portfolio"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public Portfolio Clone()
		{
			var clone = new Portfolio();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// To copy the current portfolio fields to the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The portfolio, in which fields should be copied .</param>
		public void CopyTo(Portfolio destination)
		{
			base.CopyTo(destination);

			destination.Name = Name;
			destination.Board = Board;
			destination.Currency = Currency;
			destination.Leverage = Leverage;
			destination.Connector = Connector;
			destination.State = State;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}
