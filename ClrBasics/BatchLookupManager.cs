using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace ClrBasics
{
	public class BatchLookupManager
	{
		private readonly List<BatchLookup> _batchLookups = new List<BatchLookup>();
		private readonly List<MethodInfo> _lookupFuncs = new List<MethodInfo>();

		private readonly Stack<EnqueuedResolve> _queuedResolves = new Stack<EnqueuedResolve>();

		[CanBeNull]
		private Stack<IDisposable> _activeImmediateScopes;

		internal IReadOnlyList<BatchLookup> BatchLookups => _batchLookups;

		#region EnqueuedResolve

		/// <summary>
		/// Contains a <see cref="TaskCompletionSource{TResult}"/> and the value with which to resolve it.
		/// </summary>
		internal abstract class EnqueuedResolve
		{
			public abstract void Resolve();
		}

		internal sealed class EnqueuedResolve<T> : EnqueuedResolve
		{
			private readonly TaskCompletionSource<T> _completionSource;
			private readonly T _value;

			public EnqueuedResolve(TaskCompletionSource<T> completionSource, T value)
			{
				_completionSource = completionSource;
				_value = value;
			}

			public override void Resolve() =>
				_completionSource.SetResult(_value);
		}

		#endregion

		#region BeginImmediateScope

		/// <summary>
		/// Creates a scope which, until disposed, will make all lookups occur immediately.
		/// </summary>
		public IDisposable BeginImmediateScope()
		{
			if (_activeImmediateScopes == null)
				_activeImmediateScopes = new Stack<IDisposable>();

			var scope = new ImmediateScope(this);
			_activeImmediateScopes.Push(scope);
			return scope;
		}

		private sealed class ImmediateScope : IDisposable
		{
			private readonly BatchLookupManager _lookupManager;
			private bool _isdisposed;

			public ImmediateScope(BatchLookupManager lookupManager)
			{
				_lookupManager = lookupManager;
			}

			public void Dispose()
			{
				if (_isdisposed)
					return;
				if (_lookupManager._activeImmediateScopes.Count == 0 || !ReferenceEquals(_lookupManager._activeImmediateScopes.Peek(), this))
					throw new InvalidOperationException("This scope is not the most recently created scope.");
				_lookupManager._activeImmediateScopes.Pop();
				_isdisposed = true;
			}
		}

		#endregion

		/// <summary>
		/// Used for batching of lookups returning collections of data.
		/// </summary>
		public Task<IReadOnlyList<TValue>> LookupCollection<TKey, TValue>(Func<IReadOnlyList<TKey>, IReadOnlyList<TValue>> lookupFunc, Func<TValue, TKey> keySelector, int preferredBatchSize, TKey key)
		{
			int index = _lookupFuncs.IndexOf(lookupFunc.Method);
			BatchListLookup<TKey, TValue> batchLookup;
			if (index == -1)
			{
				batchLookup = new BatchListLookup<TKey, TValue>(lookupFunc, keySelector, preferredBatchSize);
				_lookupFuncs.Add(lookupFunc.Method);
				_batchLookups.Add(batchLookup);
			}
			else
				batchLookup = (BatchListLookup<TKey, TValue>)BatchLookups[index];

			var task = LookupCollectionExImpl(key, batchLookup);
			return task;
		}

		/// <summary>
		/// Used for batching of lookups of single items.
		/// The task is faulted with an exception is thrown if the item is not found.
		/// </summary>
		/// <exception cref="KeyNotFoundException"></exception>
		/// <seealso cref="CreateNotFoundException"/>.
		public Task<TValue> Lookup<TKey, TValue>(Func<IReadOnlyList<TKey>, IReadOnlyList<TValue>> lookupFunc, Func<TValue, TKey> keySelector, int preferredBatchSize, TKey key) =>
			LookupImpl(lookupFunc, keySelector, preferredBatchSize, key, false);

		/// <summary>
		/// Used for batching of lookups of single items.
		/// The task is completed with the default value of <see cref="TValue"/> if the item is not found.
		/// </summary>
		public Task<TValue> LookupNullable<TKey, TValue>(Func<IReadOnlyList<TKey>, IReadOnlyList<TValue>> lookupFunc, Func<TValue, TKey> keySelector, int preferredBatchSize, TKey key) =>
			LookupImpl(lookupFunc, keySelector, preferredBatchSize, key, true);

		private Task<TValue> LookupImpl<TKey, TValue>(Func<IReadOnlyList<TKey>, IReadOnlyList<TValue>> lookupFunc, Func<TValue, TKey> keySelector, int preferredBatchSize, TKey key, bool throwOnNotFound)
		{
			int index = _lookupFuncs.IndexOf(lookupFunc.Method);
			BatchLookup<TKey, TValue> batchLookup;
			if (index == -1)
			{
				batchLookup = new BatchLookup<TKey, TValue>(keys => lookupFunc(keys).ToDictionary(keySelector), preferredBatchSize, default(TValue));
				_lookupFuncs.Add(lookupFunc.Method);
				_batchLookups.Add(batchLookup);
			}
			else
				batchLookup = (BatchLookup<TKey, TValue>)BatchLookups[index];

			var task = LookupExImpl(key, batchLookup, throwOnNotFound);
			return task;
		}

		protected virtual Exception CreateNotFoundException(object key, Type type)
		{
			return new KeyNotFoundException($"No value of type {type} was found for the key \"{key}\".");
		}

		private Task<TValue> LookupExImpl<TKey, TValue>(TKey key, BatchLookup<TKey, TValue> batchLookup, bool throwOnNotFound)
		{
			if (LookupImmediately)
			{
				// Her burde man måske pakke exceptions ind i task, men i hvilken udstrækning gør det en forskel ift. korrekthed, stack trace og overhead?
				IReadOnlyDictionary<TKey, TValue> dict1 = batchLookup.LookupFunc(new[] { key });

				TValue val1;
				if (!dict1.TryGetValue(key, out val1))
				{
					if (throwOnNotFound)
					{
						CreateNotFoundException(key, typeof(TValue));
						throw new KeyNotFoundException();
					}
					return Task.FromResult(batchLookup.DefaultValue);
				}
				return Task.FromResult(val1);
			}

			batchLookup.Keys.Add(key);

			var task = batchLookup.CompletionSource.Task;
			if (batchLookup.Keys.Count >= batchLookup.BatchSize)
				_queuedResolves.Push(batchLookup.RetrieveData());

			return task.ContinueWith(dictTask => {
				TValue val;
				if (!dictTask.GetAwaiter().GetResult().TryGetValue(key, out val))
				{
					if (throwOnNotFound)
						throw CreateNotFoundException(key, typeof (TValue));
					return batchLookup.DefaultValue;
				}
				return val;
			}, TaskContinuationOptions.ExecuteSynchronously);
		}

		private Task<IReadOnlyList<TValue>> LookupCollectionExImpl<TKey, TValue>(TKey key, BatchListLookup<TKey, TValue> batchLookup)
		{
			if (LookupImmediately)
			{
				// Her burde man måske pakke exceptions ind i task, men i hvilken udstrækning gør det en forskel ift. korrekthed, stack trace og overhead?
				IReadOnlyDictionary<TKey, IReadOnlyList<TValue>> dict1 = batchLookup.InnerLookup.LookupFunc(new[] { key });

				IReadOnlyList<TValue> val1;
				if (!dict1.TryGetValue(key, out val1))
					return Task.FromResult((IReadOnlyList<TValue>)EmptyArray<TValue>.Instance);
				return Task.FromResult(val1);
			}

			batchLookup.InnerLookup.Keys.Add(key);

			var task = batchLookup.InnerLookup.CompletionSource.Task;
			if (batchLookup.InnerLookup.Keys.Count >= batchLookup.BatchSize)
				_queuedResolves.Push(batchLookup.RetrieveData());

			return task.ContinueWith(dictTask => {
				IReadOnlyList<TValue> val;
				if (!dictTask.GetAwaiter().GetResult().TryGetValue(key, out val))
					return (IReadOnlyList<TValue>)EmptyArray<TValue>.Instance;
				return val;
			}, TaskContinuationOptions.ExecuteSynchronously);
		}

		private bool LookupImmediately =>
			_activeImmediateScopes != null && _activeImmediateScopes.Count != 0;

		/// <summary>
		///     Denne funktion må ikke returnere før enten 1: alle tasks er enten completed og ikke faulted, eller 2: blot én er
		///     faulted.
		///     Når en task faulter, bliver de andre ikke nødvendigvis også faulted eller cancelled (!). Dvs. de må ikke holde fat
		///     i noget der skal frigives.
		///     Rækkefølgen af elementer bevares.
		/// </summary>
		/// <remarks>
		/// TODO:
		/// - resolve fra rod ville måske være at foretrække aht. stakdybde, samt for at få bedre stakke ifbm. profiling og exceptions.
		/// - resolve bør aht. exception-stakke når det er muligt være fra opslagsstedet. Kan man undgå at roden behøver at lave opslag, fx. ved at den informerer batch-objekterne om 
		///   at der ikke er flere iterationer/poster? tror det ikke, hver post kan lave vilkårligt mange opslag.
		/// 
		/// </remarks>
		public static IEnumerable<T> BatchLookupResolve<T>(IEnumerable<Task<T>> enumerable, BatchLookupManager lookupManager)
		{
			// Vil gerne begrænse antal elementer, vi kan vente på.
			// Dels for at øge sandsynligheden for de stadig er i noget cpu-cache,
			// men mest for ikke at holde et potentielt stort set objekter ilive.
			var initialBufferSize = 2000;
			int? bufferSize = null;

			var buf = new Queue<Task<T>>();

			using (var ie = enumerable.GetEnumerator())
			{
				while (true)
				{
					for (int i = 0; i < (bufferSize ?? initialBufferSize) && lookupManager._queuedResolves.Count == 0; i++)
					{
						if (buf.Count >= bufferSize)
							break;
						if (!ie.MoveNext())
							break;
						buf.Enqueue(ie.Current);
					}

					while (lookupManager._queuedResolves.Count > 0)
					{
						var qr = lookupManager._queuedResolves.Pop();
						qr.Resolve();
					}

					if (lookupManager.BatchLookups.Count != 0)
					{
						bufferSize = lookupManager.BatchLookups.Max(bl2 => bl2.BatchSize);
						if (bufferSize == 0)
							bufferSize = null;
					}

					var notCompletedCount = buf.Count;
					if (notCompletedCount == 0)
						yield break;

					// Resolve noget. Hvis der ikke er en der er fyldt op, tager den med flest ventende. Kunne også godt resolve alle, men
					// måske resten bliver mere mere effektive (får flere i kø), når vi resolver 
					// den med flest.
					var bl = lookupManager.BatchLookups.Count != 0 ? lookupManager.BatchLookups.OrderByDescending(br => br.PendingLookups).First() : null;
					if (bl != null && bl.PendingLookups != 0)
					{
						lookupManager._queuedResolves.Push(bl.RetrieveData());
						continue;
					}
					else
					{
						// Der er ikke noget vi kan gøre - de må vente på noget andet end os. 
						// Så vi må vente på dem.

						var task = buf.Peek();
						task.GetAwaiter().GetResult();
					}

					while (buf.Count != 0)
					{
						var task = buf.Peek();
						if (!task.IsCompleted)
						{
							// må resolve noget, og så tage en runde til.
							break;
						}

						buf.Dequeue();
						if (task.IsFaulted)
						{
							task.GetAwaiter().GetResult();
							throw new Exception("Der skal da komme en exception.");
						}
						yield return task.Result;
					}
				}
			}
		}
	}

	public static class BatchLookupExtensions
	{
		public static IEnumerable<T> BatchLookupResolve<T>(this IEnumerable<Task<T>> enumerable, BatchLookupManager lookupManager) =>
			BatchLookupManager.BatchLookupResolve(enumerable, lookupManager);
	}

	abstract class BatchLookup
	{
		public abstract int PendingLookups { get; }
		public abstract int BatchSize { get; }

		public abstract BatchLookupManager.EnqueuedResolve RetrieveData();
	}

	sealed class BatchLookup<TKey, TValue> : BatchLookup
	{
		public override int BatchSize { get; }

		public readonly Func<IReadOnlyList<TKey>, IReadOnlyDictionary<TKey, TValue>> LookupFunc;

		public TaskCompletionSource<IReadOnlyDictionary<TKey, TValue>> CompletionSource;
		public List<TKey> Keys = new List<TKey>();
		public readonly TValue DefaultValue;


		public BatchLookup(Func<IReadOnlyList<TKey>, IReadOnlyDictionary<TKey, TValue>> lookupFunc, int batchSize, TValue defaultValue)
		{
			BatchSize = batchSize;
			DefaultValue = defaultValue;
			LookupFunc = lookupFunc;
			CompletionSource = new TaskCompletionSource<IReadOnlyDictionary<TKey, TValue>>();
		}

		public override int PendingLookups => Keys.Count;

		public override BatchLookupManager.EnqueuedResolve RetrieveData()
		{
			IReadOnlyDictionary<TKey, TValue> dict = LookupFunc(Keys);
			TaskCompletionSource<IReadOnlyDictionary<TKey, TValue>> cs = CompletionSource;

			CompletionSource = new TaskCompletionSource<IReadOnlyDictionary<TKey, TValue>>();
			Keys = new List<TKey>();

			// Indikér at vi er klar.
			var enqueuedResolve = new BatchLookupManager.EnqueuedResolve<IReadOnlyDictionary<TKey, TValue>>(cs, dict);
			return enqueuedResolve;
		}
	}

	sealed class BatchListLookup<TKey, TValue> : BatchLookup
	{
		public readonly BatchLookup<TKey, IReadOnlyList<TValue>> InnerLookup;

		public BatchListLookup(Func<IReadOnlyList<TKey>, IReadOnlyList<TValue>> lookupFunc, Func<TValue, TKey> keySelector, int batchSize)
		{
			InnerLookup = new BatchLookup<TKey, IReadOnlyList<TValue>>(
				keys => lookupFunc(keys).GroupBy(keySelector).ToDictionary(g => g.Key, g => (IReadOnlyList<TValue>)g.ToList()),
				batchSize, EmptyArray<TValue>.Instance);
		}

		public override int BatchSize => InnerLookup.BatchSize;

		public override int PendingLookups => InnerLookup.PendingLookups;

		public override BatchLookupManager.EnqueuedResolve RetrieveData() =>
			InnerLookup.RetrieveData();
	}
}
