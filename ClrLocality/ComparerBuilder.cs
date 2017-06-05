﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace ClrLocality
{
	/// <summary>
	/// The ComparerBuilder classes are handy for constructing implementations of <see cref="IComparer{T}"/>
	/// with a lambda syntax and convenience close to that of "normal" sorting with .OrderBy().
	/// </summary>
	public static class ComparerBuilder
	{
		public static ComparerBuilderBegin<TItem> Begin<TItem>()
		{
			return new ComparerBuilderBegin<TItem>();
		}
	}

	public sealed class ComparerBuilderBegin<TItem>
	{
		public ComparerBuilder<TItem> OrderBy<TKey>(Func<TItem, TKey> keySelector, IComparer<TKey> comparer = null)
		{
			return new ComparerBuilder<TItem, TKey>(null, keySelector, comparer, false);
		}
		public ComparerBuilder<TItem> OrderByDescending<TKey>(Func<TItem, TKey> keySelector, IComparer<TKey> comparer = null)
		{
			return new ComparerBuilder<TItem, TKey>(null, keySelector, comparer, true);
		}
	}

	public abstract class ComparerBuilder<TItem>
	{
		protected ComparerBuilder<TItem> _previous;

		public ComparerBuilder<TItem> ThenBy<TKey>(Func<TItem, TKey> keySelector, IComparer<TKey> comparer = null)
		{
			return new ComparerBuilder<TItem, TKey>(this, keySelector, comparer, false);
		}
		public ComparerBuilder<TItem> ThenByDescending<TKey>(Func<TItem, TKey> keySelector, IComparer<TKey> comparer = null)
		{
			return new ComparerBuilder<TItem, TKey>(this, keySelector, comparer, true);
		}

		protected abstract Comparison<TItem> CreateComparison();
		public IComparer<TItem> End()
		{
			var list = new List<Comparison<TItem>>();
			var cur = this;
			while (cur != null)
			{
				list.Add(cur.CreateComparison());
				cur = cur._previous;
			}

			if (list.Count == 1)
				return Comparer<TItem>.Create(list.Single());

			list.Reverse();

			return Comparer<TItem>.Create((x, y) => {
				foreach (var comparison in list)
				{
					var res = comparison(x, y);
					if (res != 0)
						return res;
				}
				return 0;
			});
		}
	}

	public sealed class ComparerBuilder<TItem, TKey> : ComparerBuilder<TItem>
	{
		private readonly Func<TItem, TKey> _keySelector;
		private readonly IComparer<TKey> _comparer;
		private readonly int _multiplier;

		public ComparerBuilder(ComparerBuilder<TItem> previous, Func<TItem, TKey> keySelector, IComparer<TKey> comparer, bool descending)
		{
			_previous = previous;
			_keySelector = keySelector;
			_comparer = comparer ?? Comparer<TKey>.Default;
			_multiplier = descending ? -1 : 1;
		}

		protected override Comparison<TItem> CreateComparison()
		{
			return (x, y) => _comparer.Compare(_keySelector(x), _keySelector(y)) * _multiplier;
		}
	}
}