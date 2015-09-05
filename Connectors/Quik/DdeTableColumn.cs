namespace StockSharp.Quik
{
	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// �������������� ������� �������, ������������ ��� DDE.
	/// </summary>
	[Ignore(FieldName = "IsDisposed")]
	[EntityFactory(typeof(UnitializedEntityFactory<DdeTableColumn>))]
	[TypeSchemaFactory(SearchBy.Properties, VisibleScopes.Both)]
	[Obfuscation(Feature = "Apply to member * when property: renaming", Exclude = true)]
	public class DdeTableColumn : Equatable<DdeTableColumn>
	{
		/// <summary>
		/// ������� <see cref="DdeTableColumn"/>.
		/// </summary>
		/// <param name="name">��� �������.</param>
		/// <param name="dataType">��� ������ � �������.</param>
		public DdeTableColumn(string name, Type dataType)
			: this(DdeTableTypes.None, name, dataType)
		{
		}

		internal DdeTableColumn(DdeTableTypes tableType, string name, Type dataType)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException("name");

			if (dataType == null)
				throw new ArgumentNullException("dataType");

			TableType = tableType;
			Name = name;
			DataType = dataType;
		}

		/// <summary>
		/// ��� �������.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// ��� ������ � �������.
		/// </summary>
		[Member]
		public Type DataType { get; private set; }

		/// <summary>
		/// �������� �� ������� ������������.
		/// </summary>
		public bool IsMandatory { get; set; }

		internal DdeTableTypes TableType { get; set; }

		///<summary>
		/// ������� ����� <see cref="DdeTableColumn" />.
		///</summary>
		///<returns>�����.</returns>
		public override DdeTableColumn Clone()
		{
			return new DdeTableColumn(TableType, Name, DataType) { IsMandatory = IsMandatory };
		}

		/// <summary>
		/// �������� <see cref="DdeTableColumn" /> �� ���������������.
		/// </summary>
		/// <param name="other">������ ��������, � ������� ���������� ����������.</param>
		/// <returns><see langword="true"/>, ���� ������ �������� ����� ��������, �����, <see langword="false"/>.</returns>
		protected override bool OnEquals(DdeTableColumn other)
		{
			return TableType == other.TableType && Name == other.Name;
		}

		/// <summary>
		/// ���������� ���-��� ������� <see cref="DdeTableColumn" />.
		/// </summary>
		/// <returns>���-���.</returns>
		public override int GetHashCode()
		{
			return TableType.GetHashCode() ^ Name.GetHashCode();
		}

		/// <summary>
		/// �������� ��������� �������������.
		/// </summary>
		/// <returns>��������� �������������.</returns>
		public override string ToString()
		{
			return Name;
		}
	}
}