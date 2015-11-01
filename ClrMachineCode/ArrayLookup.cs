using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace ClrMachineCode
{
	public sealed class ArrayLookup<TKey, TValue>
	{
		private readonly Dictionary<TKey, IndexRange> _indexDict;
		private readonly List<TValue> _list;

		private ArrayLookup(Dictionary<TKey, IndexRange> indexDict, List<TValue> list)
		{
			_indexDict = indexDict;
			_list = list;
		}

		public Enumerable this[TKey key]
		{
			get
			{
				IndexRange range;
				if (!_indexDict.TryGetValue(key, out range))
					return new Enumerable(this, -1, 0);
				return new Enumerable(this, range.Index, range.Count);

				//IndexRange range;
				//if (!_indexDict.TryGetValue(key, out range))
				//	yield break;

				//for (int i = 0; i < range.Count; i++)
				//	yield return _list[range.Index + i];
			}
		} 

		struct IndexRange
		{
			public readonly int Index;
			public readonly int Count;

			public IndexRange(int index, int count)
			{
				Index = index;
				Count = count;
			}
		}

		public struct Enumerable : IEnumerable<TValue>
		{
			private readonly ArrayLookup<TKey, TValue> _lookup;
			private readonly int _startIndex;
			private readonly int _count;

			public Enumerable(ArrayLookup<TKey, TValue> lookup, int startIndex, int count)
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
			private readonly ArrayLookup<TKey, TValue> _lookup;
			private readonly int _startIndex;
			private readonly int _count;
			private int _offset;

			internal Enumerator(ArrayLookup<TKey, TValue> lookup, int startIndex, int count)
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

		internal static ArrayLookup<TKey, TValue> FromContiguous<TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			var index = 0;
			TKey prevKey = default(TKey);
			var comparer = EqualityComparer<TKey>.Default;
			var estimate = (items as ICollection<TValue>)?.Count;
			//var indexDict = new Dictionary<TKey, IndexRange>();
			//var indexDict = estimate != null ? new Dictionary<TKey, IndexRange>(estimate.Value) : new Dictionary<TKey, IndexRange>();
			var indexDict = estimate != null ? new Dictionary<TKey, IndexRange>(estimate.Value, comparer) : new Dictionary<TKey, IndexRange>(comparer);
			var list = estimate != null ? new List<TValue>(estimate.Value) : new List<TValue>();
			int curStartIndex = -1;
			foreach (var value in items)
			{
				var key = keySelector(value);
				var isNewKey = index == 0 || !comparer.Equals(key, prevKey);
				if (isNewKey)
				{
					if (index > 0)
						indexDict.Add(prevKey, new IndexRange(curStartIndex, index - curStartIndex));

					curStartIndex = index;
					prevKey = key;
				}
				list.Add(valueSelector(value));
				index++;
			}
			if (index >= 1)
				indexDict.Add(prevKey, new IndexRange(curStartIndex, index - curStartIndex));

			return new ArrayLookup<TKey, TValue>(indexDict, list);
		}
	}

	public static class ArrayLookup
	{
		public static ArrayLookup<TKey, TValue> FromContiguous<TKey, TValue, TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			return ArrayLookup<TKey, TValue>.FromContiguous(items, keySelector, valueSelector);
		}

		public static ArrayLookup<TKey, TValue> ToLookupFromContiguous<TKey, TValue, TItem>(this IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			return ArrayLookup<TKey, TValue>.FromContiguous(items, keySelector, valueSelector);
		}
	}
}