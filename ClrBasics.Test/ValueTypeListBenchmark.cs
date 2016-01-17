using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ClrBasics.Test
{
	[TestFixture]
	public class ValueTypeListBenchmark : UnitTestBase
	{
		[Test]
		public void Benchmark()
		{
			const int count = 5 * 1000 * 1000;
			{
				var list = new List<int> { 42, 43, 44 };
				BM("List.Contains", () => {
					for (var i = 0; i < count; i++)
						list.Contains(43);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList.Contains", () => {
					for (var i = 0; i < count; i++)
						list.Contains(43);
					return count;
				});
			}
			{
				var list = new List<int> { 42, 43, 44 };
				BM("List iteration", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						foreach (var item in list)
							xx += item;
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList iteration", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						foreach (var item in list)
							xx += item;
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new List<int> { 42, 43, 44 };
				BM("List iteration, index", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list[j];
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList iteration, index", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list[j];
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList iteration, Array, index", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list.TheArray[j];
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new List<int> { 42, 43, 44 };
				BM("List Single", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						xx += list.Single(v => v == 42);
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList Single", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						xx += list.AsList().Single(v => v == 42);
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList Single, reuse", () => {
					var xx = 0;
					var en = list.NullReferenctypeList;
					for (var i = 0; i < count; i++)
						xx += list.AsList(ref en).Single(v => v == 42);
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new List<int> { 42, 43, 44 };
				BM("List IndexOf", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						xx += list.IndexOf(42);
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>(new[] { 42, 43, 44 });
				BM("ValueTypeList IndexOf", () => {
					var xx = 0;
					for (var i = 0; i < count; i++)
						xx += list.IndexOf(42);
					AssertSideeffect(xx);
					return count;
				});
			}
		}
	}
}