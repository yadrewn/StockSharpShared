namespace StockSharp.Algo.Testing
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// ������� ����������� ��������.
	/// </summary>
	public abstract class BaseEmulationConnector : Connector
	{
		/// <summary>
		/// ���������������� <see cref="BaseEmulationConnector"/>.
		/// </summary>
		protected BaseEmulationConnector()
		{
			EmulationAdapter = new EmulationMessageAdapter(TransactionIdGenerator);
		}

		/// <summary>
		/// �������, ����������� ��������� � <see cref="IMarketEmulator"/>.
		/// </summary>
		public EmulationMessageAdapter EmulationAdapter
		{
			get; private set;
		}

		/// <summary>
		/// �������������� �� ��������������� ������ ����� ����� <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// � ���� ����� ����������.
		/// </summary>
		public override bool IsSupportAtomicReRegister
		{
			get { return EmulationAdapter.Emulator.Settings.IsSupportAtomicReRegister; }
		}

		///// <summary>
		///// �������� ������.
		///// </summary>
		//public IMarketEmulator MarketEmulator
		//{
		//	get { return EmulationAdapter.Emulator; }
		//	set { EmulationAdapter.Emulator = value; }
		//}

		/// <summary>
		/// ��������� ������ ��������� ��������� <see cref="TimeMessage"/> � ���������� <see cref="Connector.MarketTimeChangedInterval"/>.
		/// </summary>
		protected override void StartMarketTimer()
		{
		}

		///// <summary>
		///// ���������� ���������, ���������� �������� ������.
		///// </summary>
		///// <param name="message">���������, ���������� �������� ������.</param>
		///// <param name="direction">����������� ���������.</param>
		//protected override void OnProcessMessage(Message message, MessageDirections direction)
		//{
		//	if (adapter == MarketDataAdapter && direction == MessageDirections.Out)
		//	{
		//		switch (message.Type)
		//		{
		//			case MessageTypes.Connect:
		//			case MessageTypes.Disconnect:
		//			case MessageTypes.MarketData:
		//			case MessageTypes.Error:
		//			case MessageTypes.SecurityLookupResult:
		//			case MessageTypes.PortfolioLookupResult:
		//				base.OnProcessMessage(message, direction);
		//				break;

		//			case MessageTypes.Execution:
		//			{
		//				var execMsg = (ExecutionMessage)message;

		//				if (execMsg.ExecutionType != ExecutionTypes.Trade)
		//					SendInMessage(message);
		//				else
		//					base.OnProcessMessage(message, direction);

		//				break;
		//			}

		//			default:
		//				SendInMessage(message);
		//				break;
		//		}
		//	}
		//	else
		//		base.OnProcessMessage(message, direction);
		//}

		private void SendInGeneratorMessage(MarketDataGenerator generator, bool isSubscribe)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			SendInMessage(new GeneratorMessage
			{
				IsSubscribe = isSubscribe,
				SecurityId = generator.SecurityId,
				Generator = generator,
				DataType = generator.DataType,
			});
		}

		/// <summary>
		/// ���������������� ��������� ������.
		/// </summary>
		/// <param name="generator">��������� ������.</param>
		public void RegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// ������� ��������� ������, ����� ������������������ ����� <see cref="RegisterTrades"/>.
		/// </summary>
		/// <param name="generator">��������� ������.</param>
		public void UnRegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// ���������������� ��������� ��������.
		/// </summary>
		/// <param name="generator">��������� ��������.</param>
		public void RegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// ������� ��������� ��������, ����� ������������������ ����� <see cref="RegisterMarketDepth"/>.
		/// </summary>
		/// <param name="generator">��������� ��������.</param>
		public void UnRegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// ���������������� ��������� ���� ������.
		/// </summary>
		/// <param name="generator">��������� ���� ������.</param>
		public void RegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// ������� ��������� ���� ������, ����� ������������������ ����� <see cref="RegisterOrderLog"/>.
		/// </summary>
		/// <param name="generator">��������� ���� ������.</param>
		public void UnRegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}
	}
}