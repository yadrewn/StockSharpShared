namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Own trade.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str502Key)]
	[DescriptionLoc(LocalizedStrings.Str503Key)]
	[CategoryOrderLoc(MainCategoryAttribute.NameKey, 0)]
	[CategoryOrderLoc(StatisticsCategoryAttribute.NameKey, 1)]
	public class MyTrade : IExtendableEntity
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="MyTrade"/>.
		/// </summary>
		public MyTrade()
		{
		}

		/// <summary>
		/// Order, for which a trade was filled.
		/// </summary>
		[DataMember]
		[RelationSingle]
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str504Key)]
		[DescriptionLoc(LocalizedStrings.Str505Key)]
		[MainCategory]
		[PropertyOrder(0)]
		public Order Order { get; set; }

		/// <summary>
		/// Trade info.
		/// </summary>
		[DataMember]
		[RelationSingle]
		[ExpandableObject]
		[DisplayNameLoc(LocalizedStrings.Str506Key)]
		[DescriptionLoc(LocalizedStrings.Str507Key)]
		[MainCategory]
		[PropertyOrder(1)]
		public Trade Trade { get; set; }

		/// <summary>
		/// Commission.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str159Key)]
		[DescriptionLoc(LocalizedStrings.Str160Key)]
		[StatisticsCategory]
		[PropertyOrder(4)]
		[Nullable]
		public decimal? Commission { get; set; }

		/// <summary>
		/// Slippage in trade price.
		/// </summary>
		[DataMember]
		[Nullable]
		[DisplayNameLoc(LocalizedStrings.Str163Key)]
		[DescriptionLoc(LocalizedStrings.Str164Key)]
		[MainCategory]
		public decimal? Slippage { get; set; }

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		/// <summary>
		/// Extended trade info.
		/// </summary>
		/// <remarks>
		/// Required if additional information associated with the own trade is stored in the program. For example, the commission amount, the currency, the trade type.
		/// </remarks>
		[Ignore]
		[XmlIgnore]
		[DisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		[DescriptionLoc(LocalizedStrings.Str427Key)]
		[MainCategory]
		[PropertyOrder(2)]
		public IDictionary<object, object> ExtensionInfo
		{
			get { return _extensionInfo; }
			set { _extensionInfo = value; }
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str509Params.Put(Trade, Order);
		}
	}
}