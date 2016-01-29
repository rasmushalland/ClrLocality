using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ClrBasics.Test
{
	[TestFixture]
	public class BatchLookupTest : UnitTestBase
	{
		[Test]
		public void Simple()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var batches = new List<IReadOnlyCollection<int>>();

			var lookupManager = new BatchLookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = key =>
				lookupManager.LookupNullable(key, itemsBatch => {
					                                                batches.Add(itemsBatch);
					                                                return itemsBatch.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList();
				}, v => v.Key, 100);

			List<string> results = items.
				Select(async item => {
					var kvp = await GetSingle_Batched(item);
					return kvp.Value;
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi for " + item).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
			AreEqualSequences(new[] { "0,1,2,3,4,5,6,7,8,9" }, batches.Select(b => b.Select(i => i.ToString()).StringJoin(",")));
		}

		[Test]
		public void Collection()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var batches = new List<IReadOnlyCollection<int>>();

			var lookupManager = new BatchLookupManager();
			Func<int, Task<IReadOnlyList<KeyValuePair<int, string>>>> GetCollection_Batched = key =>
				lookupManager.LookupCollection(key, itemsBatch => {
					                                                  batches.Add(itemsBatch);
					                                                  return itemsBatch.SelectMany(item => new[] {
						                                                  new KeyValuePair<int, string>(item, "Værdi 1 for " + item),
						                                                  new KeyValuePair<int, string>(item, "Værdi 2 for " + item),
					                                                  }).ToList();
				}, v => v.Key, 100);

			List<string> results = items.
				Select(async item => {
					var kvps = await GetCollection_Batched(item);
					return kvps.Select(kvp => kvp.Value).StringJoin("_");
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi 1 for " + item + "_Værdi 2 for " + item).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
			AreEqualSequences(new[] { "0,1,2,3,4,5,6,7,8,9" }, batches.Select(b => b.Select(i => i.ToString()).StringJoin(",")));
		}

		[Test]
		public void Multiple()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var batches = new List<IReadOnlyCollection<int>>();

			var lookupManager = new BatchLookupManager();
			Func<int, Task<IReadOnlyList<KeyValuePair<int, string>>>> GetCollection_Batched = key =>
				lookupManager.LookupCollection(key, itemsBatch => {
					                                                  batches.Add(itemsBatch);
					                                                  return itemsBatch.
						                                                  Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).
						                                                  ToList();
				}, v => v.Key, 100);

			List<string> results = items.
				Select(async item => {
					var kvps = await GetCollection_Batched(item);
					return kvps.Select(kvp => kvp.Value).StringJoin("_");
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi 1 for " + item + "_Værdi 2 for " + item).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
			AreEqualSequences(new[] { "0,1,2,3,4,5,6,7,8,9" }, batches.Select(b => b.Select(i => i.ToString()).StringJoin(",")));
		}

		[Test]
		public void Immediate()
		{
			var lookupManager = new BatchLookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = (key) =>
				lookupManager.LookupNullable(key, itemsBatch => itemsBatch.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList(), v => v.Key, 100);

			{
				Task<KeyValuePair<int, string>> res = GetSingle_Batched(123);
				IsFalse(res.IsCompleted);
			}

			using (lookupManager.BeginImmediateScope())
			{
				Task<KeyValuePair<int, string>> res = GetSingle_Batched(123);
				IsTrue(res.IsCompleted);
			}

			// Skal blot se at der ikke bliver registreret flere.
			AreEqual(1, lookupManager.BatchLookups.Count);
		}

		[Test]
		public void SimpleWithBatchSize()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var batches = new List<IReadOnlyCollection<int>>();

			var lookupManager = new BatchLookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = key =>
				lookupManager.LookupNullable(key, itemsBatch => {
					                                                batches.Add(itemsBatch);
					                                                return itemsBatch.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList();
				}, v => v.Key, 4);

			List<string> results = items.
				Select(async item => {
					var kvp = await GetSingle_Batched(item);
					return kvp.Value;
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi for " + item).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
			AreEqualSequences(new[] { "0,1,2,3", "4,5,6,7", "8,9" }, batches.Select(b => b.Select(i => i.ToString()).StringJoin(",")));
		}

		[Test]
		public void DoubleLookup()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var batches = new List<IReadOnlyCollection<int>>();

			var lookupManager = new BatchLookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = key =>
				lookupManager.LookupNullable(key, itemsBatch => {
					                                                batches.Add(itemsBatch);
					                                                return itemsBatch.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList();
				}, v => v.Key, 100);

			List<string> results = items.
				Select(async item => {
					var kvp1 = await GetSingle_Batched(item);
					var kvp2 = await GetSingle_Batched(item + 100);
					return kvp1.Value + kvp2.Value;
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi for " + item + "Værdi for " + (item + 100)).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			Console.WriteLine("expected: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
		}

		[Test]
		public void NoLookup()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);

			var lookupManager = new BatchLookupManager();

			List<string> results = items.
				Select(async item => {
					string val = "Værdi for " + item;
					return val;
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi for " + item).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
		}

		[Test]
		public void Error()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);

			var lookupManager = new BatchLookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = key =>
				lookupManager.LookupNullable(key, itemsBatch => {
					                                                return itemsBatch.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList();
				}, v => v.Key, 100);

			var results = items.
				Select(async item => {
					var kvp = await GetSingle_Batched(item);
					if (item == 5)
						throw new ApplicationException("Lad os sige at den fejler.");
					return kvp.Value;
				}).
				BatchLookupResolve(lookupManager);

			var ex = Throws<ApplicationException>(() => results.ToList());
			Assert.That(ex.Message, Is.StringContaining("Lad os sige at den fejler."));
		}

		sealed class Error_KeyNotFound_LookupManager : BatchLookupManager
		{
			protected override Exception CreateNotFoundException(object key, Type type) =>
				new ApplicationException("oh noes");
		}

		[Test]
		public void Error_KeyNotFound()
		{
			IEnumerable<int> items = Enumerable.Range(0, 2);

			var lookupManager = new Error_KeyNotFound_LookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = key => lookupManager.Lookup(key, itemsBatch => new[] { 123 }.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList(), v => v.Key, 100);

			var results = items.
				Select(async item => {
					var kvp = await GetSingle_Batched(item);
					return kvp.Value;
				}).
				BatchLookupResolve(lookupManager);

			var ex = Throws<ApplicationException>(() => results.ToList());
			Assert.That(ex.Message, Is.StringContaining("oh noes"));
		}

		[Test]
		public void Error_InLookup()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var batches = new List<IReadOnlyCollection<int>>();

			var lookupManager = new BatchLookupManager();
			Func<int, Task<KeyValuePair<int, string>>> GetSingle_Batched = key =>
				lookupManager.LookupNullable(key, itemsBatch => {
					                                                batches.Add(itemsBatch);
					                                                throw new ApplicationException("Fejl i opslag");
					                                                return itemsBatch.Select(item => new KeyValuePair<int, string>(item, "Værdi for " + item)).ToList();
				}, v => v.Key, 100);

			var results = items.
				Select(async item => {
					var kvp = await GetSingle_Batched(item);
					return kvp.Value;
				}).
				BatchLookupResolve(lookupManager);

			var ex = Throws<ApplicationException>(() => results.ToList());
			Assert.That(ex.Message, Is.StringContaining("Fejl i opslag"));
		}

		[Test]
		public void AwaitOther()
		{
			IEnumerable<int> items = Enumerable.Range(0, 10);
			var lookupManager = new BatchLookupManager();

			List<string> results = items.
				Select(async item => {
					// configureawait false er vigtig, for vi kan (pt?) ikke vente på en task, samtidig med at sync ctx pumper beskeder.
					await Task.Delay(10).ConfigureAwait(false);
					string val = "Værdi for " + item;
					return val;
				}).
				BatchLookupResolve(lookupManager).
				ToList();

			List<string> expected = items.Select(item => "Værdi for " + item).ToList();
			Console.WriteLine("Items: " + string.Join("; ", results));
			AreEqualSequences(expected, results);
		}
	}
}
