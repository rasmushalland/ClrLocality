using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ClrLocality.Test
{
	[TestFixture]
	public class ComparerBuilderTest
	{
		[Test]
		public void Test()
		{
			IComparer<MyClass> comparer = ComparerBuilder.Begin<MyClass>().
				OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).
				ThenBy(r => r.Id).
				End();
			Assert.That(
				new[] { new MyClass(1, "henrik"), new MyClass(1, "henrik2") }.OrderBy(r => r, comparer).Select(r => r.Name).ToArray(),
				Is.EquivalentTo(new[] { "henrik", "henrik2" }));
			Assert.That(
				new[] { new MyClass(1, "henrik2"), new MyClass(1, "henrik") }.OrderBy(r => r, comparer).Select(r => r.Name).ToArray(),
				Is.EquivalentTo(new[] { "henrik", "henrik2" }));
			Assert.That(
				new[] { new MyClass(1, "henrik"), new MyClass(1, "HENRIK") }.OrderBy(r => r, comparer).Select(r => r.Name).ToArray(),
				Is.EquivalentTo(new[] { "henrik", "HENRIK" }));
			Assert.That(
				new[] { new MyClass(1, "HENRIK"), new MyClass(1, "henrik") }.OrderBy(r => r, comparer).Select(r => r.Name).ToArray(),
				Is.EquivalentTo(new[] { "HENRIK", "henrik" }));
			Assert.That(
				new[] { new MyClass(2, "HENRIK"), new MyClass(1, "henrik") }.OrderBy(r => r, comparer).Select(r => r.Name).ToArray(),
				Is.EquivalentTo(new[] { "henrik", "HENRIK" }));
		}

		private class MyClass
		{
			public int Id { get; }
			public string Name { get; }

			public MyClass(int id, string name)
			{
				Id = id;
				Name = name;
			}
		}
	}
}