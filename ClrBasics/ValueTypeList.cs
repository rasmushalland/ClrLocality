using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using JetBrains.Annotations;

namespace ClrBasics
{
	/// <summary>
	///     A value type version of <see cref="List{T}" />, saving allocation of the List object and having to go through that
	///     object when using the list.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[DebuggerDisplay("Count = {Count}")]
	[Serializable]
	public struct ValueTypeList<T> : IList<T>, IReadOnlyList<T>
	{
		private const int DefaultCapacity = 4;

		[CanBeNull] private T[] _items;
		private int _size;
		private int _version;

		public T[] TheArray => _items ?? EmptyArray;

		private static readonly T[] EmptyArray = new T[0];

		public ValueTypeList(int capacity)
		{
			if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity));

			_items = capacity == 0 ? EmptyArray : new T[capacity];

			_size = 0;
			_version = 0;
		}

		public ValueTypeList(IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));
			_version = 0;
			_size = 0;

			var c = collection as ICollection<T>;
			if (c != null)
			{
				var count = c.Count;
				if (count == 0)
					_items = EmptyArray;
				else
				{
					_items = new T[count];
					c.CopyTo(_items, 0);
					_size = count;
				}
			}
			else
			{
				_items = EmptyArray;

				foreach (var item in collection)
					Add(item);
			}
		}

		public int Capacity
		{
			get { return _items.Length; }
			set
			{
				if (value < _size)
					throw new ArgumentOutOfRangeException(nameof(value));

				if (_items == null || value != _items.Length)
				{
					if (value > 0)
					{
						var newItems = new T[value];
						if (_size > 0)
							Array.Copy(_items, 0, newItems, 0, _size);
						_items = newItems;
					}
					else
						_items = EmptyArray;
				}
			}
		}

		public int Count => _size;

		bool ICollection<T>.IsReadOnly => false;

		public T this[int index]
		{
			get
			{
				// Following trick can reduce the range check by one
				if ((uint) index >= (uint) _size)
					throw new ArgumentOutOfRangeException(nameof(index));

				return _items[index];
			}

			set
			{
				if ((uint) index >= (uint) _size)
					throw new ArgumentOutOfRangeException(nameof(index));
				_items[index] = value;
				_version++;
			}
		}

		public void Add(T item)
		{
			if (_items == null || _size == _items.Length) EnsureCapacity(_size + 1);
			_items[_size++] = item;
			_version++;
		}

		public void AddRange(IEnumerable<T> collection) =>
			InsertRange(_size, collection);

		public ReadOnlyCollection<T> AsReadOnly() =>
			new ReadOnlyCollection<T>(this);

		public int BinarySearch(int index, int count, T item, IComparer<T> comparer)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (_size - index < count)
				throw new ArgumentException("Indices");

			return Array.BinarySearch(_items ?? EmptyArray, index, count, item, comparer);
		}

		public int BinarySearch(T item) =>
			BinarySearch(0, Count, item, null);

		public int BinarySearch(T item, IComparer<T> comparer) =>
			BinarySearch(0, Count, item, comparer);


		// Clears the contents of List.
		public void Clear()
		{
			if (_size > 0)
			{
				Array.Clear(_items, 0, _size);
				// Don't need to doc this but we clear the elements so that the gc can reclaim the references.
				_size = 0;
			}
			_version++;
		}

		public bool Contains(T item)
		{
			if (item == null)
			{
				for (var i = 0; i < _size; i++)
					if (_items[i] == null)
						return true;
				return false;
			}
			var c = EqualityComparer<T>.Default;
			for (var i = 0; i < _size; i++)
			{
				if (c.Equals(_items[i], item)) return true;
			}
			return false;
		}

		public ValueTypeList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
		{
			if (converter == null)
				throw new ArgumentNullException(nameof(converter));

			var list = new ValueTypeList<TOutput>(_size);
			for (var i = 0; i < _size; i++)
				list._items[i] = converter(_items[i]);
			list._size = _size;
			return list;
		}

		public void CopyTo(T[] array) =>
			CopyTo(array, 0);

		public void CopyTo(int index, T[] array, int arrayIndex, int count)
		{
			if (_size - index < count)
				throw new ArgumentException("Bad indices.");

			// Delegate rest of error checking to Array.Copy.
			Array.Copy(_items ?? EmptyArray, index, array, arrayIndex, count);
		}

		public void CopyTo(T[] array, int arrayIndex) =>
			Array.Copy(_items ?? EmptyArray, 0, array, arrayIndex, _size);

		private void EnsureCapacity(int min)
		{
			if (_items == null || _items.Length < min)
			{
				var newCapacity = _items == null || _items.Length == 0 ? DefaultCapacity : _items.Length*2;
				if (newCapacity < min) newCapacity = min;
				Capacity = newCapacity;
			}
		}

		public bool Exists(Predicate<T> match) =>
			FindIndex(match) != -1;

		public T Find(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));
			if (_items == null)
				return default(T);

			for (var i = 0; i < _size; i++)
			{
				if (match(_items[i]))
					return _items[i];
			}
			return default(T);
		}

		public ValueTypeList<T> FindAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			var list = new ValueTypeList<T>();
			if (_items == null)
				return list;
			for (var i = 0; i < _size; i++)
			{
				if (match(_items[i]))
					list.Add(_items[i]);
			}
			return list;
		}

		public int FindIndex(Predicate<T> match) =>
			FindIndex(0, _size, match);

		public int FindIndex(int startIndex, Predicate<T> match) =>
			FindIndex(startIndex, _size - startIndex, match);

		public int FindIndex(int startIndex, int count, Predicate<T> match)
		{
			if ((uint) startIndex > (uint) _size)
				throw new ArgumentOutOfRangeException(nameof(startIndex));

			if (count < 0 || startIndex > _size - count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (_items == null)
				return -1;

			var endIndex = startIndex + count;
			for (var i = startIndex; i < endIndex; i++)
				if (match(_items[i])) return i;
			return -1;
		}

		public T FindLast(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (_items == null)
				return default(T);

			for (var i = _size - 1; i >= 0; i--)
			{
				if (match(_items[i]))
					return _items[i];
			}
			return default(T);
		}

		public int FindLastIndex(Predicate<T> match) =>
			FindLastIndex(_size - 1, _size, match);

		public int FindLastIndex(int startIndex, Predicate<T> match) =>
			FindLastIndex(startIndex, startIndex + 1, match);

		public int FindLastIndex(int startIndex, int count, Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (_size == 0)
			{
				// Special case for 0 length List
				if (startIndex != -1)
					throw new ArgumentOutOfRangeException(nameof(startIndex));
			}
			else
			{
				// Make sure we're not out of range            
				if ((uint) startIndex >= (uint) _size)
					throw new ArgumentOutOfRangeException(nameof(startIndex));
			}

			// 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
			if (count < 0 || startIndex - count + 1 < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_items == null)
				return -1;

			var endIndex = startIndex - count;
			for (var i = startIndex; i > endIndex; i--)
			{
				if (match(_items[i]))
					return i;
			}
			return -1;
		}

		public void ForEach(Action<T> action)
		{
			if (action == null)
				throw new ArgumentNullException(nameof(action));

			if (_items == null)
				return;

			var version = _version;

			for (var i = 0; i < _size; i++)
			{
				if (version != _version)
					break;
				action(_items[i]);
			}

			if (version != _version)
				throw new InvalidOperationException("Enum");
		}

		public Enumerator GetEnumerator() => new Enumerator(this);

		IEnumerator IEnumerable.GetEnumerator() => new Enumerator(this);

		IEnumerator<T> IEnumerable<T>.GetEnumerator() => new Enumerator(this);

		public ValueTypeList<T> GetRange(int index, int count)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_size - index < count)
				throw new ArgumentException("Indices.");

			var list = new ValueTypeList<T>(count);
			if (_items == null)
				return list;

			Array.Copy(_items, index, list._items, 0, count);
			list._size = count;
			return list;
		}

		public int IndexOf(T item) => Array.IndexOf(_items ?? EmptyArray, item, 0, _size);

		public int IndexOf(T item, int index)
		{
			if (index > _size)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_items == null)
				return -1;

			return Array.IndexOf(_items, item, index, _size - index);
		}

		public int IndexOf(T item, int index, int count)
		{
			if (index > _size)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0 || index > _size - count)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_items == null)
				return -1;

			return Array.IndexOf(_items, item, index, count);
		}

		public void Insert(int index, T item)
		{
			// Note that insertions at the end are legal.
			if ((uint) index > (uint) _size)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (_items == null || _size == _items.Length) EnsureCapacity(_size + 1);
			if (index < _size)
				Array.Copy(_items, index, _items, index + 1, _size - index);
			_items[index] = item;
			_size++;
			_version++;
		}

		public void InsertRange(int index, IEnumerable<T> collection)
		{
			if (collection == null)
				throw new ArgumentNullException(nameof(collection));

			if ((uint) index > (uint) _size)
				throw new ArgumentOutOfRangeException(nameof(index));

			var c = collection as ICollection<T>;
			if (c != null)
			{
				var count = c.Count;
				if (count > 0)
				{
					EnsureCapacity(_size + count);
					if (index < _size)
						Array.Copy(_items, index, _items, index + count, _size - index);

					// If we're inserting a List into itself, we want to be able to deal with that.
					if (collection is ValueTypeList<T> && ((ValueTypeList<T>) collection)._items == _items)
					{
						// Copy first part of _items to insert location
						Array.Copy(_items, 0, _items, index, index);
						// Copy last part of _items back to inserted location
						Array.Copy(_items, index + count, _items, index*2, _size - index);
					}
					else
					{
						// Should problably copy directly to _items...
						var itemsToInsert = new T[count];
						c.CopyTo(itemsToInsert, 0);
						itemsToInsert.CopyTo(_items, index);
					}
					_size += count;
				}
			}
			else
			{
				foreach (var item in collection)
					Insert(index++, item);
			}
			_version++;
		}

		public int LastIndexOf(T item)
		{
			if (_size == 0)
				return -1;
			return LastIndexOf(item, _size - 1, _size);
		}

		public int LastIndexOf(T item, int index)
		{
			if (index >= _size)
				throw new ArgumentOutOfRangeException(nameof(index));
			return LastIndexOf(item, index, index + 1);
		}

		public int LastIndexOf(T item, int index, int count)
		{
			if ((Count != 0) && (index < 0))
				throw new ArgumentOutOfRangeException(nameof(index));

			if ((Count != 0) && (count < 0))
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_size == 0)
				return -1;

			if (index >= _size)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count > index + 1)
				throw new ArgumentOutOfRangeException(nameof(count));

			return Array.LastIndexOf(_items, item, index, count);
		}

		public bool Remove(T item)
		{
			var index = IndexOf(item);
			if (index >= 0)
			{
				RemoveAt(index);
				return true;
			}

			return false;
		}

		public int RemoveAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			var freeIndex = 0; // the first free slot in items array
			if (_items == null)
				return 0;

			// Find the first item which needs to be removed.
			while (freeIndex < _size && !match(_items[freeIndex])) freeIndex++;
			if (freeIndex >= _size) return 0;

			var current = freeIndex + 1;
			while (current < _size)
			{
				// Find the first item which needs to be kept.
				while (current < _size && match(_items[current])) current++;

				if (current < _size)
				{
					// copy item to the free slot.
					_items[freeIndex++] = _items[current++];
				}
			}

			Array.Clear(_items, freeIndex, _size - freeIndex);
			var result = _size - freeIndex;
			_size = freeIndex;
			_version++;
			return result;
		}

		public void RemoveAt(int index)
		{
			if ((uint) index >= (uint) _size)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (_items == null)
				return;

			_size--;
			if (index < _size)
				Array.Copy(_items, index + 1, _items, index, _size - index);
			_items[_size] = default(T);
			_version++;
		}

		public void RemoveRange(int index, int count)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_size - index < count)
				throw new ArgumentException("Length");

			if (count > 0)
			{
				_size -= count;
				if (index < _size)
					Array.Copy(_items, index + count, _items, index, _size - index);
				Array.Clear(_items, _size, count);
				_version++;
			}
		}

		public void Reverse() => Reverse(0, Count);

		public void Reverse(int index, int count)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));

			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_size - index < count)
				throw new ArgumentException("Length");

			if (_items == null)
				return;

			Array.Reverse(_items, index, count);
			_version++;
		}

		public void Sort() => Sort(0, Count, null);

		public void Sort(IComparer<T> comparer) => Sort(0, Count, comparer);

		public void Sort(int index, int count, IComparer<T> comparer)
		{
			if (index < 0)
				throw new ArgumentOutOfRangeException(nameof(index));
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));

			if (_size - index < count)
				throw new ArgumentException("Length");

			if (_items == null)
				return;

			Array.Sort(_items, index, count, comparer);
			_version++;
		}

		public void Sort(Comparison<T> comparison)
		{
			if (comparison == null)
				throw new ArgumentNullException(nameof(comparison));

			if (_size > 0)
			{
				IComparer<T> comparer = new FunctorComparer(comparison);
				Array.Sort(_items, 0, _size, comparer);
			}
		}

		internal sealed class FunctorComparer : IComparer<T>
		{
			private readonly Comparison<T> _comparison;

			public FunctorComparer(Comparison<T> comparison)
			{
				_comparison = comparison;
			}

			public int Compare(T x, T y) => _comparison(x, y);
		}

		public T[] ToArray()
		{
			if (_items == null)
				return EmptyArray;

			var array = new T[_size];
			Array.Copy(_items, 0, array, 0, _size);
			return array;
		}

		public void TrimExcess()
		{
			if (_items == null)
				return;

			var threshold = (int) (_items.Length*0.9);
			if (_size < threshold)
				Capacity = _size;
		}

		public bool TrueForAll(Predicate<T> match)
		{
			if (match == null)
				throw new ArgumentNullException(nameof(match));

			if (_items == null)
				return true;

			for (var i = 0; i < _size; i++)
			{
				if (!match(_items[i]))
					return false;
			}
			return true;
		}


		public struct Enumerator : IEnumerator<T>
		{
			private ValueTypeList<T> _list;
			private int _index;
			private readonly int _version;

			internal Enumerator(ValueTypeList<T> list)
			{
				_list = list;
				_index = 0;
				_version = list._version;
				Current = default(T);
			}

			public void Dispose()
			{
			}

			public bool MoveNext()
			{
				var localList = _list;

				if ((uint)_index < (uint)localList._size)
				{
					Current = localList._items[_index];
					_index++;
					return true;
				}
				return MoveNextRare();
			}

			private bool MoveNextRare()
			{
				if (_version != _list._version)
					throw new InvalidOperationException("Version");

				_index = _list._size + 1;
				Current = default(T);
				return false;
			}

			public T Current { get; private set; }

			object IEnumerator.Current
			{
				get
				{
					if (_index == 0 || _index == _list._size + 1)
						throw new InvalidOperationException();
					return Current;
				}
			}

			void IEnumerator.Reset()
			{
				if (_version != _list._version)
					throw new InvalidOperationException("Version");

				_index = 0;
				Current = default(T);
			}
		}
	}
}