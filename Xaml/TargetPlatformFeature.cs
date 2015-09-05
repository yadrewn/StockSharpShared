namespace StockSharp.Xaml
{
	using System;

	using Ecng.Common;
	using Ecng.Interop;
	using Ecng.Localization;

	using StockSharp.Localization;

	/// <summary>
	/// ����������������.
	/// </summary>
	public class TargetPlatformFeature
	{
		/// <summary>
		/// ���� ��� <see cref="LocalizedStrings"/>, �� �������� ����� �������� �������������� ��������.
		/// </summary>
		public string LocalizationKey { get; private set; }

		/// <summary>
		/// ������� ���������.
		/// </summary>
		public Languages PreferLanguage { get; private set; }

		/// <summary>
		/// ���������.
		/// </summary>
		public Platforms Platform { get; private set; }

		/// <summary>
		/// ������� <see cref="TargetPlatformFeature"/>.
		/// </summary>
		/// <param name="localizationKey">���� ��� <see cref="LocalizedStrings"/>, �� �������� ����� �������� �������������� ��������.</param>
		/// <param name="preferLanguage">������� ���������.</param>
		/// <param name="platform">���������.</param>
		public TargetPlatformFeature(string localizationKey, Languages preferLanguage = Languages.English, Platforms platform = Platforms.AnyCPU)
		{
			if (localizationKey.IsEmpty())
				throw new ArgumentNullException("localizationKey");

			LocalizationKey = localizationKey;
			PreferLanguage = preferLanguage;
			Platform = platform;
		}

		/// <summary>
		/// �������� ��������� �������������.
		/// </summary>
		/// <returns>��������� �������������.</returns>
		public override string ToString()
		{
			var str = LocalizedStrings.GetString(LocalizationKey);

			if (PreferLanguage != Languages.English && LocalizedStrings.ActiveLanguage != PreferLanguage)
				str += " ({0})".Put(PreferLanguage.ToString().Substring(0, 2).ToLowerInvariant());

			return str;
		}
	}
}