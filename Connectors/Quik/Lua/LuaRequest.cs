namespace StockSharp.Quik.Lua
{
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// ���������������� ������.
	/// </summary>
	public class LuaRequest
	{
		/// <summary>
		/// ������� <see cref="LuaRequest"/>.
		/// </summary>
		public LuaRequest()
		{
		}

		/// <summary>
		/// ��� ���������.
		/// </summary>
		public MessageTypes MessageType { get; set; }

		/// <summary>
		/// ������������� ����������.
		/// </summary>
		public long TransactionId { get; set; }

		/// <summary>
		/// �������� �������.
		/// </summary>
		public string Value { get; set; }

		/// <summary>
		/// ������������� �����������.
		/// </summary>
		public SecurityId? SecurityId { get; set; }

		/// <summary>
		/// ��� ������.
		/// </summary>
		public OrderTypes? OrderType { get; set; }

		/// <summary>
		/// �������� �� ��������� ��������� �� ������-������.
		/// </summary>
		public bool IsSubscribe { get; set; }

		/// <summary>
		/// ��� ������-������.
		/// </summary>
		public MarketDataTypes DataType { get; set; }

		/// <summary>
		/// �������� ��������� �������������.
		/// </summary>
		/// <returns>��������� �������������.</returns>
		public override string ToString()
		{
			return "Type = {0} TrId = {1} Value = {2} SecId = {3} OrdType = {4} IsSubscribe = {5} DataType = {6}"
				.Put(MessageType, TransactionId, Value, SecurityId, OrderType, IsSubscribe, DataType);
		}
	}
}