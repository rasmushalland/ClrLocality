using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class IntrinsicOpsPerfTest
	{
		const long DefaultIterationCount = 1000 * 1000;

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

			MachineCodeHandler.EnsurePrepared(typeof(IntrinsicOps));

			BM("popcnt32-software", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCountSoftware(12);
				AssertSideeffect(sideeffect, cnt * 2);
				return cnt;
			});
			BM("popcnt32-native", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCount(12);
				AssertSideeffect(sideeffect, cnt * 2);
				return cnt;
			});
			BM("popcnt64-software", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
				AssertSideeffect(sideeffect, cnt * 2);
				return cnt;
			});
			BM("popcnt64-native", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCount(12L);
				AssertSideeffect(sideeffect, cnt * 2);
				return cnt;
			});
			BM("empty loop", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (var i = 0; i < cnt; i++)
					sideeffect += 12 << (int)i;
				//AssertSideeffect(sideeffect, cnt);
				return cnt;
			});
			BM("popcnt64-software 4x unrolled", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
				{
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
				}
				AssertSideeffect(sideeffect, cnt * 2 * 4);
				return cnt;
			});
			BM("popcnt64-native 4x unrolled", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
				{
					sideeffect += IntrinsicOps.PopulationCount(12L);
					sideeffect += IntrinsicOps.PopulationCount(12L);
					sideeffect += IntrinsicOps.PopulationCount(12L);
					sideeffect += IntrinsicOps.PopulationCount(12L);
				}
				AssertSideeffect(sideeffect, cnt * 2 * 4);
				return cnt;
			});


			BM("swapbytes32-software", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0U;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.SwapBytesSoftware(12);
				IntrinsicOps.Nop(sideeffect);
				return cnt;
			});
			BM("swapbytes32-native", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0U;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.SwapBytes(12);
				IntrinsicOps.Nop(sideeffect);
				return cnt;
			});
			BM("swapbytes64-software", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0UL;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.SwapBytesSoftware(12L);
				IntrinsicOps.Nop(sideeffect);
				return cnt;
			});
			BM("swapbytes64-native", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0UL;
				for (var i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.SwapBytes(12L);
				IntrinsicOps.Nop(sideeffect);
				return cnt;
			});
			BM("swapbytes64-software, 4x unrolled", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0UL;
				for (var i = 0; i < cnt; i++)
				{
					sideeffect += IntrinsicOps.SwapBytesSoftware(12L);
					sideeffect += IntrinsicOps.SwapBytesSoftware(12L);
					sideeffect += IntrinsicOps.SwapBytesSoftware(12L);
					sideeffect += IntrinsicOps.SwapBytesSoftware(12L);
				}
				IntrinsicOps.Nop(sideeffect);
				return cnt;
			});
			BM("swapbytes64-native, 4x unrolled", () => {
				var cnt = DefaultIterationCount;
				var sideeffect = 0UL;
				for (var i = 0; i < cnt; i++)
				{
					sideeffect += IntrinsicOps.SwapBytes(12L);
					sideeffect += IntrinsicOps.SwapBytes(12L);
					sideeffect += IntrinsicOps.SwapBytes(12L);
					sideeffect += IntrinsicOps.SwapBytes(12L);
				}
				IntrinsicOps.Nop(sideeffect);
				return cnt;
			});
		}

		private static void AssertSideeffect(long sideeffect, long cnt)
		{
			//Console.WriteLine(sideeffect);
			Assert.AreEqual(cnt, sideeffect);
		}
	}
}
