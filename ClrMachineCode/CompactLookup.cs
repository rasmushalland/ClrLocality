using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClrMachineCode
{
	public sealed class CompactLookup<TKey, TValue> : ILookup<TKey, TValue>
	{
		private static readonly TValue[] _emptyValues = new TValue[0];
		private readonly Dictionary<TKey, CompactLookupIndexRange> _indexDict;
		private readonly List<TValue> _list;

		private CompactLookup(Dictionary<TKey, CompactLookupIndexRange> indexDict, List<TValue> list)
		{
			_indexDict = indexDict;
			_list = list;
		}

		public bool Contains(TKey key) => _indexDict.ContainsKey(key);

		public int Count => _indexDict.Count;

		IEnumerable<TValue> ILookup<TKey, TValue>.this[TKey key] => this[key];

		public Grouping this[TKey key]
		{
			get
			{
				CompactLookupIndexRange range;
				if (!_indexDict.TryGetValue(key, out range))
					return new Grouping(this, -1, 0, key);
				return new Grouping(this, range.Index, range.Count, key);
			}
		}

		#region IEnumerable<IGrouping>

		public IEnumerator<Grouping> GetEnumerator()
		{
			foreach (var kvp in _indexDict)
			{
				yield return new Grouping(this, kvp.Value.Index, kvp.Value.Count, kvp.Key);
			}
		}

		IEnumerator<IGrouping<TKey, TValue>> IEnumerable<IGrouping<TKey, TValue>>.GetEnumerator()
		{
			foreach (var kvp in _indexDict)
			{
				yield return new Grouping(this, kvp.Value.Index, kvp.Value.Count, kvp.Key);
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		#endregion

		#region IGrouping

		public struct Grouping : IGrouping<TKey, TValue>, ICollection<TValue>, IReadOnlyCollection<TValue>
		{
			private readonly CompactLookup<TKey, TValue> _lookup;
			private readonly int _startIndex;
			private readonly int _count;

			public Grouping(CompactLookup<TKey, TValue> lookup, int startIndex, int count, TKey key)
			{
				_lookup = lookup;
				_startIndex = startIndex;
				_count = count;
				Key = key;
			}

			public TKey Key { get; }

			public Enumerator GetEnumerator() => new Enumerator(_lookup, _startIndex, _count);

			/// <summary>
			/// Only relevant when casting to interface: It avoids allocating if there are no elements;
			/// </summary>
			/// <returns></returns>
			public ICollection<TValue> AsCollection() => _count == 0 ? (ICollection<TValue>) _emptyValues : this;

			/// <summary>
			/// Only relevant when casting to interface: It avoids allocating if there are no elements;
			/// </summary>
			/// <returns></returns>
			public IReadOnlyCollection<TValue> AsReadOnlyCollection() => _count == 0 ? (IReadOnlyCollection<TValue>) _emptyValues : this;

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public int Count => _count;

			#region ICollection<TValue>

			public void Add(TValue item)
			{
				throw new InvalidOperationException();
			}

			public void Clear()
			{
				throw new InvalidOperationException();
			}

			public bool Contains(TValue item)
			{
				var comp = EqualityComparer<TValue>.Default;
				foreach (var v in this)
				{
					if (comp.Equals(v, item))
						return true;
				}
				return false;
			}

			public void CopyTo(TValue[] array, int arrayIndex)
			{
				var index = -1;
				foreach (var v in this)
				{
					index++;
					array[arrayIndex + index] = v;
				}
			}

			public bool Remove(TValue item)
			{
				throw new InvalidOperationException();
			}

			public bool IsReadOnly => true;

			#endregion
		}

		public struct Enumerator : IEnumerator<TValue>
		{
			private readonly CompactLookup<TKey, TValue> _lookup;
			private readonly int _startIndex;
			private readonly int _count;
			private int _offset;

			internal Enumerator(CompactLookup<TKey, TValue> lookup, int startIndex, int count)
			{
				_lookup = lookup;
				_startIndex = startIndex;
				_count = count;
				_offset = -1;
			}

			public TValue Current => _lookup._list[_startIndex + _offset];

			object IEnumerator.Current => Current;

			public bool MoveNext()
			{
				_offset++;
				return _offset < _count;
			}

			#region Reset, Dispose

			public void Dispose()
			{
				// nothing.
			}

			public void Reset()
			{
				throw new NotImplementedException();
			}

			#endregion
		}

		#endregion

		internal static CompactLookup<TKey, TValue> FromContiguous<TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			var comparer = EqualityComparer<TKey>.Default;
			var estimate = (items as ICollection<TValue>)?.Count;
			var indexDict = estimate != null
				? new Dictionary<TKey, CompactLookupIndexRange>(estimate.Value, comparer)
				: new Dictionary<TKey, CompactLookupIndexRange>(comparer);
			var list = estimate != null ? new List<TValue>(estimate.Value) : new List<TValue>();
			int curStartIndex = -1;
			var index = 0;
			TKey prevKey = default(TKey);
			foreach (var value in items)
			{
				var key = keySelector(value);
				var isNewKey = index == 0 || !comparer.Equals(key, prevKey);
				if (isNewKey)
				{
					if (index > 0)
						indexDict.Add(prevKey, new CompactLookupIndexRange(curStartIndex, index - curStartIndex));

					curStartIndex = index;
					prevKey = key;
				}
				list.Add(valueSelector(value));
				index++;
			}
			if (index >= 1)
				indexDict.Add(prevKey, new CompactLookupIndexRange(curStartIndex, index - curStartIndex));

			return new CompactLookup<TKey, TValue>(indexDict, list);
		}

		internal static CompactLookup<TKey, TValue> FromGroupings<TItem>(IEnumerable<IGrouping<TKey, TItem>> groupings, Func<TItem, TValue> elementSelector)
		{
			var comparer = EqualityComparer<TKey>.Default;
			var estimate = (groupings as ICollection<TValue>)?.Count;
			var indexDict = estimate != null
				? new Dictionary<TKey, CompactLookupIndexRange>(estimate.Value, comparer)
				: new Dictionary<TKey, CompactLookupIndexRange>(comparer);
			var list = estimate != null ? new List<TValue>(estimate.Value) : new List<TValue>();
			var index = 0;

			foreach (var g in groupings)
			{
				var curStartIndex = index;
				foreach (var item in g)
				{
					list.Add(elementSelector(item));

					index++;
				}
				if (index != curStartIndex)
					indexDict.Add(g.Key, new CompactLookupIndexRange(curStartIndex, index - curStartIndex));
			}

			return new CompactLookup<TKey, TValue>(indexDict, list);
		}
	}

	struct CompactLookupIndexRange
	{
		public readonly int Index;
		public readonly int Count;

		public CompactLookupIndexRange(int index, int count)
		{
			Index = index;
			Count = count;
		}
	}

	public static class CompactLookup
	{
		public static CompactLookup<TKey, TValue> ToCompactLookupFromContiguous<TKey, TValue, TItem>(
			this IEnumerable<TItem> items,
			Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			return CompactLookup<TKey, TValue>.FromContiguous(items, keySelector, valueSelector);
		}

		public static CompactLookup<TKey, TValue> ToCompactLookup<TKey, TValue, TItem>(
			this IEnumerable<IGrouping<TKey, TItem>> groupings,
			Func<TItem, TValue> elementSelector)
		{
			return CompactLookup<TKey, TValue>.FromGroupings(groupings, elementSelector);
		}

		public static CompactLookup<TKey, TValue> ToCompactLookup<TKey, TValue>(
			this IEnumerable<IGrouping<TKey, TValue>> groupings)
		{
			return CompactLookup<TKey, TValue>.FromGroupings(groupings, g => g);
		}
	}
}