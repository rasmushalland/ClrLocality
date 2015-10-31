using System;
using System.Diagnostics;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class IntrinsicOpsPerfTest
	{
		[Test]
		public void Benchmark()
		{
			MachineCodeHandler.EnsurePrepared(typeof(IntrinsicOps));

			const long defaultCnt = 1000 * 1000;

			{
				var cnt = defaultCnt * 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCountSoftware(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt32-software: {elapsed / cnt} cycles/iter.");
			}
			{
				var cnt = defaultCnt * 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCount(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt32-native: {elapsed / cnt} cycles/iter.");
			}
			{
				var cnt = defaultCnt * 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64-software: {elapsed / cnt} cycles/iter.");
			}
			{
				var cnt = defaultCnt << 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += IntrinsicOps.PopulationCount(12L);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64-native: {elapsed / cnt} cycles/iter.");
			}
			{
				var cnt = defaultCnt << 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += 12 << (int)i;
				var elapsed = sw.GetCurrentCycles();
				//AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, empty loop: {elapsed / cnt} cycles/iter.");
			}
			{
				var cnt = defaultCnt << 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
				{
					sideeffect += IntrinsicOps.PopulationCount(12L);
					sideeffect += IntrinsicOps.PopulationCount(12L);
					{ }
					{ }
					{ }
					sideeffect += IntrinsicOps.PopulationCount(12L);
					sideeffect += IntrinsicOps.PopulationCount(12L);
				}
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt * 4);
				Console.WriteLine($"Elapsed, popcnt64-native 4x: {elapsed / cnt} cycles/iter.");
			}
			{
				var sideeffect = 0L;
				var cnt = defaultCnt << 1;

				var sw = ThreadCycleStopWatch.StartNew();
				for (long i = 0; i < cnt; i++)
				{
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
					sideeffect += IntrinsicOps.PopulationCountSoftware(12L);
				}
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt * 4);
				Console.WriteLine($"Elapsed, popcnt64-software, 4x: {elapsed / cnt} cycles/iter.");
			}
		}

		private static void AssertSideeffect(long sideeffect, long cnt)
		{
			//Console.WriteLine(sideeffect);
			Assert.AreEqual(cnt * 2, sideeffect);
		}
	}
}
