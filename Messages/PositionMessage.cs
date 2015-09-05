namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The message contains information about the position.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public sealed class PositionMessage : Message
	{
		/// <summary>
		/// Portfolio, in which position is created.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.Str270Key)]
		[MainCategory]
		public string PortfolioName { get; set; }

		/// <summary>
		/// Text position description.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description { get; set; }

		/// <summary>
		/// Security, for which a position was created.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str271Key)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.DepoKey)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Limit type for Т+ market.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str272Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		[Nullable]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// ID of the original message <see cref="PortfolioMessage.TransactionId"/> for which this message is a response.
		/// </summary>
		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionMessage"/>.
		/// </summary>
		public PositionMessage()
			: base(MessageTypes.Position)
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() +  ",Sec={0},P={1}".Put(SecurityId, PortfolioName);
		}

		/// <summary>
		/// Create a copy of <see cref="PositionMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new PositionMessage
			{
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
				OriginalTransactionId = OriginalTransactionId,
				DepoName = DepoName,
				LimitType = LimitType,
				LocalTime = LocalTime,
				Description = Description
			};

			this.CopyExtensionInfo(clone);

			return clone;
		}
	}
}