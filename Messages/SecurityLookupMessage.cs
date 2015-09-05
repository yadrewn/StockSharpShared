namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// Message security lookup for specified criteria.
	/// </summary>
	[DataContract]
	[Serializable]
	public class SecurityLookupMessage : SecurityMessage, IEquatable<SecurityLookupMessage>
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
		/// Securities types.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TypeKey)]
		[DescriptionLoc(LocalizedStrings.Str360Key)]
		[MainCategory]
		public IEnumerable<SecurityTypes> SecurityTypes { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="SecurityLookupMessage"/>.
		/// </summary>
		public SecurityLookupMessage()
			: base(MessageTypes.SecurityLookup)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="SecurityLookupMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var clone = new SecurityLookupMessage
			{
				TransactionId = TransactionId,
				SecurityTypes = SecurityTypes
			};
			
			CopyTo(clone);

			return clone;
		}

		/// <summary>
		/// Determines whether the specified criterias are considered equal.
		/// </summary>
		/// <param name="other">Another search criteria with which to compare.</param>
		/// <returns><see langword="true" />, if criterias are equal, otherwise, <see langword="false" />.</returns>
		public bool Equals(SecurityLookupMessage other)
		{
			if (SecurityId.Equals(other.SecurityId))
				return true;

			if (Name == other.Name && 
				ShortName == other.ShortName && 
				Currency == other.Currency && 
				ExpiryDate == other.ExpiryDate && 
				OptionType == other.OptionType &&
				((SecurityTypes == null && other.SecurityTypes == null) ||
				(SecurityTypes != null && other.SecurityTypes != null && SecurityTypes.SequenceEqual(other.SecurityTypes))) && 
				SettlementDate == other.SettlementDate &&
				Strike == other.Strike &&
				UnderlyingSecurityCode == other.UnderlyingSecurityCode)
				return true;

			return false;
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