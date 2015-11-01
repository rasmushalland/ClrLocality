using System;
using System.Collections.Generic;

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

		public IEnumerable<TValue> this[TKey key]
		{
			get
			{
				IndexRange range;
				if (!_indexDict.TryGetValue(key, out range))
					yield break;

				for (int i = 0; i < range.Count; i++)
					yield return _list[range.Index + i];
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

		internal static ArrayLookup<TKey, TValue> FromContiguous<TItem>(IEnumerable<TItem> items, Func<TItem, TKey> keySelector, Func<TItem, TValue> valueSelector)
		{
			var index = 0;
			TKey prevKey = default(TKey);
			var indexDict = new Dictionary<TKey, IndexRange>();
			var estimate = (items as ICollection<TValue>)?.Count;
			var list = estimate != null ? new List<TValue>(estimate.Value) : new List<TValue>();
			var comparer = EqualityComparer<TKey>.Default;
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