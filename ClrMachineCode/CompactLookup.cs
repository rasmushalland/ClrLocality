using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ClrMachineCode
{
	public sealed class CompactLookup<TKey, TValue> : ILookup<TKey, TValue>
	{
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

		public Enumerable this[TKey key]
		{
			get
			{
				CompactLookupIndexRange range;
				if (!_indexDict.TryGetValue(key, out range))
					return new Enumerable(this, -1, 0);
				return new Enumerable(this, range.Index, range.Count);
			}
		}

		#region IEnumerable<IGrouping>

		public IEnumerator<Grouping> GetEnumerator()
		{
			foreach (var kvp in _indexDict)
			{
				yield return new Grouping(kvp.Key, new Enumerable(this, kvp.Value.Index, kvp.Value.Count));
			}
		}

		IEnumerator<IGrouping<TKey, TValue>> IEnumerable<IGrouping<TKey, TValue>>.GetEnumerator()
		{
			foreach (var kvp in _indexDict)
			{
				yield return new Grouping(kvp.Key, new Enumerable(this, kvp.Value.Index, kvp.Value.Count));
			}
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public struct Grouping : IGrouping<TKey, TValue>
		{
			private readonly Enumerable _enumerable;

			public Grouping(TKey key, Enumerable enumerable)
			{
				Key = key;
				_enumerable = enumerable;
			}

			public IEnumerator<TValue> GetEnumerator() => _enumerable.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public TKey Key { get; }
		}

		#endregion

		#region Enumerate single grouping.

		public struct Enumerable : IEnumerable<TValue>
		{
			private readonly CompactLookup<TKey, TValue> _lookup;
			private readonly int _startIndex;
			private readonly int _count;

			public Enumerable(CompactLookup<TKey, TValue> lookup, int startIndex, int count)
			{
				_lookup = lookup;
				_startIndex = startIndex;
				_count = count;
			}

			public Enumerator GetEnumerator() => new Enumerator(_lookup, _startIndex, _count);

			IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator() => GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
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
			var index = 0;
			TKey prevKey = default(TKey);
			var comparer = EqualityComparer<TKey>.Default;
			var estimate = (items as ICollection<TValue>)?.Count;
			var indexDict = estimate != null ? new Dictionary<TKey, CompactLookupIndexRange>(estimate.Value, comparer) : new Dictionary<TKey, CompactLookupIndexRange>(comparer);
			var list = estimate != null ? new List<TValue>(estimate.Value) : new List<TValue>();
			int curStartIndex = -1;
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
		public static CompactLookup<TKey, TValue> FromContiguous<TKey, TValue, TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			return CompactLookup<TKey, TValue>.FromContiguous(items, keySelector, valueSelector);
		}

		public static CompactLookup<TKey, TValue> ToCompactLookupFromContiguous<TKey, TValue, TItem>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			return CompactLookup<TKey, TValue>.FromContiguous(items, keySelector, valueSelector);
		}

		//public static CompactLookup<TKey, TValue> ToCompactLookupFromContiguous<TKey, TValue>(this IEnumerable<IGrouping<TKey, TValue>> groupings)
		//{
		//	return CompactLookup<TKey, TValue>.FromContiguous(groupings.SelectMany(g => g.Select()), keySelector, valueSelector);
		//}
	}
}