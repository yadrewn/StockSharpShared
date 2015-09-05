namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// A message requesting current registered orders and trades.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderStatusMessage : Message
	{
		/// <summary>
		/// Transaction ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TransactionKey)]
		[DescriptionLoc(LocalizedStrings.TransactionIdKey, true)]
		[MainCategory]
		public long TransactionId { get; set; }

		/// <summary>
		/// The requested order id.
		/// </summary>
		[DataMember]
		public long? OrderId { get; set; }

		/// <summary>
		/// The identifier of the requested order (as a string if the electronic board does not use a numeric representation of the identifiers).
		/// </summary>
		[DataMember]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Transaction ID of the requested order.
		/// </summary>
		[DataMember]
		public long? OrderTransactionId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderStatusMessage"/>.
		/// </summary>
		public OrderStatusMessage()
			: base(MessageTypes.OrderStatus)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderStatusMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new OrderStatusMessage
			{
				LocalTime = LocalTime,
				TransactionId = TransactionId,
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				OrderTransactionId = OrderTransactionId
			};
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",TransId={0}".Put(TransactionId);
		}
	}
}