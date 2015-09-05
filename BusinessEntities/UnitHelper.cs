namespace StockSharp.BusinessEntities
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Extension class for <see cref="Unit"/>.
	/// </summary>
	public static class UnitHelper2
	{
		/// <summary>
		/// To create from <see cref="Int32"/> the pips values.
		/// </summary>
		/// <param name="value"><see cref="Int32"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment is taken .</param>
		/// <returns>Pips.</returns>
		public static Unit Pips(this int value, Security security)
		{
			return Pips((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="Double"/> the pips values.
		/// </summary>
		/// <param name="value"><see cref="Double"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment is taken .</param>
		/// <returns>Pips.</returns>
		public static Unit Pips(this double value, Security security)
		{
			return Pips((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="Decimal"/> the pips values.
		/// </summary>
		/// <param name="value"><see cref="Decimal"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment is taken .</param>
		/// <returns>Pips.</returns>
		public static Unit Pips(this decimal value, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return new Unit(value, UnitTypes.Step, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// To create from <see cref="Int32"/> the points values.
		/// </summary>
		/// <param name="value"><see cref="Int32"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment cost is taken .</param>
		/// <returns>Points.</returns>
		public static Unit Points(this int value, Security security)
		{
			return Points((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="Double"/> the points values.
		/// </summary>
		/// <param name="value"><see cref="Double"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment cost is taken .</param>
		/// <returns>Points.</returns>
		public static Unit Points(this double value, Security security)
		{
			return Points((decimal)value, security);
		}

		/// <summary>
		/// To create from <see cref="Decimal"/> the points values.
		/// </summary>
		/// <param name="value"><see cref="Decimal"/> value.</param>
		/// <param name="security">The instrument from which information about the price increment cost is taken .</param>
		/// <returns>Points.</returns>
		public static Unit Points(this decimal value, Security security)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			return new Unit(value, UnitTypes.Point, type => GetTypeValue(security, type));
		}

		/// <summary>
		/// Convert string to <see cref="Unit"/>.
		/// </summary>
		/// <param name="str">String value of <see cref="Unit"/>.</param>
		/// <param name="security">Information about the instrument. Required when using <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
		/// <returns>Object <see cref="Unit"/>.</returns>
		public static Unit ToUnit2(this string str, Security security = null)
		{
			return str.ToUnit(t => GetTypeValue(security, t));
		}

		/// <summary>
		/// Cast the value to another type.
		/// </summary>
		/// <param name="unit">Source unit.</param>
		/// <param name="destinationType">Destination value type.</param>
		/// <param name="security">Information about the instrument. Required when using <see cref="UnitTypes.Point"/> и <see cref="UnitTypes.Step"/>.</param>
		/// <returns>Converted value.</returns>
		public static Unit Convert(this Unit unit, UnitTypes destinationType, Security security)
		{
			return unit.Convert(destinationType, type => GetTypeValue(security, type));
		}

		internal static decimal ShrinkPrice(this Security security, decimal price)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (security.PriceStep == null)
				throw new ArgumentException(LocalizedStrings.Str1546, "security");

			return price.Round(security.PriceStep ?? 1m, security.Decimals ?? 0, null);
		}

		/// <summary>
		/// To set the <see cref="Unit.GetTypeValue"/> property for the value.
		/// </summary>
		/// <param name="unit">Unit.</param>
		/// <param name="security">Security.</param>
		/// <returns>Unit.</returns>
		public static Unit SetSecurity(this Unit unit, Security security)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");

			unit.GetTypeValue = type => GetTypeValue(security, type);

			return unit;
		}

		private static decimal? GetTypeValue(Security security, UnitTypes type)
		{
			switch (type)
			{
				case UnitTypes.Point:
					if (security == null)
						throw new ArgumentNullException("security");

					return security.StepPrice;
				case UnitTypes.Step:
					if (security == null)
						throw new ArgumentNullException("security");

					return security.PriceStep;
				default:
					throw new ArgumentOutOfRangeException("type", type, LocalizedStrings.Str1291);
			}
		}
	}
}