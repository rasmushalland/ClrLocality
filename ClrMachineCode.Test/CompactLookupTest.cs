using System;
using System.Linq;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class CompactLookupTest
	{
		[Test]
		public void ToCompactLookupFromContiguous()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key1", value = "value1_2"},
				new {key = "key2", value = "value2_1"},
			};

			var lookup = items.ToCompactLookupFromContiguous(item => item.key, item => item.value);
			Assert.IsEmpty(lookup["nokey"]);
			Assert.That(lookup["key1"], Is.EquivalentTo(new[] {"value1_1", "value1_2"}));
			Assert.That(lookup["key2"], Is.EquivalentTo(new[] {"value2_1"}));
			Assert.That(lookup["key1"], Is.EquivalentTo(new[] { "value1_1", "value1_2" }));
			Assert.IsEmpty(lookup["nokey"]);
		}

		[Test]
		public void ToCompactLookup_FromGroupings()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key1", value = "value1_2"},
				new {key = "key2", value = "value2_1"},
			};

			var lookup = items.GroupBy(item => item.key, item => item.value).ToCompactLookup();
			Assert.IsEmpty(lookup["nokey"]);
			Assert.That(lookup["key1"], Is.EquivalentTo(new[] {"value1_1", "value1_2"}));
			Assert.That(lookup["key2"], Is.EquivalentTo(new[] {"value2_1"}));
			Assert.That(lookup["key1"], Is.EquivalentTo(new[] { "value1_1", "value1_2" }));
			Assert.IsEmpty(lookup["nokey"]);
		}

		[Test]
		public void Enumeration()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key1", value = "value1_2"},
				new {key = "key2", value = "value2_1"},
			};

			var lookup = items.ToCompactLookupFromContiguous(item => item.key, item => item.value);
			var lines = lookup
				.Select(g => g.Key + ": " + string.Join(", ", g))
				.OrderBy(str => str)
				.ToArray();

			Assert.That(lines, Is.EquivalentTo(new[] { "key1: value1_1, value1_2", "key2: value2_1" }));
		}

		[Test]
		public void BasicTest_OutOfOrder()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key2", value = "value2_1"},
				new {key = "key1", value = "value1_2"},
			};
            Assert.Throws<ArgumentException>(() => items.ToCompactLookupFromContiguous(item => item.key, item => item.value));
		}

		[Test]
		public void Empty()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key1", value = "value1_2"},
				new {key = "key2", value = "value2_1"},
			};

			var lookup = items.Take(0).ToCompactLookupFromContiguous(item => item.key, item => item.value);
			Assert.IsEmpty(lookup["nokey"]);
		}
	}
}