namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// A message containing the data for the cancellation of the order.
	/// </summary>
	[DataContract]
	[Serializable]
	public class OrderCancelMessage : OrderMessage
	{
		/// <summary>
		/// ID cancellation order.
		/// </summary>
		[DataMember]
		public long? OrderId { get; set; }

		/// <summary>
		/// Cancelling order id (as a string if the electronic board does not use a numeric representation of the identifiers).
		/// </summary>
		[DataMember]
		public string OrderStringId { get; set; }

		/// <summary>
		/// Order cancellation transaction id.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// Transaction ID cancellation order.
		/// </summary>
		[DataMember]
		public long OrderTransactionId { get; set; }

		/// <summary>
		/// Cancelling volume. If not specified, then it canceled the entire balance.
		/// </summary>
		[DataMember]
		public decimal? Volume { get; set; }

		/// <summary>
		/// Order side.
		/// </summary>
		[DataMember]
		public Sides? Side { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderCancelMessage"/>.
		/// </summary>
		public OrderCancelMessage()
			: base(MessageTypes.OrderCancel)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="OrderCancelMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new OrderCancelMessage
			{
				OrderId = OrderId,
				OrderStringId = OrderStringId,
				TransactionId = TransactionId,
				OrderTransactionId = OrderTransactionId,
				Volume = Volume,
				OrderType = OrderType,
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
				Side = Side,
			};

			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",OriginTransId={0},TransId={1},OrderId={2}".Put(OrderTransactionId, TransactionId, OrderId);
		}
	}
}