using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class IntrinsicOpsTest
	{
		private static readonly object Dummy = MachineCodeClassMarker.Prepare(typeof(IntrinsicOps));

		[Test]
		public static void PopulationCount64()
		{
			AreEqual(2, IntrinsicOps.PopulationCountSoftware(3L));
			AreEqual(2, IntrinsicOps.PopulationCountSoftware(3L << 5));
			AreEqual(2, IntrinsicOps.PopulationCountSoftware(3L << 55));
			AreEqual(4, IntrinsicOps.PopulationCountSoftware((3 << 55) | (3 << 5)));

			AreEqual(2, IntrinsicOps.PopulationCount(3L));
			AreEqual(2, IntrinsicOps.PopulationCount(3L << 5));
			AreEqual(2, IntrinsicOps.PopulationCount(3L << 55));
			AreEqual(4, IntrinsicOps.PopulationCount((3 << 55) | (3 << 5)));
		}

		[Test]
		public static void PopulationCount32()
		{
			AreEqual(2, IntrinsicOps.PopulationCountSoftware(3));
			AreEqual(2, IntrinsicOps.PopulationCountSoftware(3 << 5));
			AreEqual(2, IntrinsicOps.PopulationCountSoftware(3 << 25));
			AreEqual(4, IntrinsicOps.PopulationCountSoftware((3 << 25) | (3 << 5)));

			AreEqual(2, IntrinsicOps.PopulationCount(3));
			AreEqual(2, IntrinsicOps.PopulationCount(3 << 5));
			AreEqual(2, IntrinsicOps.PopulationCount(3 << 25));
			AreEqual(4, IntrinsicOps.PopulationCount((3 << 25) | (3 << 5)));
		}

		[Test]
		public static void SwapBytes64()
		{
			//MachineCodeHandler.PrepareClass(typeof(IntrinsicOps));
			AreEqual(0x0807060504030201UL, IntrinsicOps.SwapBytes(0x0102030405060708));
			AreEqual(0x0102030405060708UL, IntrinsicOps.SwapBytes(0x0807060504030201));
		}

		static void AreEqual<T>(T expected, T actual)
		{
			if (!EqualityComparer<T>.Default.Equals(expected, actual))
				throw new ApplicationException("Expected " + expected + ", got " + actual);
		}
	}
}
