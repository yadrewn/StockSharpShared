namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// The interface for all trading types that have the property <see cref="IExtendableEntity.ExtensionInfo"/> for keeping extended information.
	/// </summary>
	public interface IExtendableEntity
	{
		/// <summary>
		/// Extended information.
		/// </summary>
		/// <remarks>
		/// Required when extra information is stored in the program.
		/// </remarks>
		IDictionary<object, object> ExtensionInfo { get; set; }
	}

	/// <summary>
	/// Extension class for <see cref="IExtendableEntity.ExtensionInfo"/>.
	/// </summary>
	public static class ExtandableEntityHelper
	{
		/// <summary>
		/// Add value into <see cref="IExtendableEntity.ExtensionInfo"/>.
		/// </summary>
		/// <param name="entity">Entity.</param>
		/// <param name="key">Key.</param>
		/// <param name="value">Value.</param>
		public static void AddValue(this IExtendableEntity entity, object key, object value)
		{
			entity.GetExtInfo(true)[key] = value;
		}

		/// <summary>
		/// Get value from <see cref="IExtendableEntity.ExtensionInfo"/>.
		/// </summary>
		/// <typeparam name="T">Value type.</typeparam>
		/// <param name="entity">Entity.</param>
		/// <param name="key">Key.</param>
		/// <returns>Value.</returns>
		public static T GetValue<T>(this IExtendableEntity entity, object key)
		{
			var info = entity.GetExtInfo(false);

			if (info == null)
				return default(T);

			return (T)(info.TryGetValue(key) ?? default(T));
		}

		private static IDictionary<object, object> GetExtInfo(this IExtendableEntity entity, bool createIfNotExist)
		{
			if (entity == null)
				throw new ArgumentNullException("entity");

			var info = entity.ExtensionInfo;

			if (info == null && createIfNotExist)
			{
				info = new SynchronizedDictionary<object, object>();
				entity.ExtensionInfo = info;
			}

			return info;
		}
	}
}