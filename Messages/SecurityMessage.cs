namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// A message containing info about the security.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public class SecurityMessage : Message
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str361Key)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		[ReadOnly(true)]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Security name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str362Key)]
		[MainCategory]
		public string Name { get; set; }

		/// <summary>
		/// Short security name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str363Key)]
		[DescriptionLoc(LocalizedStrings.Str364Key)]
		[MainCategory]
		public string ShortName { get; set; }

		/// <summary>
		/// Minimum volume step.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeStepKey)]
		[DescriptionLoc(LocalizedStrings.Str366Key)]
		[MainCategory]
		[Nullable]
		public decimal? VolumeStep { get; set; }

		/// <summary>
		/// Lot multiplier.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str330Key)]
		[DescriptionLoc(LocalizedStrings.LotVolumeKey)]
		[MainCategory]
		[Nullable]
		public decimal? Multiplier { get; set; }

		/// <summary>
		/// Number of digits in price after coma.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DecimalsKey)]
		[DescriptionLoc(LocalizedStrings.Str548Key)]
		[MainCategory]
		[Nullable]
		public int? Decimals { get; set; }
		
		/// <summary>
		/// Minimum price step.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceStepKey)]
		[DescriptionLoc(LocalizedStrings.MinPriceStepKey)]
		[MainCategory]
		[Nullable]
		public decimal? PriceStep { get; set; }

		/// <summary>
		/// Security type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[MainCategory]
		[Nullable]
		public SecurityTypes? SecurityType { get; set; }

		/// <summary>
		/// Security expiration date (for derivatives - expiration, for bonds — redemption).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ExpiryDateKey)]
		[DescriptionLoc(LocalizedStrings.Str371Key)]
		[MainCategory]
		[Nullable]
		public DateTimeOffset? ExpiryDate { get; set; }

		/// <summary>
		/// Settlement date for security (for derivatives and bonds).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SettlementDateKey)]
		[DescriptionLoc(LocalizedStrings.Str373Key)]
		[MainCategory]
		[Nullable]
		public DateTimeOffset? SettlementDate { get; set; }

		/// <summary>
		/// Underlying asset code, on which the current security is based.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.UnderlyingAssetKey)]
		[DescriptionLoc(LocalizedStrings.UnderlyingAssetCodeKey)]
		public string UnderlyingSecurityCode { get; set; }

		/// <summary>
		/// Option strike price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StrikeKey)]
		[DescriptionLoc(LocalizedStrings.OptionStrikePriceKey)]
		[Nullable]
		public decimal? Strike { get; set; }

		/// <summary>
		/// Option type.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.OptionsContractKey)]
		[DescriptionLoc(LocalizedStrings.OptionContractTypeKey)]
		[Nullable]
		public OptionTypes? OptionType { get; set; }

		/// <summary>
		/// Type of binary option.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BinaryOptionKey)]
		[DescriptionLoc(LocalizedStrings.TypeBinaryOptionKey)]
		public string BinaryOptionType { get; set; }

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
		/// ID of the original message <see cref="SecurityLookupMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Security class.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.ClassKey)]
		[DescriptionLoc(LocalizedStrings.SecurityClassKey)]
		[MainCategory]
		public string Class { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityMessage"/>.
		/// </summary>
		public SecurityMessage()
			: base(MessageTypes.Security)
		{
		}

		/// <summary>
		/// Initialize <see cref="SecurityMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected SecurityMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityMessage();
			CopyTo(clone);
			return clone;
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, which copied information.</param>
		public void CopyTo(SecurityMessage destination)
		{
			if (destination == null)
				throw new ArgumentNullException("destination");

			destination.SecurityId = SecurityId;
			destination.Name = Name;
			destination.ShortName = ShortName;
			destination.Currency = Currency;
			destination.ExpiryDate = ExpiryDate;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.OptionType = OptionType;
			destination.PriceStep = PriceStep;
			destination.Decimals = Decimals;
			destination.SecurityType = SecurityType;
			destination.SettlementDate = SettlementDate;
			destination.Strike = Strike;
			destination.UnderlyingSecurityCode = UnderlyingSecurityCode;
			destination.VolumeStep = VolumeStep;
			destination.Multiplier = Multiplier;
			destination.Class = Class;
			destination.BinaryOptionType = BinaryOptionType;
			destination.LocalTime = LocalTime;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0}".Put(SecurityId);
		}
	}
}