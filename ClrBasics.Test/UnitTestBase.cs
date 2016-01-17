using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ClrBasics.Test
{
	public class UnitTestBase
	{
		public static void AreEqual<T>(T expected, T actual)
		{
			Assert.AreEqual(expected, actual, "expected 0x{0:X}, got 0x{1:X}.", expected, actual);
		}

		public static void AreNotEqual<T>(T expected, T actual)
		{
			Assert.AreNotEqual(expected, actual, " expected {0}, got {1}.", expected, actual);
		}

		public static void AreEqualSequences<T>(IEnumerable<T> expected, IEnumerable<T> actual)
		{
			Assert.AreEqual(expected is IList<T> ? expected : expected.ToList(), actual is IList<T> ? actual : actual.ToList());
		}

		public static void AreEqualSequences<T>(IEnumerable<T> expected, IEnumerable<T> actual, string message)
		{
			Assert.AreEqual(expected is IList<T> ? expected : expected.ToList(), actual is IList<T> ? actual : actual.ToList(), message);
		}

		[DebuggerStepThrough]
		public static T Throws<T>(Action action) where T : Exception
		{
			try
			{
				action();
			}
			catch (T ex)
			{
				return ex;
			}
			Assert.Fail("Exception of type " + typeof(T).Name + " was expected.");
			return null;
		}
	}
}
