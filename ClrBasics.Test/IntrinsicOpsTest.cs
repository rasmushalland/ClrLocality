using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;

namespace ClrBasics.Test
{
	[TestFixture]
	public class IntrinsicOpsTest : UnitTestBase
	{
		private static readonly object Dummy = MachineCodeClassMarker.EnsurePrepared(typeof(IntrinsicOps));

		private static readonly bool TestX64Code = Environment.Is64BitProcess;

		[TestFixtureSetUp]
		public void SetUp()
		{
			//return;
			MachineCodeHandler.TraceSource.Listeners.Add(new ConsoleTraceListener());
			MachineCodeHandler.TraceSource.Switch.Level = SourceLevels.All;

			MachineCodeClassMarker.EnsurePrepared(typeof(IntrinsicOps));
		}

		[Test]
		public void ShowReplaced()
		{
			var values = typeof(IntrinsicOps).GetFields(BindingFlags.Static | BindingFlags.Public).
				Select(fi => new { name = fi.Name, isreplaced = fi.GetValue(null) }).
				ToList();
			Console.WriteLine(values.Select(v => v.name + ":  " + v.isreplaced).StringJoin("\r\n"));
		}

		[Test]
		public void PopulationCount64()
		{
			AreEqualHex(4, IntrinsicOps.PopulationCount((3L << 55) | (3 << 5)));

			AreEqualHex(2, IntrinsicOps.PopulationCountSoftware(3L));
			AreEqualHex(2, IntrinsicOps.PopulationCountSoftware(3L << 5));
			AreEqualHex(2, IntrinsicOps.PopulationCountSoftware(3L << 55));
			AreEqualHex(4, IntrinsicOps.PopulationCountSoftware((3L << 55) | (3 << 5)));

			if (TestX64Code)
			{
				AreEqualHex(2, IntrinsicOps.PopulationCountReplaced(3L));
				AreEqualHex(2, IntrinsicOps.PopulationCountReplaced(3L << 5));
				AreEqualHex(2, IntrinsicOps.PopulationCountReplaced(3L << 55));
				AreEqualHex(4, IntrinsicOps.PopulationCountReplaced((3L << 55) | (3 << 5)));
			}
		}

		[Test]
		public void PopulationCount32()
		{
			AreEqualHex(2, IntrinsicOps.PopulationCountSoftware(3));
			AreEqualHex(2, IntrinsicOps.PopulationCountSoftware(3 << 5));
			AreEqualHex(2, IntrinsicOps.PopulationCountSoftware(3 << 25));
			AreEqualHex(4, IntrinsicOps.PopulationCountSoftware((3 << 25) | (3 << 5)));

			if (TestX64Code)
			{
				AreEqualHex(2, IntrinsicOps.PopulationCountReplaced(3));
				AreEqualHex(2, IntrinsicOps.PopulationCountReplaced(3 << 5));
				AreEqualHex(2, IntrinsicOps.PopulationCountReplaced(3 << 25));
				AreEqualHex(4, IntrinsicOps.PopulationCountReplaced((3 << 25) | (3 << 5)));
			}
		}

		[Test]
		public void LeadingZeroCount32()
		{
			AreEqualHex(30, IntrinsicOps.LeadingZeroCountSoftware(3));
			AreEqualHex(25, IntrinsicOps.LeadingZeroCountSoftware(3 << 5));
			AreEqualHex(5, IntrinsicOps.LeadingZeroCountSoftware(3 << 25));
			AreEqualHex(5, IntrinsicOps.LeadingZeroCountSoftware((3 << 25) | (3 << 5)));

			if (TestX64Code)
			{
				AreEqualHex(30, IntrinsicOps.LeadingZeroCountReplaced(3));
				AreEqualHex(25, IntrinsicOps.LeadingZeroCountReplaced(3 << 5));
				AreEqualHex(5, IntrinsicOps.LeadingZeroCountReplaced(3 << 25));
				AreEqualHex(5, IntrinsicOps.LeadingZeroCountReplaced((3 << 25) | (3 << 5)));
			}
		}

		[Test]
		public void SwapBytes64()
		{
			AreEqualHex(0x0102030405060708UL, IntrinsicOps.SwapBytes(0x0807060504030201));

			AreEqualHex(0x0807060504030201UL, IntrinsicOps.SwapBytesSoftware(0x0102030405060708));
			AreEqualHex(0x0102030405060708UL, IntrinsicOps.SwapBytesSoftware(0x0807060504030201));

			if (TestX64Code)
			{
				AreEqualHex(0x0807060504030201UL, IntrinsicOps.SwapBytesReplaced(0x0102030405060708));
				AreEqualHex(0x0102030405060708UL, IntrinsicOps.SwapBytesReplaced(0x0807060504030201));
			}
		}

		[Test]
		public void SwapBytes32()
		{
			AreEqualHex(0x01020304U, IntrinsicOps.SwapBytes(0x04030201));

			AreEqualHex(0x04030201U, IntrinsicOps.SwapBytesSoftware(0x01020304));
			AreEqualHex(0x01020304U, IntrinsicOps.SwapBytesSoftware(0x04030201));

			if (TestX64Code)
			{
				AreEqualHex(0x04030201U, IntrinsicOps.SwapBytesReplaced(0x01020304));
				AreEqualHex(0x01020304U, IntrinsicOps.SwapBytesReplaced(0x04030201));
			}
		}

		[Test]
		public void CPUID()
		{
			{
				var ecx = CpuIDOps.CPUIDEcxReplaced(1);
				Console.WriteLine("1h: {0:x}", ecx);
			}
			{
				var ecx = CpuIDOps.CPUIDEcxReplaced(0x80000001);
				Console.WriteLine("80000001h: {0:x}", ecx);
			}
		}

		[Test]
		public void Pause()
		{
			IntrinsicOps.Pause();
		}
	}
}
