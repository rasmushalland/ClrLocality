using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class ArrayLookupBenchmark
	{
		private static bool outputMarkdownTable = false;

		static void BM(string name, Func<long> doit)
		{
			doit();
			var sw = ThreadCycleStopWatch.StartNew();
			var iterations = doit();
			var elapsed = sw.GetCurrentCycles();
			if (outputMarkdownTable)
				Console.WriteLine("|" + name + " | " + (elapsed / iterations) + " |");
			else
				Console.WriteLine($"{name}: {elapsed / iterations} cycles/iter.");
		}

		[Test]
		public void Benchmark()
		{
			if (outputMarkdownTable)
				Console.WriteLine(@"| Test | Cycles/iteration |
| ------ |------:|");

			// Operation:
			// - build
			// - lookup
			// Size:
			// - fits in some cache
			// - does not fit in cache
			// Implementation:
			// - ToLookup
			// - ArrayLookup
			// Data distribution:?
			// - many values per key
			// - few values per key
			// Value size:
			// - small: 4 bytes
			// - larger: 16 bytes
			// - largest: 32 bytes

			{
				var setups =
					from fitsInCache in new[] {true, false}
					from manyValuesPerKey in new[] {true, false}
					let datadesc = (fitsInCache ? "cache_ok" : "no_cache") + ", " + (manyValuesPerKey ? "many_values" : "few_values")
					select new {
						fitsInCache,
						manyValuesPerKey,
						datadesc,
					};
				foreach (var setup in setups)
				{
					var keys = GenerateDistinctKeys(setup.fitsInCache, setup.manyValuesPerKey).ToList();

					// Precompute array of keys to lookup, so we're not measuring the time to get the keys.
					var keysToLookup = new List<long>();
					for (int i = 0, index = 1; i < 1000*1000; i++)
					{
						keysToLookup.Add(keys[index]);
						index = (int) ((index*786431U)%keys.Count);
					}

					var arrayLookup = keys.ToLookupFromContiguous(key => key, key => default(int));
					var iLookup = keys.ToLookup(key => key, key => default(int));

					// Make sure it's jit'ed.
					arrayLookup[123].Count();
					iLookup[123].Count();
					GC.Collect();
					GC.WaitForPendingFinalizers();

					BM("arraylookup, " + setup.datadesc, () => {
						var theLookup = arrayLookup;
						var theKeys = keysToLookup;
						long sideeffect = 0;
						foreach (var key in theKeys)
							foreach (var value in theLookup[key])
								sideeffect++;
						AssertSideeffect(sideeffect);
						return theKeys.Count;
					});
					BM("ilookup, " + setup.datadesc, () => {
						var theLookup = iLookup;
						var theKeys = keysToLookup;
						long sideeffect = 0;
						foreach (var key in theKeys)
							foreach (var value in theLookup[key])
								sideeffect++;
						AssertSideeffect(sideeffect);
						return theKeys.Count;
					});
				}
			}
		}

		//private static Func<TValue> GetFunc<TValue>(Func<TValue> func) => func;

		static IEnumerable<long> GenerateDistinctKeys(bool fitsInCache, bool manyValuesPerKey)
		{
			const int l3estimate = 16*1024*1024;
			//var sizePerItem = 

			var count = fitsInCache ? l3estimate/4/40 : l3estimate*16/40;
			var rand = new Random(count);
			var hist = new HashSet<long>();

			long cur = 0;
			if (manyValuesPerKey)
			{
				for (int i = 0; i < count; i++)
				{
					if ((i%100) == 0)
					{
						do
						{
							cur = rand.Next();
						} while (hist.Contains(cur));
						hist.Add(cur);
					}
					yield return cur;
				}
			}
			else
			{
				for (int i = 0; i < count; i++)
				{
					do
					{
						cur = rand.Next();
					} while (hist.Contains(cur));
					hist.Add(cur);
					yield return cur;
				}
			}
		} 

		class ItemSmallValue
		{
			public long Key { get; }
			public int Value { get; }

			public ItemSmallValue(long key, int value)
			{
				Key = key;
				Value = value;
			}
		}
		class ItemMediumValue
		{
			public long Key { get; }
			public Guid Value { get; }

			public ItemMediumValue(long key, Guid value)
			{
				Key = key;
				Value = value;
			}
		}
		class ItemLargeValue
		{
			public long Key { get; }
			public KeyValuePair<Guid, Guid> Value { get; }

			public ItemLargeValue(long key, KeyValuePair<Guid, Guid> value)
			{
				Key = key;
				Value = value;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void AssertSideeffect(long sideeffect)
		{
			// nothing.
		}
	}
}