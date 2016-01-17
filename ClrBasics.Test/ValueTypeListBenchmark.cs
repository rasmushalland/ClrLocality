using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

			var count = 1000 * 1000;
			{
				var list = new List<int> { 42, 43, 44 };
				BM("List.Contains", () => {
					for (int i = 0; i < count; i++)
						list.Contains(43);
					return count;
				});
			}
			{
				var list = new ValueTypeList<int>() { 42, 43, 44 };
				BM("ValueTypeList.Contains", () => {
					for (int i = 0; i < count; i++)
						list.Contains(43);
					return count;
				});
			}
		}

	}
}
