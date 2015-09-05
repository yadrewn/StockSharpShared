namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Portfolio lookup result message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class PortfolioLookupResultMessage : Message
	{
		/// <summary>
		/// ID of the original message <see cref="PortfolioMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Portfolio lookup error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		public PortfolioLookupResultMessage()
			: base(MessageTypes.PortfolioLookupResult)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="PortfolioLookupResultMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new PortfolioLookupResultMessage
			{
				OriginalTransactionId = OriginalTransactionId,
				LocalTime = LocalTime,
				Error = Error
			};
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Orig={0}".Put(OriginalTransactionId);
		}
	}
}