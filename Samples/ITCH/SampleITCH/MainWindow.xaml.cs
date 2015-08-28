namespace SampleITCH
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Windows;

	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Net;
	using Ecng.Xaml;

	using Ookii.Dialogs.Wpf;

	using StockSharp.BusinessEntities;
	using StockSharp.ITCH;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml;

	public partial class MainWindow
	{
		private bool _isConnected;

		public ItchTrader Trader;

		private readonly SecuritiesWindow _securitiesWindow = new SecuritiesWindow();
		private readonly TradesWindow _tradesWindow = new TradesWindow();
		private readonly OrdersLogWindow _orderLogWindow = new OrdersLogWindow();

		private readonly HashSet<string> _requestedBoards = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);

		private readonly LogManager _logManager = new LogManager();

		public MainWindow()
		{
			InitializeComponent();

			_orderLogWindow.MakeHideable();
			_tradesWindow.MakeHideable();
			_securitiesWindow.MakeHideable();

			Title = Title.Put("ITCH");

			Instance = this;

			_logManager.Listeners.Add(new FileLogListener { LogDirectory = "StockSharp_ITCH" });
			_logManager.Listeners.Add(new GuiLogListener(Monitor));
		}

		protected override void OnClosing(CancelEventArgs e)
		{
			_orderLogWindow.DeleteHideable();
			_tradesWindow.DeleteHideable();
			_securitiesWindow.DeleteHideable();
			
			_securitiesWindow.Close();
			_tradesWindow.Close();
			_orderLogWindow.Close();

			if (Trader != null)
				Trader.Dispose();

			base.OnClosing(e);
		}

		public static MainWindow Instance { get; private set; }

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!SecuritiesCsv.Text.IsEmpty())
				dlg.SelectedPath = SecuritiesCsv.Text;

			if (dlg.ShowDialog(this) == true)
			{
				SecuritiesCsv.Text = dlg.SelectedPath;
			}
		}

		private void ConnectClick(object sender, RoutedEventArgs e)
		{
			if (!_isConnected)
			{
				if (Login.Text.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2974);
					return;
				}
				else if (Password.Password.IsEmpty())
				{
					MessageBox.Show(this, LocalizedStrings.Str2975);
					return;
				}

				if (Trader == null)
				{
					// create connector
					Trader = new ItchTrader
					{
						//LogLevel = LogLevels.Debug,
						CreateDepthFromOrdersLog = true
					};

					_logManager.Sources.Add(Trader);

					ConfigManager.RegisterService(new FilterableSecurityProvider(Trader));

					// update gui labes
					Trader.ReConnectionSettings.WorkingTime = ExchangeBoard.Forts.WorkingTime;
					Trader.Restored += () => this.GuiAsync(() =>
					{
						// разблокируем кнопку Экспорт (соединение было восстановлено)
						ChangeConnectStatus(true);
						MessageBox.Show(this, LocalizedStrings.Str2958);
					});

					// subscribe on connection successfully event
					Trader.Connected += () =>
					{
						// set flag (connection is established)
						_isConnected = true;

						// update gui labes
						this.GuiAsync(() => ChangeConnectStatus(true));
					};
					Trader.Disconnected += () => this.GuiAsync(() => ChangeConnectStatus(false));

					// subscribe on connection error event
					Trader.ConnectionError += error => this.GuiAsync(() =>
					{
						// update gui labes
						ChangeConnectStatus(false);

						MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2959);	
					});

					// subscribe on error event
					Trader.Error += error =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2955));

					// subscribe on error of market data subscription event
					Trader.MarketDataSubscriptionFailed += (security, type, error) =>
						this.GuiAsync(() => MessageBox.Show(this, error.ToString(), LocalizedStrings.Str2956Params.Put(type, security)));

					var isAllDepths = AllDepths.IsChecked == true;

					Trader.NewSecurities += securities =>
					{
						foreach (var security in securities)
						{
							_securitiesWindow.SecurityPicker.Securities.Add(security);

							if (isAllDepths && _requestedBoards.Add(security.Board.Code))
							{
								Trader.SendInMessage(new MarketDataMessage
								{
									SecurityId = new SecurityId
									{
										BoardCode = security.Board.Code,
									},
									IsSubscribe = true,
									DataType = MarketDataTypes.OrderLog,
									TransactionId = Trader.TransactionIdGenerator.GetNextId(),
								});
							}
						}
					};
					Trader.NewTrades += _tradesWindow.TradeGrid.Trades.AddRange;
					Trader.NewOrderLogItems += _orderLogWindow.OrderLogGrid.LogItems.AddRange;

					// set market data provider
					_securitiesWindow.SecurityPicker.MarketDataProvider = Trader;

					ShowSecurities.IsEnabled = ShowTrades.IsEnabled = ShowOrdersLog.IsEnabled = true;
				}

				Trader.Login = Login.Text;
				Trader.Password = Password.Password;
				Trader.PrimaryMulticast = new MulticastSourceAddress
				{
					GroupAddress = GroupAddr.Address,
					SourceAddress = SourceAddr.Address,
					Port = Port.Value ?? 0
				};
				Trader.RecoveryAddress = Recovery.EndPoint;
				Trader.ReplayAddress = Replay.EndPoint;
				Trader.SecurityCsvFile = SecuritiesCsv.Text;

				// clear password box for security reason
				//Password.Clear();

				Trader.Connect();
			}
			else
			{
				Trader.Disconnect();
			}
		}

		private void ChangeConnectStatus(bool isConnected)
		{
			_isConnected = isConnected;
			ConnectBtn.Content = isConnected ? LocalizedStrings.Disconnect : LocalizedStrings.Connect;
		}

		private void ShowSecuritiesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_securitiesWindow);
		}

		private void ShowTradesClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_tradesWindow);
		}

		private void ShowOrdersLogClick(object sender, RoutedEventArgs e)
		{
			ShowOrHide(_orderLogWindow);
		}

		private static void ShowOrHide(Window window)
		{
			if (window == null)
				throw new ArgumentNullException("window");

			if (window.Visibility == Visibility.Visible)
				window.Hide();
			else
				window.Show();
		}
	}
}
