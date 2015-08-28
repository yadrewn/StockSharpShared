namespace StockSharp.Hydra.ITCH
{
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Net;

	using StockSharp.Hydra.Core;
	using StockSharp.ITCH;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	[DisplayNameLoc(_sourceName)]
	[DescriptionLoc(LocalizedStrings.Str2281ParamsKey, _sourceName)]
	// TODO
	//[TaskDoc("")]
	[TaskIcon("itch_logo.png")]
	[TaskCategory(TaskCategories.America | TaskCategories.RealTime | TaskCategories.OrderLog |
		TaskCategories.Level1 | TaskCategories.MarketDepth | TaskCategories.Stock |
		TaskCategories.Transactions | TaskCategories.Paid | TaskCategories.Ticks)]
	class ItchTask : ConnectorHydraTask<ItchTrader>
	{
		private const string _sourceName = "ITCH";

		[TaskSettingsDisplayName(_sourceName)]
		[CategoryOrder(_sourceName, 0)]
		private sealed class ItchSettings : ConnectorHydraTaskSettings
		{
			public ItchSettings(HydraTaskSettings settings)
				: base(settings)
			{
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.MainKey)]
			[DescriptionLoc(LocalizedStrings.MainServerKey, true)]
			[PropertyOrder(0)]
			public MulticastSourceAddress PrimaryMulticast
			{
				get { return (MulticastSourceAddress)ExtensionInfo["PrimaryMulticast"]; }
				set { ExtensionInfo["PrimaryMulticast"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.RecoveryKey)]
			[DescriptionLoc(LocalizedStrings.RecoveryServerKey, true)]
			[PropertyOrder(1)]
			public EndPoint RecoveryAddress
			{
				get { return ExtensionInfo["RecoveryAddress"].To<EndPoint>(); }
				set { ExtensionInfo["RecoveryAddress"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.ReplayKey)]
			[DescriptionLoc(LocalizedStrings.ReplayServerKey, true)]
			[PropertyOrder(2)]
			public EndPoint ReplayAddress
			{
				get { return ExtensionInfo["ReplayAddress"].To<EndPoint>(); }
				set { ExtensionInfo["ReplayAddress"] = value.To<string>(); }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.LoginKey)]
			[DescriptionLoc(LocalizedStrings.LoginKey, true)]
			[PropertyOrder(3)]
			public string Login
			{
				get { return (string)ExtensionInfo["Login"]; }
				set { ExtensionInfo["Login"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.PasswordKey)]
			[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
			[PropertyOrder(4)]
			public SecureString Password
			{
				get { return ExtensionInfo["Password"].To<SecureString>(); }
				set { ExtensionInfo["Password"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.SecuritiesKey)]
			[DescriptionLoc(LocalizedStrings.Str2137Key, true)]
			[PropertyOrder(5)]
			public string SecurityCsvFile
			{
				get { return (string)ExtensionInfo["SecurityCsvFile"]; }
				set { ExtensionInfo["SecurityCsvFile"] = value; }
			}

			[CategoryLoc(_sourceName)]
			[DisplayNameLoc(LocalizedStrings.XamlStr23Key)]
			[DescriptionLoc(LocalizedStrings.XamlStr23Key, true)]
			[PropertyOrder(6)]
			public bool OnlyActiveSecurities
			{
				get { return (bool)ExtensionInfo["OnlyActiveSecurities"]; }
				set { ExtensionInfo["OnlyActiveSecurities"] = value; }
			}
		}

		private ItchSettings _settings;

		public override HydraTaskSettings Settings
		{
			get { return _settings; }
		}

		protected override MarketDataConnector<ItchTrader> CreateConnector(HydraTaskSettings settings)
		{
			_settings = new ItchSettings(settings);

			if (settings.IsDefault)
			{
				_settings.PrimaryMulticast = new MulticastSourceAddress
				{
					GroupAddress = IPAddress.Any,
					Port = 1,
					SourceAddress = IPAddress.Any,
				};
				_settings.Login = string.Empty;
				_settings.Password = new SecureString();
				_settings.ReplayAddress = _settings.RecoveryAddress = new IPEndPoint(IPAddress.Loopback, 3);
				_settings.SecurityCsvFile = string.Empty;
				_settings.OnlyActiveSecurities = true;
			}

			return new MarketDataConnector<ItchTrader>(EntityRegistry.Securities, this, () => new ItchTrader
			{
				Login = _settings.Login,
				Password = _settings.Password.To<string>(),
				PrimaryMulticast = _settings.PrimaryMulticast,
				RecoveryAddress = _settings.RecoveryAddress,
				ReplayAddress = _settings.ReplayAddress,
				SecurityCsvFile = _settings.SecurityCsvFile,
				OnlyActiveSecurities = _settings.OnlyActiveSecurities
			});
		}
	}
}