using System;
using System.Linq;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class ArrayLookupTest
	{
		[Test]
		public void BasicTest1()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key1", value = "value1_2"},
				new {key = "key2", value = "value2_1"},
			};

			var lookup = ArrayLookup.FromContiguous(items, item => item.key, item => item.value);
			Assert.IsEmpty(lookup["nokey"]);
			Assert.That(lookup["key1"], Is.EquivalentTo(new[] {"value1_1", "value1_2"}));
			Assert.That(lookup["key2"], Is.EquivalentTo(new[] {"value2_1"}));
			Assert.That(lookup["key1"], Is.EquivalentTo(new[] { "value1_1", "value1_2" }));
			Assert.IsEmpty(lookup["nokey"]);
		}

		[Test]
		public void BasicTest_OutOfOrder()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key2", value = "value2_1"},
				new {key = "key1", value = "value1_2"},
			};
            Assert.Throws<ArgumentException>(() => ArrayLookup.FromContiguous(items, item => item.key, item => item.value));
		}

		[Test]
		public void Empty()
		{
			var items = new[] {
				new {key = "key1", value = "value1_1"},
				new {key = "key1", value = "value1_2"},
				new {key = "key2", value = "value2_1"},
			};

			var lookup = ArrayLookup.FromContiguous(items.Take(0), item => item.key, item => item.value);
			Assert.IsEmpty(lookup["nokey"]);
		}
	}
}