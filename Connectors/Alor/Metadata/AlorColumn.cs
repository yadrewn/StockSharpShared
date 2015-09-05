namespace StockSharp.Alor.Metadata
{
	using System;

	using Ecng.Common;

	/// <summary>
	///  �������� �������.
	/// </summary>
	public class AlorColumn : Equatable<AlorColumn>
	{
		internal AlorColumn(AlorTableTypes tableType, string name, Type dataType, bool isMandatory = true)
		{
			//TableTypeName = TableType.ToString();
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (dataType == null)
				throw new ArgumentNullException("dataType");

			TableType = tableType;
			IsMandatory = isMandatory;
			Name = name;
			DataType = dataType;
			//TableTypeName = TableType.ToString();
			AlorManagerColumns.AllAlorColumn.Add(this);
		}

		///// <summary>
		///// getTableTypeName
		///// </summary>
		///// <returns></returns>
		//public string TableTypeName;

		internal AlorTableTypes TableType { get; private set; }

		/// <summary>
		/// ������ �� �������� �� ���������
		/// </summary>
		public bool IsMandatory { get; private set; }

		/// <summary>
		/// �������� �������.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// ��� �������.
		/// </summary>
		public Type DataType { get; private set; }

		/// <summary>
		/// ������� ����� <see cref="AlorColumn"/>.
		/// </summary>
		/// <returns>�����.</returns>
		public override AlorColumn Clone()
		{
			return new AlorColumn(TableType, Name, DataType)
			{
				IsMandatory = IsMandatory
			};
		}

		/// <summary>
		/// �������� <see cref="AlorColumn" /> �� ���������������.
		/// </summary>
		/// <param name="other">������ ��������, � ������� ���������� ����������.</param>
		/// <returns><see langword="true"/>, ���� ������ �������� ����� ��������, �����, <see langword="false"/>.</returns>
		protected override bool OnEquals(AlorColumn other)
		{
			return TableType == other.TableType && Name == other.Name;
		}

		/// <summary>
		/// ���������� ���-��� �������.
		/// </summary>
		/// <returns>���-��� �������.</returns>
		public override int GetHashCode()
		{
			return TableType.GetHashCode() ^ Name.GetHashCode();
		}
	}
}