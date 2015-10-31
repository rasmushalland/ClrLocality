using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;

namespace ClrMachineCode.Test
{
	[TestFixture]
	public class String15Test
	{
		[Test]
		public void String15_Basic()
		{
			TestStringType(s => new String15(s), s => s.Length, 15);
		}

		[Test]
		public void String15_Comparison_Basic()
		{
			{
				// Difference is in first half.
				const string lowest = "abco";
				const string highest = "abda";
				Assert.That(new String15(lowest).CompareTo(new String15(highest)), Is.LessThan(0));
				Assert.That(new String15(highest).CompareTo(new String15(lowest)), Is.GreaterThan(0));
			}
			{
				// Difference is in second half.
				const string lowest = "01234567abco";
				const string highest = "01234567abda";
				Assert.That(new String15(lowest).CompareTo(new String15(highest)), Is.LessThan(0));
				Assert.That(new String15(highest).CompareTo(new String15(lowest)), Is.GreaterThan(0));
			}
		}

		[Test]
		public void String15_Comparison()
		{
			var strings = new[] {
				"aben",
				"abf",
				"abe",
				"abd",
			};
			var actual = strings.
				Select(s => new String15(s)).
				OrderBy(s => s).
				Select(s => s.ToString()).
				ToArray();

			var expected = strings.
				OrderBy(s => s, StringComparer.Ordinal).
				ToArray();
			Console.WriteLine("Expected: " + expected.StringJoin(", "));
			Console.WriteLine("Actual: " + actual.StringJoin(", "));
			AreEqualSequences(expected, actual);
		}

		[Test]
		public void String15Ex_Basic()
		{
			TestStringType(s => new String15Ex(s), s => s.Length, null);
		}

		void TestStringType<T>(Func<string, T> ctor, Func<T, int> getLength, int? maxLength)
		{
			{
				// ind og ud, længde.
				const string s1_str = "AsciiString";
				var s1_vt = ctor(s1_str);
				string s1_str_back = s1_vt.ToString();
				AreEqual(s1_str.Length, getLength(s1_vt));
				AreEqual(s1_str, s1_str_back);
			}
			{
				// ind og ud, længde. 2-bytes-tegn.
				const string s1_str = "Måne";
				var s1_vt = ctor(s1_str);
				string s1_str_back = s1_vt.ToString();
				AreEqual(s1_str.Length, getLength(s1_vt));
				AreEqual(s1_str, s1_str_back);
			}
			{
				ctor("der er lige netop plads til mig".Substring(0, maxLength ?? 15));
				if (maxLength != null)
					Throws<ArgumentException>(() => new String15("jeg er lidt for lang".Substring(0, maxLength.Value + 1)));
				else
					ctor("jeg er en meget lang tekststreng. Der er ikke nogen problem i det");
			}
			{
				// lighed.
				AreEqual(ctor("hej"), ctor("hej"));
				AreNotEqual(ctor("hej"), ctor("hejs"));
				AreNotEqual(ctor("hej"), ctor("Hej"));
				AreNotEqual(ctor("hej"), ctor("hEj"));
				AreNotEqual(ctor("hej"), ctor("heJ"));
				if (maxLength == null)
				{
					AreEqual(
						ctor("jeg er en meget lang tekststreng. Der er ikke noget problem i det"),
						ctor("jeg er en meget lang tekststreng. Der er ikke noget problem i det"));
					AreNotEqual(
						ctor("jeg Er en meget lang tekststreng. Der er ikke noget problem i det"),
						ctor("jeg er en meget lang tekststreng. Der er ikke noget problem i det"));
					AreNotEqual(
						ctor("jeg er en meget lang tekststreng. Der er ikke noget problem i det"),
						ctor("jeg er en meget lang tekststreng. Der er ikke noget problem i de"));
					AreNotEqual(
						ctor("jeg er en"),
						ctor("jeg er en meget lang tekststreng. Der er ikke noget problem i de"));
				}
			}
			unsafe
			{
				// IStringContentsUnsafe
				const int shortDestBufferSize = 32;
				var chars = stackalloc char[shortDestBufferSize];
				int shortLength;
				string longDest;

				((IStringContentsUnsafe)ctor("hej")).GetContents(chars, shortDestBufferSize, out shortLength, out longDest);
				AreEqual(new {
					shortLength = 3,
					longDest = (string)null,
				}, new {
					shortLength,
					longDest,
				});
				AreEqual("hej", new string(chars, 0, shortLength));
				((IStringContentsUnsafe)ctor("Måne")).GetContents(chars, shortDestBufferSize, out shortLength, out longDest);
				AreEqual(new {
					shortLength = 4,
					longDest = (string)null,
				}, new {
					shortLength,
					longDest,
				});
				AreEqual("Måne", new string(chars, 0, shortLength));
			}
		}

		[Test]
		public void Performance()
		{
			var cnt = 1000000;
			long sideeffect = 0;
			string shortString = "abcde".Substring(int.Parse("0"));
			string shortStringComparand = "12345";
			string longerString = "abcde012345678".Substring(int.Parse("0"));
			string longerStringComparand = "a3435012aab678".Substring(int.Parse("0"));

			{
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += shortString.GetHashCode();
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, short, String.GetHashCode(): " + cpiter);
			}
			{
				var s15 = new String15("abcde");
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += s15.GetHashCode();
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, short, String15.GetHashCode(): " + cpiter);
			}
			{
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += longerString.GetHashCode();
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, longer, String.GetHashCode(): " + cpiter);
			}
			{
				var s15 = new String15(longerString);
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += s15.GetHashCode();
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, longer, String15.GetHashCode(): " + cpiter);
			}

			// Equality
			{
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += shortString.Equals(shortStringComparand) ? 1 : 0;
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, short, String.Equals(): " + cpiter);
			}
			{
				var s15 = new String15(shortString);
				var s152 = new String15(shortStringComparand);
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += s15.Equals(s152) ? 1 : 0;
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, short, String15.Equals(): " + cpiter);
			}
			{
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += longerString.Equals(longerStringComparand) ? 1 : 0;
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, longer, String.Equals(): " + cpiter);
			}
			{
				var s15 = new String15(longerString);
				var s152 = new String15(longerStringComparand);
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += s15.Equals(s152) ? 1 : 0;
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, longer, String15.Equals(): " + cpiter);
			}

			// Comparison.
			{
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += StringComparer.Ordinal.Compare(shortString, shortStringComparand);
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, short, Compare(): " + cpiter);
			}
			{
				var s15 = new String15(shortString);
				var s152 = new String15(shortStringComparand);
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += s15.CompareTo(s152);
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, short, String15.CompareTo(): " + cpiter);
			}
			{
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += StringComparer.Ordinal.Compare(longerString, longerStringComparand);
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, longer, Compare(): " + cpiter);
			}
			{
				var s15 = new String15(longerString);
				var s152 = new String15(longerStringComparand);
				var sw = ThreadCycleStopWatch.StartNew();
				for (int i = 0; i < cnt; i++)
					sideeffect += s15.CompareTo(s152);
				var cycles = sw.GetCurrentCycles();
				var cpiter = cycles / cnt;
				Console.WriteLine("Avg cycles per iteration, longer, String15.CompareTo(): " + cpiter);
			}
		}

		static void AreEqual<T>(T expected, T actual)
		{
			Assert.AreEqual(expected, actual, " hex: expected {0:X}, got {1:X}.", expected, actual);
		}
		static void AreNotEqual<T>(T expected, T actual)
		{
			Assert.AreNotEqual(expected, actual, " hex: expected {0:X}, got {1:X}.", expected, actual);
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