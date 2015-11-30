using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using NUnit.Framework;

namespace ClrBasics.Test
{
	[TestFixture]
	public class CompactLookupBenchmark
	{
		private static bool outputMarkdownTable = false;

		static void BM(string name, Func<long> doit)
		{
			doit();
			long iterations = 0;
			long elapsed = 0;

			bool avoidGC = false;
			if (avoidGC)
			{
				for (var i = 0; i < 10; i++)
				{
					var gcsBefore = GC.CollectionCount(0);
					var sw = ThreadCycleStopWatch.StartNew();
					iterations = doit();
					elapsed = sw.GetCurrentCycles();

					var gcsAfter = GC.CollectionCount(0);
					if (gcsAfter == gcsBefore)
						break;
					if (i > 8)
						throw new ApplicationException("Can't get measurement without GC.");
				}
			}
			else
			{
				var sw = ThreadCycleStopWatch.StartNew();
				iterations = doit();
				elapsed = sw.GetCurrentCycles();
			}
			if (outputMarkdownTable)
				Console.WriteLine("|" + name + " | " + (elapsed / iterations) + " |");
			else
				Console.WriteLine($"{name}: {elapsed / iterations} cycles/iter.");
		}

		[Test]
		[Explicit("Takes a bit of time.")]
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

					{
						// small values.
						var arrayLookup = keys.ToCompactLookupFromContiguous(key => key, key => default(int));
						var iLookup = keys.ToLookup(key => key, key => default(int));

						// Make sure it's jit'ed.
						arrayLookup[123].Count();
						iLookup[123].Count();
						GC.Collect();
						GC.WaitForPendingFinalizers();

						BM("arraylookup, small value, " + setup.datadesc, () => {
							var theLookup = arrayLookup;
							var theKeys = keysToLookup;
							long sideeffect = 0;
							foreach (var key in theKeys)
								foreach (var value in theLookup[key])
									sideeffect++;
							AssertSideeffect(sideeffect);
							return theKeys.Count;
						});
						BM("ilookup, small value, " + setup.datadesc, () => {
							var theLookup = iLookup;
							var theKeys = keysToLookup;
							long sideeffect = 0;
							foreach (var key in theKeys)
								foreach (var value in theLookup[key])
									sideeffect++;
							AssertSideeffect(sideeffect);
							return theKeys.Count;
						});

						{
							// random keys.
							var randomKeysToLookup = new List<long>();
							for (int i = 0, index = 1; i < 1000 * 1000; i++)
							{
								randomKeysToLookup.Add(index);
								index = (int)((index * 786431U) % keys.Count);
							}
							BM("arraylookup, random keys, " + setup.datadesc, () => {
								var theLookup = arrayLookup;
								var theKeys = randomKeysToLookup;
								long sideeffect = 0;
								foreach (var key in theKeys)
									foreach (var value in theLookup[key])
										sideeffect++;
								AssertSideeffect(sideeffect);
								return theKeys.Count;
							});
							BM("ilookup, random keys, " + setup.datadesc, () => {
								var theLookup = iLookup;
								var theKeys = randomKeysToLookup;
								long sideeffect = 0;
								foreach (var key in theKeys)
									foreach (var value in theLookup[key])
										sideeffect++;
								AssertSideeffect(sideeffect);
								return theKeys.Count;
							});
						}
					}
					{
						// Medium values.
						var arrayLookup = keys.ToCompactLookupFromContiguous(key => key, key => default(Guid));
						var iLookup = keys.ToLookup(key => key, key => default(Guid));

						// Make sure it's jit'ed.
						arrayLookup[123].Count();
						iLookup[123].Count();
						GC.Collect();
						GC.WaitForPendingFinalizers();

						BM("arraylookup, medium value, " + setup.datadesc, () => {
							var theLookup = arrayLookup;
							var theKeys = keysToLookup;
							long sideeffect = 0;
							foreach (var key in theKeys)
								foreach (var value in theLookup[key])
									sideeffect++;
							AssertSideeffect(sideeffect);
							return theKeys.Count;
						});
						BM("ilookup, medium value, " + setup.datadesc, () => {
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
					{
						// Medium values.
						var arrayLookup = keys.ToCompactLookupFromContiguous(key => key, key => default(KeyValuePair<Guid, Guid>));
						var iLookup = keys.ToLookup(key => key, key => default(KeyValuePair<Guid, Guid>));

						// Make sure it's jit'ed.
						arrayLookup[123].Count();
						iLookup[123].Count();
						GC.Collect();
						GC.WaitForPendingFinalizers();

						//if (!setup.manyValuesPerKey && !setup.fitsInCache)
						//{
						//	Console.WriteLine("waiting for inspection");
						//	Thread.Sleep(1000000);
						//}

						BM("arraylookup, large value, " + setup.datadesc, () => {
							var theLookup = arrayLookup;
							var theKeys = keysToLookup;
							long sideeffect = 0;
							foreach (var key in theKeys)
								foreach (var value in theLookup[key])
									sideeffect++;
							AssertSideeffect(sideeffect);
							return theKeys.Count;
						});
						BM("ilookup, large value, " + setup.datadesc, () => {
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
		}

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

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void AssertSideeffect(long sideeffect)
		{
			// nothing.
		}
	}
}