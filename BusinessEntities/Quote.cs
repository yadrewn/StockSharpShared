namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Market depth quote representing bid or ask.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str273Key)]
	[DescriptionLoc(LocalizedStrings.Str274Key)]
	[ExpandableObject]
	public class Quote : Cloneable<Quote>, IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Quote"/>.
		/// </summary>
		public Quote()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Quote"/>.
		/// </summary>
		/// <param name="security">The instrument by which the quote is received.</param>
		/// <param name="price">Quote price.</param>
		/// <param name="volume">Quote volume.</param>
		/// <param name="direction">Direction (buy or sell).</param>
		public Quote(Security security, decimal price, decimal volume, Sides direction)
		{
			_security = security;
			_price = price;
			_volume = volume;
			_direction = direction;
		}

		private Security _security;

		/// <summary>
		/// The instrument by which the quote is received.
		/// </summary>
		[Ignore]
		[XmlIgnore]
		[Browsable(false)]
		public Security Security
		{
			get { return _security; }  
			set { _security = value; }
		}

		private decimal _price;

		/// <summary>
		/// Quote price.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.PriceKey)]
		[DescriptionLoc(LocalizedStrings.Str275Key)]
		[MainCategory]
		public decimal Price
		{
			get { return _price; }
			set { _price = value; }
		}

		private decimal _volume;

		/// <summary>
		/// Quote volume.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.VolumeKey)]
		[DescriptionLoc(LocalizedStrings.Str276Key)]
		[MainCategory]
		public decimal Volume
		{
			get { return _volume; }
			set { _volume = value; }
		}

		private Sides _direction;

		/// <summary>
		/// Direction (buy or sell).
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str128Key)]
		[DescriptionLoc(LocalizedStrings.Str277Key)]
		[MainCategory]
		public Sides OrderDirection
		{
			get { return _direction; }
			set { _direction = value; }
		}

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended quote info.
		/// </summary>
		/// <remarks>
		/// Uses in case of keep additional information associated with the quotation. For example, the number of contracts in its own order book, the amount of the best buying and selling.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set { _extensionInfo = value; }
		}

		/// <summary>
		/// Create a copy of <see cref="Quote"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Quote Clone()
		{
			return new Quote(_security, _price, _volume, _direction)
			{
				ExtensionInfo = ExtensionInfo,
			};
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} {1} {2}".Put(OrderDirection == Sides.Buy ? LocalizedStrings.Bid : LocalizedStrings.Ask, Price, Volume);
		}
	}
}
