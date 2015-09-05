namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;
	using System.Security;

	/// <summary>
	/// Change password message.
	/// </summary>
	[DataContract]
	[Serializable]
	public class ChangePasswordMessage : Message
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ChangePasswordMessage"/>.
		/// </summary>
		public ChangePasswordMessage()
			: base(MessageTypes.ChangePassword)
		{
		}

		/// <summary>
		/// Request identifier.
		/// </summary>
		[DataMember]
		public long TransactionId { get; set; }

		/// <summary>
		/// ID of the original message <see cref="ChangePasswordMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// New password.
		/// </summary>
		[DataMember]
		public SecureString NewPassword { get; set; }

		/// <summary>
		/// Change password error info.
		/// </summary>
		[DataMember]
		public Exception Error { get; set; }

		/// <summary>
		/// Create a copy of <see cref="ChangePasswordMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return new ChangePasswordMessage
			{
				LocalTime = LocalTime,
				NewPassword = NewPassword,
				Error = Error,
			};
		}
	}
}