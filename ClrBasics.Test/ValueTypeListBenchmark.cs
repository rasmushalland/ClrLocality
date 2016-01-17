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
			// It shouldn't be necessary to benchmark operations that much:
			// The implemtation is pretty much identical to that of List<T>,
			// so the performance is expected to be about the same, at least at long as we don't run into 
			// cache misses.

			const int count = 5*1000*1000;
			{
				var list = new List<int> {42, 43, 44};
				BM("List.Contains", () =>
				{
					for (var i = 0; i < count; i++)
						list.Contains(43);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int> {42, 43, 44};
				BM("ValueTypeList.Contains", () =>
				{
					for (var i = 0; i < count; i++)
						list.Contains(43);
					return count;
				});
			}
			{
				var list = new List<int> {42, 43, 44};
				BM("List iteration", () =>
				{
					var xx = 0;
					for (var i = 0; i < count; i++)
						foreach (var item in list)
							xx += item;
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int> {42, 43, 44};
				BM("ValueTypeList iteration", () =>
				{
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
				var list = new ValueTypeList<int> {42, 43, 44};
				BM("ValueTypeList iteration, index", () =>
				{
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list[j];
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int> {42, 43, 44};
				BM("ValueTypeList iteration, Array, index", () =>
				{
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list.TheArray[j];
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new List<int> {42, 43, 44};
				BM("List Single", () =>
				{
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list.Single(v => v == 42);
					AssertSideeffect(xx);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int> {42, 43, 44};
				BM("ValueTypeList Single", () =>
				{
					var xx = 0;
					for (var i = 0; i < count; i++)
						for (var j = 0; j < list.Count; j++)
							xx += list.Single(v => v == 42);
					AssertSideeffect(xx);
					return count;
				});
			}
		}
	}
}