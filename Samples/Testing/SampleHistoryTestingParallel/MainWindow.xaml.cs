namespace SampleHistoryTestingParallel
{
	using System;
	using System.IO;
	using System.Linq;
	using System.Windows;
	using System.Windows.Media;

	using Ookii.Dialogs.Wpf;

	using Ecng.Collections;
	using Ecng.Xaml;
	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.Algo.Strategies.Testing;
	using StockSharp.Algo.Testing;
	using StockSharp.Algo.Indicators;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Xaml.Charting;
	using StockSharp.Localization;

	public partial class MainWindow
	{
		private DateTime _startEmulationTime;

		public MainWindow()
		{
			InitializeComponent();

			HistoryPath.Text = @"..\..\..\HistoryData\".ToFullPath();
		}

		private void FindPathClick(object sender, RoutedEventArgs e)
		{
			var dlg = new VistaFolderBrowserDialog();

			if (!HistoryPath.Text.IsEmpty())
				dlg.SelectedPath = HistoryPath.Text;

			if (dlg.ShowDialog() == true)
			{
				HistoryPath.Text = dlg.SelectedPath;
			}
		}

		private void StartBtnClick(object sender, RoutedEventArgs e)
		{
			if (HistoryPath.Text.IsEmpty() || !Directory.Exists(HistoryPath.Text))
			{
				MessageBox.Show(this, LocalizedStrings.Str3014);
				return;
			}

			if (Math.Abs(TestingProcess.Value - 0) > double.Epsilon)
			{
				MessageBox.Show(this, LocalizedStrings.Str3015);
				return;
			}

			var logManager = new LogManager();
			var fileLogListener = new FileLogListener("sample.log");
			logManager.Listeners.Add(fileLogListener);

			// SMA periods
			var periods = new[]
			{
				new Tuple<int, int, Color>(80, 10, Colors.DarkGreen),
				new Tuple<int, int, Color>(70, 8, Colors.Red),
				new Tuple<int, int, Color>(60, 6, Colors.DarkBlue)
			};

			// storage to historical data
			var storageRegistry = new StorageRegistry
			{
				// set historical path
				DefaultDrive = new LocalMarketDataDrive(HistoryPath.Text)
			};

			var timeFrame = TimeSpan.FromMinutes(5);

			// create test security
			var security = new Security
			{
				Id = "RIZ2@FORTS", // sec id has the same name as folder with historical data
				Code = "RIZ2",
				Name = "RTS-12.12",
				Board = ExchangeBoard.Forts,
			};

			var startTime = new DateTime(2012, 10, 1);
			var stopTime = new DateTime(2012, 10, 31);

			var level1Info = new Level1ChangeMessage
			{
				SecurityId = security.ToSecurityId(),
				ServerTime = startTime,
			}
			.TryAdd(Level1Fields.PriceStep, 10m)
			.TryAdd(Level1Fields.StepPrice, 6m)
			.TryAdd(Level1Fields.MinPrice, 10m)
			.TryAdd(Level1Fields.MaxPrice, 1000000m)
			.TryAdd(Level1Fields.MarginBuy, 10000m)
			.TryAdd(Level1Fields.MarginSell, 10000m);

			// test portfolio
			var portfolio = new Portfolio
			{
				Name = "test account",
				BeginValue = 1000000,
			};

			// create backtesting connector
			var batchEmulation = new BatchEmulation(new[] { security }, new[] { portfolio }, storageRegistry)
			{
				EmulationSettings =
				{
					MarketTimeChangedInterval = timeFrame,
					StartTime = startTime,
					StopTime = stopTime,

					// count of parallel testing strategies
					BatchSize = periods.Length,
				}
			};

			// handle historical time for update ProgressBar
			batchEmulation.ProgressChanged += (curr, total) => this.GuiAsync(() => TestingProcess.Value = total);

			batchEmulation.StateChanged += (oldState, newState) =>
			{
				if (batchEmulation.State != EmulationStates.Stopped)
					return;

				this.GuiAsync(() =>
				{
					if (batchEmulation.IsFinished)
					{
						TestingProcess.Value = TestingProcess.Maximum;
						MessageBox.Show(this, LocalizedStrings.Str3024.Put(DateTime.Now - _startEmulationTime));
					}
					else
						MessageBox.Show(this, LocalizedStrings.cancelled);
				});
			};

			// get emulation connector
			var connector = batchEmulation.EmulationConnector;

			logManager.Sources.Add(connector);

			connector.NewSecurities += securities =>
			{
				if (securities.All(s => s != security))
					return;

				// fill level1 values
				connector.SendInMessage(level1Info);

				connector.RegisterMarketDepth(new TrendMarketDepthGenerator(connector.GetSecurityId(security))
				{
					// order book freq refresh is 1 sec
					Interval = TimeSpan.FromSeconds(1),
				});
			};

			TestingProcess.Maximum = 100;
			TestingProcess.Value = 0;

			_startEmulationTime = DateTime.Now;

			var strategies = periods
				.Select(period =>
				{
					var series = new CandleSeries(typeof(TimeFrameCandle), security, timeFrame);

					// create strategy based SMA
					var strategy = new SmaStrategy(series, new SimpleMovingAverage { Length = period.Item1 }, new SimpleMovingAverage { Length = period.Item2 })
					{
						Volume = 1,
						Security = security,
						Portfolio = portfolio,
						Connector = connector,

						// by default interval is 1 min,
						// it is excessively for time range with several months
						UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>()
					};

					strategy.SetCandleManager(new CandleManager(connector));

					var curveItems = Curve.CreateCurve(LocalizedStrings.Str3026Params.Put(period.Item1, period.Item2), period.Item3);
					strategy.PnLChanged += () =>
					{
						var data = new EquityData
						{
							Time = strategy.CurrentTime,
							Value = strategy.PnL,
						};

						this.GuiAsync(() => curveItems.Add(data));
					};

					Stat.AddStrategies(new[] { strategy });

					return strategy;
				})
				.ToEx(periods.Length);

			// start emulation
			batchEmulation.Start(strategies);
		}
	}
}