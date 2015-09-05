namespace SampleStorage
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Messages;

	class Program
	{
		static void Main()
		{
			// creating AAPL security
			var security = new Security
			{
				Id = "AAPL@NASDAQ",
				PriceStep = 0.1m,
				Decimals = 1,
			};

			var trades = new List<Trade>();

			// generation 1000 random ticks
			//

			for (var i = 0; i < 1000; i++)
			{
				var t = new Trade
				{
					Time = DateTime.Today + TimeSpan.FromMinutes(i),
					Id = i + 1,
					Security = security,
					Volume = RandomGen.GetInt(1, 10),
					Price = RandomGen.GetInt(1, 100) * security.PriceStep ?? 1m + 99
				};

				trades.Add(t);
			}

			using (var drive = new LocalMarketDataDrive())
			{
				// get AAPL storage
				var aaplStorage = drive.GetSecurityDrive(security);

				// get tick storage
				var tradeStorage = (IMarketDataStorage<Trade>)aaplStorage.GetTickStorage(new CsvMarketDataSerializer<ExecutionMessage>());

				// saving ticks
				tradeStorage.Save(trades);

				// loading ticks
				var loadedTrades = tradeStorage.Load(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(1000));

				foreach (var trade in loadedTrades)
				{
					Console.WriteLine(LocalizedStrings.Str2968Params, trade.Id, trade);
				}

				Console.ReadLine();

				// deleting ticks (and removing file)
				tradeStorage.Delete(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(1000));	
			}
		}
	}
}