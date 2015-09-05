namespace StockSharp.Messages
{
	using System;
	using System.ComponentModel;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Type of the changes in <see cref="PositionChangeMessage"/>.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public enum PositionChangeTypes
	{
		/// <summary>
		/// Initial value.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str253Key)]
		BeginValue,

		/// <summary>
		/// Current value.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str254Key)]
		CurrentValue,

		/// <summary>
		/// Blocked.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str255Key)]
		BlockedValue,

		/// <summary>
		/// Position price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str256Key)]
		CurrentPrice,

		/// <summary>
		/// Average price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str257Key)]
		AveragePrice,

		/// <summary>
		/// Unrealized profit.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str258Key)]
		UnrealizedPnL,

		/// <summary>
		/// Realized profit.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str259Key)]
		RealizedPnL,

		/// <summary>
		/// Variation margin.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str260Key)]
		VariationMargin,

		/// <summary>
		/// Currency.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.CurrencyKey)]
		Currency,

		/// <summary>
		/// Extended information.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		ExtensionInfo,

		/// <summary>
		/// Margin leverage.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str261Key)]
		Leverage,

		/// <summary>
		/// Total commission.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str262Key)]
		Commission,

		/// <summary>
		/// Current value (in lots).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str263Key)]
		CurrentValueInLots,

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str264Key)]
		DepoName,

		/// <summary>
		/// Portfolio state.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str265Key)]
		State,
	}

	/// <summary>
	/// The message contains information about the position changes.
	/// </summary>
	[System.Runtime.Serialization.DataContract]
	[Serializable]
	public sealed class PositionChangeMessage : BaseChangeMessage<PositionChangeTypes>
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityIdKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Portfolio name.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PortfolioKey)]
		[DescriptionLoc(LocalizedStrings.PortfolioNameKey)]
		[MainCategory]
		[ReadOnly(true)]
		public string PortfolioName { get; set; }

		/// <summary>
		/// The depositary where the physical security.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str264Key)]
		[DescriptionLoc(LocalizedStrings.DepoNameKey)]
		[MainCategory]
		public string DepoName { get; set; }

		/// <summary>
		/// Limit type for Т+ market.
		/// </summary>
		[DisplayNameLoc(LocalizedStrings.Str266Key)]
		[DescriptionLoc(LocalizedStrings.Str267Key)]
		[MainCategory]
		[Nullable]
		public TPlusLimits? LimitType { get; set; }

		/// <summary>
		/// Text position description.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.DescriptionKey)]
		[DescriptionLoc(LocalizedStrings.Str269Key)]
		[MainCategory]
		public string Description { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionChangeMessage"/>.
		/// </summary>
		public PositionChangeMessage()
			: base(MessageTypes.PositionChange)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="PositionChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var msg = new PositionChangeMessage
			{
				LocalTime = LocalTime,
				PortfolioName = PortfolioName,
				SecurityId = SecurityId,
				DepoName = DepoName,
				ServerTime = ServerTime,
				LimitType = LimitType,
				Description = Description,
			};

			msg.Changes.AddRange(Changes);
			this.CopyExtensionInfo(msg);

			return msg;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + ",Sec={0},P={1},Changes={2}".Put(SecurityId, PortfolioName, Changes.Select(c => c.ToString()).Join(","));
		}
	}
}