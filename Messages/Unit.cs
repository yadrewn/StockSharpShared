namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Localization;
	using Ecng.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Mearure units.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public enum UnitTypes
	{
		/// <summary>
		/// The absolute value. Incremental change is a given number.
		/// </summary>
		[EnumMember]
		Absolute,

		/// <summary>
		/// Percents.Step change - one hundredth of a percent.
		/// </summary>
		[EnumMember]
		Percent,

		/// <summary>
		/// Point.
		/// </summary>
		[EnumMember]
		Point,

		/// <summary>
		/// Price step.
		/// </summary>
		[EnumMember]
		Step,

		/// <summary>
		/// The limited value. This unit allows to set a specific change number, which can not be used in arithmetic operations <see cref="Unit"/>.
		/// </summary>
		[EnumMember]
		Limit,
	}

	/// <summary>
	/// Special class, allows to set the value as a percentage, absolute, points and pips values.
	/// </summary>
	[Serializable]
	[System.Runtime.Serialization.DataContract]
	public class Unit : Equatable<Unit>, IOperable<Unit>
	{
		/// <summary>
		/// Create unit.
		/// </summary>
		public Unit()
		{
		}

		/// <summary>
		/// Create absolute value <see cref="UnitTypes.Absolute"/>.
		/// </summary>
		/// <param name="value">Value.</param>
		public Unit(decimal value)
			: this(value, UnitTypes.Absolute)
		{
		}

		/// <summary>
		/// Create a value of types <see cref="UnitTypes.Absolute"/> and <see cref="UnitTypes.Percent"/>.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="type">Mearure unit.</param>
		public Unit(decimal value, UnitTypes type)
			: this(value, type, null)
		{
		}

		/// <summary>
		/// Create a value of types <see cref="UnitTypes.Point"/> and <see cref="UnitTypes.Step"/>.
		/// </summary>
		/// <param name="value">Value.</param>
		/// <param name="type">Mearure unit.</param>
		/// <param name="getTypeValue">The handler returns a value associated with <see cref="Unit.Type"/> (price or volume steps).</param>
		public Unit(decimal value, UnitTypes type, Func<UnitTypes, decimal?> getTypeValue)
		{
			// mika Äàííóþ ïðîâåðêó ëó÷øå äåëàòü ïðè àðèôìåòè÷åñêèõ äåéñòâèÿõ
			//
			//if (type == UnitTypes.Point || type == UnitTypes.Step)
			//{
			//    if (security == null)
			//        throw new ArgumentException("Åäèíèöà èçìåðåíèÿ íå ìîæåò áûòü '{0}' òàê êàê íå ïåðåäàíà èíôîðìàöèÿ îá èíñòðóìåíòå.".Put(type), "type");
			//}

			Value = value;
			Type = type;
			GetTypeValue = getTypeValue;
		}

		/// <summary>
		/// Mearure unit.
		/// </summary>
		[DataMember]
		public UnitTypes Type { get; set; }

		/// <summary>
		/// Value.
		/// </summary>
		[DataMember]
		public decimal Value { get; set; }

		[field: NonSerialized]
		private Func<UnitTypes, decimal?> _getTypeValue;

		/// <summary>
		/// The handler returns a value associated with <see cref="Unit.Type"/> (price or volume steps).
		/// </summary>
		[Ignore]
		public Func<UnitTypes, decimal?> GetTypeValue
		{
			get { return _getTypeValue; }
			set { _getTypeValue = value; }
		}

		/// <summary>
		/// Create a copy of <see cref="Unit"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Unit Clone()
		{
			return new Unit
			{
				Type = Type,
				Value = Value,
				GetTypeValue = GetTypeValue,
			};
		}

		/// <summary>
		/// Compare <see cref="Unit"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns>The result of the comparison.</returns>
		public override int CompareTo(Unit other)
		{
			if (this == other)
				return 0;

			if (this < other)
				return -1;

			return 1;
		}

		/// <summary>
		/// Cast <see cref="Decimal"/> object to the type <see cref="Unit"/>.
		/// </summary>
		/// <param name="value"><see cref="Decimal"/> value.</param>
		/// <returns>Object <see cref="Unit"/>.</returns>
		public static implicit operator Unit(decimal value)
		{
			return new Unit(value);
		}

		/// <summary>
		/// Cast <see cref="Int32"/> object to the type <see cref="Unit"/>.
		/// </summary>
		/// <param name="value"><see cref="Int32"/> value.</param>
		/// <returns>Object <see cref="Unit"/>.</returns>
		public static implicit operator Unit(int value)
		{
			return new Unit(value);
		}

		/// <summary>
		/// Cast object from <see cref="Unit"/> to <see cref="Decimal"/>.
		/// </summary>
		/// <param name="unit">Object <see cref="Unit"/>.</param>
		/// <returns><see cref="Decimal"/> value.</returns>
		public static explicit operator decimal(Unit unit)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");

			switch (unit.Type)
			{
				case UnitTypes.Limit:
				case UnitTypes.Absolute:
					return unit.Value;
				case UnitTypes.Percent:
					throw new ArgumentException(LocalizedStrings.PercentagesConvert, "unit");
				case UnitTypes.Point:
					return unit.Value * unit.SafeGetTypeValue(null);
				case UnitTypes.Step:
					return unit.Value * unit.SafeGetTypeValue(null);
				default:
					throw new ArgumentOutOfRangeException("unit");
			}
		}

		/// <summary>
		/// Cast <see cref="Double"/> object to the type <see cref="Unit"/>.
		/// </summary>
		/// <param name="value"><see cref="Double"/> value.</param>
		/// <returns>Object <see cref="Unit"/>.</returns>
		public static implicit operator Unit(double value)
		{
			return (decimal)value;
		}

		/// <summary>
		/// Cast object from <see cref="Unit"/> to <see cref="Double"/>.
		/// </summary>
		/// <param name="unit">Object <see cref="Unit"/>.</param>
		/// <returns><see cref="Double"/> value.</returns>
		public static explicit operator double(Unit unit)
		{
			return (double)(decimal)unit;
		}

		private decimal SafeGetTypeValue(Func<UnitTypes, decimal?> getTypeValue)
		{
			var func = GetTypeValue ?? getTypeValue;

			if (func == null)
				throw new InvalidOperationException("Îáðàáîò÷èê ïîëó÷åíèÿ çíà÷åíèÿ íå óñòàíîâëåí.");

			var value = func(Type);

			if (value != null && value != 0)
				return value.Value;

			if (getTypeValue == null)
				throw new ArgumentNullException("getTypeValue");

			value = getTypeValue(Type);

			if (value == null || value == 0)
				throw new InvalidOperationException(LocalizedStrings.Str1291);

			return value.Value;
		}

		private static Unit CreateResult(Unit u1, Unit u2, Func<decimal, decimal, decimal> operation, Func<decimal, decimal, decimal> percentOperation)
		{
			//  ïðåäîâðàòèòü âûçîâ ïåðåîïðåäåëåííîãî îïåðàòîðà
			//if (u1 == null)
			if (u1.IsNull())
				throw new ArgumentNullException("u1");

			//if (u2 == null)
			if (u2.IsNull())
				throw new ArgumentNullException("u2");

			if (u1.Type == UnitTypes.Limit || u2.Type == UnitTypes.Limit)
				throw new ArgumentException(LocalizedStrings.LimitedValueNotMath);

			if (operation == null)
				throw new ArgumentNullException("operation");

			if (percentOperation == null)
				throw new ArgumentNullException("percentOperation");

			//if (u1.CheckGetTypeValue(false) != u2.CheckGetTypeValue(false))
			//	throw new ArgumentException("Ó îäíîé èç âåëè÷èí íå óñòàíîâëåíî ïîëó÷åíèå çíà÷åíèÿ.");

			//if (u1.GetTypeValue != null && u2.GetTypeValue != null && u1.GetTypeValue != u2.GetTypeValue)
			//	throw new ArgumentException(LocalizedStrings.Str614Params.Put(u1.Security.Id, u2.Security.Id));

			var result = new Unit
			{
				Type = u1.Type,
				GetTypeValue = u1.GetTypeValue ?? u2.GetTypeValue
			};

			if (u1.Type == u2.Type)
			{
				result.Value = operation(u1.Value, u2.Value);
			}
			else
			{
				if (u1.Type == UnitTypes.Percent || u2.Type == UnitTypes.Percent)
				{
					result.Type = u1.Type == UnitTypes.Percent ? u2.Type : u1.Type;

					var nonPerValue = u1.Type == UnitTypes.Percent ? u2.Value : u1.Value;
					var perValue = u1.Type == UnitTypes.Percent ? u1.Value : u2.Value;

					result.Value = percentOperation(nonPerValue, perValue * nonPerValue.Abs() / 100.0m);
				}
				else
				{
					var value = operation((decimal)u1, (decimal)u2);

					switch (result.Type)
					{
						case UnitTypes.Absolute:
							break;
						case UnitTypes.Point:
							value /= u1.SafeGetTypeValue(result.GetTypeValue);
							break;
						case UnitTypes.Step:
							value /= u1.SafeGetTypeValue(result.GetTypeValue);
							break;
						default:
							throw new ArgumentOutOfRangeException();
					}

					result.Value = value;
				}
			}

			return result;
		}

		/// <summary>
		/// Add the two objects <see cref="Unit"/>.
		/// </summary>
		/// <param name="u1">First object <see cref="Unit"/>.</param>
		/// <param name="u2">Second object <see cref="Unit"/>.</param>
		/// <returns>The result of addition.</returns>
		public static Unit operator +(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 + v2, (nonPer, per) => nonPer + per);
		}

		/// <summary>
		/// Multiply the two objects <see cref="Unit"/>.
		/// </summary>
		/// <param name="u1">First object <see cref="Unit"/>.</param>
		/// <param name="u2">Second object <see cref="Unit"/>.</param>
		/// <returns>The result of the multiplication.</returns>
		public static Unit operator *(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 * v2, (nonPer, per) => nonPer * per);
		}

		/// <summary>
		/// Subtract the unit <see cref="Unit"/> from another.
		/// </summary>
		/// <param name="u1">First object <see cref="Unit"/>.</param>
		/// <param name="u2">Second object <see cref="Unit"/>.</param>
		/// <returns>The result of the subtraction.</returns>
		public static Unit operator -(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 - v2, (nonPer, per) => (u1.Type == UnitTypes.Percent ? (per - nonPer) : (nonPer - per)));
		}

		/// <summary>
		/// Divide the unit <see cref="Unit"/> to another.
		/// </summary>
		/// <param name="u1">First object <see cref="Unit"/>.</param>
		/// <param name="u2">Second object <see cref="Unit"/>.</param>
		/// <returns>The result of the division.</returns>
		public static Unit operator /(Unit u1, Unit u2)
		{
			return CreateResult(u1, u2, (v1, v2) => v1 / v2, (nonPer, per) => u1.Type == UnitTypes.Percent ? per / nonPer : nonPer / per);
		}

		/// <summary>
		/// Get the hash code of the object <see cref="Unit"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ Value.GetHashCode();
		}

		/// <summary>
		/// Compare <see cref="Unit"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(Unit other)
		{
			//var retVal = Type == other.Type && Value == other.Value;

			//if (Type == UnitTypes.Percent || Type == UnitTypes.Absolute || Type == UnitTypes.Limit)
			//	return retVal;

			//return retVal && CheckGetTypeValue(true) == other.CheckGetTypeValue(true);

			if (Type == other.Type)
				return Value == other.Value;

			if (Type == UnitTypes.Percent || other.Type == UnitTypes.Percent)
				return false;

			var curr = this;

			if (other.Type == UnitTypes.Absolute)
				curr = Convert(other.Type);
			else
				other = other.Convert(Type);

			return curr.Value == other.Value;
		}

		/// <summary>
		/// Compare <see cref="Unit"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		public override bool Equals(object other)
		{
			return base.Equals(other);
		}

		/// <summary>
		/// Compare two values in the inequality (if the value of different types, the convertion will be used).
		/// </summary>
		/// <param name="u1">First unit.</param>
		/// <param name="u2">Second unit.</param>
		/// <returns><see langword="true" />, if the values are equals, otherwise, <see langword="false" />.</returns>
		public static bool operator !=(Unit u1, Unit u2)
		{
			return !(u1 == u2);
		}

		/// <summary>
		/// Compare two values for equality (if the value of different types, the convertion will be used).
		/// </summary>
		/// <param name="u1">First unit.</param>
		/// <param name="u2">Second unit.</param>
		/// <returns><see langword="true" />, if the values are equals, otherwise, <see langword="false" />.</returns>
		public static bool operator ==(Unit u1, Unit u2)
		{
			if (ReferenceEquals(u1, null))
				return u2.IsNull();

			if (ReferenceEquals(u2, null))
				return false;

			return u1.OnEquals(u2);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			switch (Type)
			{
				case UnitTypes.Percent:
					return Value + "%";
				case UnitTypes.Absolute:
					return Value.To<string>();
				case UnitTypes.Step:
					return Value + (LocalizedStrings.ActiveLanguage == Languages.Russian ? "ø" : "s");
				case UnitTypes.Point:
					return Value + (LocalizedStrings.ActiveLanguage == Languages.Russian ? "ï" : "p");
				case UnitTypes.Limit:
					return Value + (LocalizedStrings.ActiveLanguage == Languages.Russian ? "ë" : "l");
				default:
					throw new InvalidOperationException(LocalizedStrings.UnknownUnitMeasurement.Put(Type));
			}
		}

		/// <summary>
		/// Cast the value to another type.
		/// </summary>
		/// <param name="destinationType">Destination value type.</param>
		/// <returns>Converted value.</returns>
		public Unit Convert(UnitTypes destinationType)
		{
			return Convert(destinationType, GetTypeValue);
		}

		/// <summary>
		/// Cast the value to another type.
		/// </summary>
		/// <param name="destinationType">Destination value type.</param>
		/// <param name="getTypeValue">The handler returns a value associated with <see cref="Unit.Type"/> (price or volume steps).</param>
		/// <returns>Converted value.</returns>
		public Unit Convert(UnitTypes destinationType, Func<UnitTypes, decimal?> getTypeValue)
		{
			if (Type == destinationType)
				return Clone();

			if (Type == UnitTypes.Percent || destinationType == UnitTypes.Percent)
				throw new InvalidOperationException(LocalizedStrings.PercentagesConvert);

			var value = (decimal)this;

			if (destinationType == UnitTypes.Point || destinationType == UnitTypes.Step)
			{
				if (getTypeValue == null)
					throw new ArgumentException(LocalizedStrings.UnitHandlerNotSet, "destinationType");

				switch (destinationType)
				{
					case UnitTypes.Point:
						var point = getTypeValue(UnitTypes.Point);

						if (point == null || point == 0)
							throw new InvalidOperationException("Price step cost is equal to zero.".Translate());

						value /= point.Value;
						break;
					case UnitTypes.Step:
						var step = getTypeValue(UnitTypes.Step);

						if (step == null || step == 0)
							throw new InvalidOperationException(LocalizedStrings.Str1546);

						value /= step.Value;
						break;
				}
			}

			return new Unit(value, destinationType, getTypeValue);
		}

		/// <summary>
		/// Check whether the first value is greater than the second.
		/// </summary>
		/// <param name="u1">First unit.</param>
		/// <param name="u2">Second unit.</param>
		/// <returns><see langword="true" />, if the first value is greater than the second, <see langword="false" />.</returns>
		public static bool operator >(Unit u1, Unit u2)
		{
			if (u1.IsNull())
				throw new ArgumentNullException("u1");

			if (u2.IsNull())
				throw new ArgumentNullException("u2");

			//if (u1.Type == UnitTypes.Limit || u2.Type == UnitTypes.Limit)
			//	throw new ArgumentException("Ëèìèòèðîâàííîå çíà÷åíèå íå ìîæåò ó÷àñòâîâàòü â àðèôìåòè÷åñêèõ îïåðàöèÿõ.");

			//if (u1.CheckGetTypeValue(false) != u2.CheckGetTypeValue(false))
			//	throw new ArgumentException("Ó îäíîé èç âåëè÷èí íå óñòàíîâëåíî ïîëó÷åíèå çíà÷åíèÿ.");

			if (u1.Type != u2.Type)
			{
				if (u1.Type == UnitTypes.Percent || u2.Type == UnitTypes.Percent)
					throw new ArgumentException(LocalizedStrings.PercentagesCannotCompare.Put(u1, u2));

				if (u2.Type == UnitTypes.Absolute)
					u1 = u1.Convert(u2.Type);
				else
					u2 = u2.Convert(u1.Type);
			}

			return u1.Value > u2.Value;
		}

		/// <summary>
		/// Check whether the first value is greater than or equal to the second.
		/// </summary>
		/// <param name="u1">First unit.</param>
		/// <param name="u2">Second unit.</param>
		/// <returns><see langword="true" />, if the first value is greater than or equal the second, otherwise, <see langword="false" />.</returns>
		public static bool operator >=(Unit u1, Unit u2)
		{
			return u1 == u2 || u1 > u2;
		}

		/// <summary>
		/// Check whether the first value is less than the second.
		/// </summary>
		/// <param name="u1">First unit.</param>
		/// <param name="u2">Second unit.</param>
		/// <returns><see langword="true" />, if the first value is less than the second, <see langword="false" />.</returns>
		public static bool operator <(Unit u1, Unit u2)
		{
			return u1 != u2 && !(u1 > u2);
		}

		/// <summary>
		/// Check whether the first value is less than or equal to the second.
		/// </summary>
		/// <param name="u1">First unit.</param>
		/// <param name="u2">Second unit.</param>
		/// <returns><see langword="true" />, if the first value is less than or equal to the second, <see langword="false" />.</returns>
		public static bool operator <=(Unit u1, Unit u2)
		{
			return !(u1 > u2);
		}

		/// <summary>
		/// Get the value with the opposite sign from the value <see cref="Unit.Value"/>.
		/// </summary>
		/// <param name="u">Unit.</param>
		/// <returns>Opposite value.</returns>
		public static Unit operator -(Unit u)
		{
			if (u == null)
				throw new ArgumentNullException("u");

			return new Unit
			{
				GetTypeValue = u.GetTypeValue,
				Type = u.Type,
				Value = -u.Value
			};
		}

		Unit IOperable<Unit>.Add(Unit other)
		{
			return this + other;
		}

		Unit IOperable<Unit>.Subtract(Unit other)
		{
			return this - other;
		}

		Unit IOperable<Unit>.Multiply(Unit other)
		{
			return this * other;
		}

		Unit IOperable<Unit>.Divide(Unit other)
		{
			return this / other;
		}
	}

	/// <summary>
	/// Extension class for <see cref="Unit"/>.
	/// </summary>
	public static class UnitHelper
	{
		/// <summary>
		/// Convert the <see cref="Int32"/> to percents.
		/// </summary>
		/// <param name="value"><see cref="Int32"/> value.</param>
		/// <returns>Percents.</returns>
		public static Unit Percents(this int value)
		{
			return Percents((decimal)value);
		}

		/// <summary>
		/// Convert the <see cref="Double"/> to percents.
		/// </summary>
		/// <param name="value"><see cref="Double"/> value.</param>
		/// <returns>Percents.</returns>
		public static Unit Percents(this double value)
		{
			return Percents((decimal)value);
		}

		/// <summary>
		/// Convert the <see cref="Decimal"/> to percents.
		/// </summary>
		/// <param name="value"><see cref="Decimal"/> value.</param>
		/// <returns>Percents.</returns>
		public static Unit Percents(this decimal value)
		{
			return new Unit(value, UnitTypes.Percent);
		}

		/// <summary>
		/// Convert string to <see cref="Unit"/>.
		/// </summary>
		/// <param name="str">String value of <see cref="Unit"/>.</param>
		/// <param name="getTypeValue">The handler returns a value associated with <see cref="Type"/> (price or volume steps).</param>
		/// <returns>Object <see cref="Unit"/>.</returns>
		public static Unit ToUnit(this string str, Func<UnitTypes, decimal?> getTypeValue = null)
		{
			if (str.IsEmpty())
				throw new ArgumentNullException("str");

			var lastSymbol = str.Last();

			if (char.IsDigit(lastSymbol))
				return new Unit(str.To<decimal>(), UnitTypes.Absolute);

			var value = str.Substring(0, str.Length - 1).To<decimal>();

			UnitTypes type;

			switch (lastSymbol)
			{
				case 'ø':
				case 's':
					if (getTypeValue == null)
						throw new ArgumentNullException("getTypeValue");

					type = UnitTypes.Step;
					break;
				case 'ï':
				case 'p':
					if (getTypeValue == null)
						throw new ArgumentNullException("getTypeValue");
			
					type = UnitTypes.Point;
					break;
				case '%':
					type = UnitTypes.Percent;
					break;
				case 'ë':
				case 'l':
					type = UnitTypes.Limit;
					break;
				default:
					throw new ArgumentException(LocalizedStrings.UnknownUnitMeasurement.Put(lastSymbol), "str");
			}

			return new Unit(value, type, getTypeValue);
		}
	}
}