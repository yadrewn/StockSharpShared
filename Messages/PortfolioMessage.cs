namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Portfolio states.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum PortfolioStates
	{
		/// <summary>
		/// Active.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str248Key)]
		Active,
		
		/// <summary>
		/// Blocked.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str249Key)]
		Blocked,
	}

	/// <summary>
	/// The message contains information about portfolio.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioMessage : Message
	{
		/// <summary>
		/// Portfolio code name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.NameKey)]
		[DescriptionLoc(LocalizedStrings.Str247Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Portfolio currency.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.CurrencyKey)]
		[DescriptionLoc(LocalizedStrings.Str251Key)]
		[MainCategory]
		public CurrencyTypes? Currency { get; set; }

		/// <summary>
		/// Electronic board code.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.BoardKey)]
		[DescriptionLoc(LocalizedStrings.BoardCodeKey)]
		[MainCategory]
		public string BoardCode { get; set; }

		/// <summary>
		/// Portfolio state.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.StateKey)]
		[DescriptionLoc(LocalizedStrings.Str252Key)]
		[MainCategory]
		public PortfolioStates? State { get; set; }

		/// <summary>
		/// ID of the original message <see cref="PortfolioMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Subscription/unsubscription portfolio changes transaction id.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// Is the message subscription portfolio changes.
		/// </summary>
		[DataMember]
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioMessage"/>.
		/// </summary>
		public PortfolioMessage()
			: base(MessageTypes.Portfolio)
		{
		}

		/// <summary>
		/// Initialize <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <param name="type">Message type.</param>
		protected PortfolioMessage(MessageTypes type)
			: base(type)
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Name={0}".Put(PortfolioName);
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new PortfolioMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, which copied information.</param>
		protected PortfolioMessage CopyTo(PortfolioMessage destination)
		{
			destination.PortfolioName = PortfolioName;
			destination.Currency = Currency;
			destination.BoardCode = BoardCode;
			destination.OriginalTransactionId = OriginalTransactionId;
			destination.IsSubscribe = IsSubscribe;
			destination.State = State;
			destination.TransactionId = TransactionId;

			this.CopyExtensionInfo(destination);

			return destination;
		}
	}
}