namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using Ecng.Common;

	/// <summary>
	/// Currency.
	/// </summary>
	[DataContract]
	[Serializable]
	public class Currency : Equatable<Currency>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Currency"/>.
		/// </summary>
		public Currency()
		{
			Type = CurrencyTypes.RUB;
		}

		/// <summary>
		/// Currency type. The default is <see cref="CurrencyTypes.RUB"/>.
		/// </summary>
		[DataMember]
		public CurrencyTypes Type { get; set; }

		/// <summary>
		/// Absolute value in <see cref="CurrencyTypes"/>.
		/// </summary>
		[DataMember]
		public decimal Value { get; set; }

		/// <summary>
		/// Create a copy of <see cref="Currency"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Currency Clone()
		{
			return new Currency { Type = Type, Value = Value };
		}

		/// <summary>
		/// Compare <see cref="Currency"/> on the equivalence.
		/// </summary>
		/// <param name="other">Another value with which to compare.</param>
		/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
		protected override bool OnEquals(Currency other)
		{
			return Type == other.Type && Value == other.Value;
		}

		/// <summary>
		/// Get the hash code of the object <see cref="Currency"/>.
		/// </summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return Type.GetHashCode() ^ Value.GetHashCode();
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} {1}".Put(Value, Type);
		}

		/// <summary>
		/// Cast <see cref="Decimal"/> object to the type <see cref="Currency"/>.
		/// </summary>
		/// <param name="value"><see cref="Decimal"/> value.</param>
		/// <returns>Object <see cref="Currency"/>.</returns>
		public static implicit operator Currency(decimal value)
		{
			return new Currency { Value = value };
		}

		/// <summary>
		/// Cast object from <see cref="Currency"/> to <see cref="Decimal"/>.
		/// </summary>
		/// <param name="unit">Object <see cref="Currency"/>.</param>
		/// <returns><see cref="Decimal"/> value.</returns>
		public static explicit operator decimal(Currency unit)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");

			return unit.Value;
		}

		/// <summary>
		/// Add the two objects <see cref="Currency"/>.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of addition.</returns>
		/// <remarks>
		/// The values must be the same <see cref="Currency.Type"/>.
		/// </remarks>
		public static Currency operator +(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 + (decimal)c2;
		}

		/// <summary>
		/// Subtract one value from another value.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of the subtraction.</returns>
		public static Currency operator -(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 - (decimal)c2;
		}

		/// <summary>
		/// Multiply one value to another.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of the multiplication.</returns>
		public static Currency operator *(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 * (decimal)c2;
		}

		/// <summary>
		/// Divide one value to another.
		/// </summary>
		/// <param name="c1">First object <see cref="Currency"/>.</param>
		/// <param name="c2">Second object <see cref="Currency"/>.</param>
		/// <returns>The result of the division.</returns>
		public static Currency operator /(Currency c1, Currency c2)
		{
			if (c1 == null)
				throw new ArgumentNullException("c1");

			if (c2 == null)
				throw new ArgumentNullException("c2");

			return (decimal)c1 / (decimal)c2;
		}
	}

	/// <summary>
	/// Extension class for <see cref="Currency"/>.
	/// </summary>
	public static class CurrencyHelper
	{
		/// <summary>
		/// Cast <see cref="Decimal"/> to <see cref="Currency"/>.
		/// </summary>
		/// <param name="value">Currency value.</param>
		/// <param name="type">Currency type.</param>
		/// <returns>Currency.</returns>
		public static Currency ToCurrency(this decimal value, CurrencyTypes type)
		{
			return new Currency { Type = type, Value = value };
		}
	}
}