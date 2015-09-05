namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Serialization;

	using StockSharp.Messages;

	/// <summary>
	/// Description of the error that occurred during the registration or cancellation of the order.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public class OrderFail : IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OrderFail"/>.
		/// </summary>
		public OrderFail()
		{
		}

		/// <summary>
		/// The order which was not registered or was canceled due to an error.
		/// </summary>
		[DataMember]
		[RelationSingle]
		public Order Order { get; set; }

		/// <summary>
		/// System information about error containing the reason for the refusal or cancel of registration.
		/// </summary>
		[DataMember]
		[BinaryFormatter]
		public Exception Error { get; set; }

		/// <summary>
		/// Server time.
		/// </summary>
		[DataMember]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// Local time, when the error has been received.
		/// </summary>
		public DateTime LocalTime { get; set; }

		/// <summary>
		/// Extended information on the order with an error.
		/// </summary>
		[XmlIgnore]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return Order.ExtensionInfo; }
			set { Order.ExtensionInfo = value; }
		}
	}
}