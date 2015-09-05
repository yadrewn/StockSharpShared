namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// ������, ����������� �� ������������. ��������, ��� ������� ������ ��� ��������� ��� ������ ���������.
	/// </summary>
	public abstract class IndexSecurity : BasketSecurity
	{
		/// <summary>
		/// ���������������� <see cref="IndexSecurity"/>.
		/// </summary>
		protected IndexSecurity()
		{
			Type = SecurityTypes.Index;
		}

		/// <summary>
		/// ��������� �������� �������.
		/// </summary>
		/// <param name="prices">���� ��������� ������������ ������� <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>�������� �������.</returns>
		public abstract decimal? Calculate(IDictionary<Security, decimal> prices);
	}

	/// <summary>
	/// ������� ������������, ���������� �� ����� <see cref="Weights"/>.
	/// </summary>
	public class WeightedIndexSecurity : IndexSecurity
	{
		private sealed class WeightsDictionary : CachedSynchronizedDictionary<Security, decimal>
		{
			private readonly WeightedIndexSecurity _parent;

			public WeightsDictionary(WeightedIndexSecurity parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;
			}

			public override void Add(Security key, decimal value)
			{
				base.Add(key, value);
				RefreshName();
			}

			public override bool Remove(Security key)
			{
				if (base.Remove(key))
				{
					RefreshName();
					return true;
				}

				return false;
			}

			public override void Clear()
			{
				base.Clear();
				RefreshName();
			}

			private void RefreshName()
			{
				_parent.Id = GetName(s => s.Id);
				_parent.Code = GetName(s => s.Code);
				_parent.Name = GetName(s => s.Name);
			}

			private string GetName(Func<Security, string> getSecurityName)
			{
				return this.Select(p => "{0} * {1}".Put(p.Value, getSecurityName(p.Key))).Join(", ");
			}
		}

		/// <summary>
		/// ������� <see cref="WeightedIndexSecurity"/>.
		/// </summary>
		public WeightedIndexSecurity()
		{
			_weights = new WeightsDictionary(this);
		}

		private readonly WeightsDictionary _weights;

		/// <summary>
		/// ����������� � �� ������� ������������ � �������.
		/// </summary>
		public SynchronizedDictionary<Security, decimal> Weights
		{
			get { return _weights; }
		}

		/// <summary>
		/// �����������, �� ������� ������� ������ �������.
		/// </summary>
		public override IEnumerable<Security> InnerSecurities
		{
			get { return _weights.CachedKeys; }
		}

		/// <summary>
		/// ��������� �������� �������.
		/// </summary>
		/// <param name="prices">���� ��������� ������������ ������� <see cref="BasketSecurity.InnerSecurities"/>.</param>
		/// <returns>�������� �������.</returns>
		public override decimal? Calculate(IDictionary<Security, decimal> prices)
		{
			if (prices == null)
				throw new ArgumentNullException("prices");

			if (prices.Count != _weights.Count || !InnerSecurities.All(prices.ContainsKey))
				return null;

			return prices.Sum(pair => _weights[pair.Key] * pair.Value);
		}

		/// <summary>
		/// ������� ����� <see cref="Security"/>.
		/// </summary>
		/// <returns>����� �������.</returns>
		public override Security Clone()
		{
			var clone = new WeightedIndexSecurity();
			clone.Weights.AddRange(Weights.SyncGet(d => d.ToArray()));
			CopyTo(clone);
			return clone;
		}
	}
}