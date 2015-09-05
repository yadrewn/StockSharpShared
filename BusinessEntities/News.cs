namespace StockSharp.BusinessEntities
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;
	using System.Xml.Serialization;

	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// News.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	[DisplayNameLoc(LocalizedStrings.Str395Key)]
	[DescriptionLoc(LocalizedStrings.Str510Key)]
	public class News : NotifiableObject, IExtendableEntity
	{
		/// <summary>
		/// News ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.IdKey)]
		[DescriptionLoc(LocalizedStrings.NewsIdKey)]
		[MainCategory]
		//[Identity]
		public string Id { get; set; }

		/// <summary>
		/// Exchange board for which the news is published.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str511Key)]
		[DescriptionLoc(LocalizedStrings.Str512Key)]
		[MainCategory]
		public ExchangeBoard Board { get; set; }

		/// <summary>
		/// Security, for which news have been published.
		/// </summary>
		[RelationSingle(IdentityType = typeof(string))]
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.Str513Key)]
		[MainCategory]
		public Security Security { get; set; }

		/// <summary>
		/// News source.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str213Key)]
		[DescriptionLoc(LocalizedStrings.Str214Key)]
		[MainCategory]
		public string Source { get; set; }

		/// <summary>
		/// Header.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str215Key)]
		[DescriptionLoc(LocalizedStrings.Str215Key, true)]
		[MainCategory]
		public string Headline { get; set; }

		/// <summary>
		/// News text.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str217Key)]
		[DescriptionLoc(LocalizedStrings.Str218Key)]
		[MainCategory]
		public string Story
		{
			get { return _story; }
			set
			{
				_story = value;
				NotifyChanged("Story");
			}
		}

		/// <summary>
		/// Time of news arrival.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.TimeKey)]
		[DescriptionLoc(LocalizedStrings.Str220Key)]
		[MainCategory]
		public DateTimeOffset ServerTime { get; set; }

		/// <summary>
		/// News received local time.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str514Key)]
		[DescriptionLoc(LocalizedStrings.Str515Key)]
		[MainCategory]
		public DateTime LocalTime { get; set; }

		/// <summary>
		/// News link in the internet.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.Str221Key)]
		[DescriptionLoc(LocalizedStrings.Str222Key)]
		[MainCategory]
		[Url]
		public Uri Url { get; set; }

		[field: NonSerialized]
		private IDictionary<object, object> _extensionInfo;

		private string _story;

		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Required when extra information is stored in the program.
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
	}
}