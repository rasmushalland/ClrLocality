using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml.Serialization;
using NUnit.Framework;

namespace ClrLocality.Test
{
	[TestFixture]
	public class String15Test : UnitTestBase
	{
//		private static readonly object Dummy = MachineCodeClassMarker.EnsurePrepared(typeof(IntrinsicOps));

		[TestFixtureSetUp]
		public void SetUp()
		{
			MachineCodeHandler.TraceSource.Listeners.Add(new ConsoleTraceListener());
			MachineCodeHandler.TraceSource.Switch.Level = SourceLevels.All;

			MachineCodeClassMarker.EnsurePrepared(typeof(IntrinsicOps));
		}

		[Test]
		public void String15_Basic()
		{
			TestStringType(s => new String15(s), s => s.Length, (s, buf, index) => s.CopyTo(buf, index), 15);
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
	    public void String15_Comparison_Basic2()
	    {
			var s1str = "øiiiiøøøøi";
			var s2str = "øiiiiøøøø";

			var s1 = new String15(s1str);
			var s2 = new String15(s2str);

			var errors = new List<string>();
			{
				var expected = (ComparisonResult)Math.Sign(StringComparer.Ordinal.Compare(s2str, s1str));
				var actual = (ComparisonResult)Math.Sign(s2.CompareTo(s1));
				if (actual != expected)
				{
					errors.Add($"Comparison failed. x='{s2str}', y='{s1str}'. Expected=" + expected + ", actual=" + actual);
				}
			}
			{
				var expected = (ComparisonResult)Math.Sign(StringComparer.Ordinal.Compare(s1str, s2str));
				var actual = (ComparisonResult)Math.Sign(s1.CompareTo(s2));
				if (actual != expected)
				{
					errors.Add($"Comparison failed. x='{s1str}', y='{s2str}'. Expected=" + expected + ", actual=" + actual);
				}
			}
			Assert.IsEmpty(errors);
		}

		[Test]
        public void String15_Comparison_Ex()
	    {
            var strings = GenerateStrings().ToList();
            var errors = new List<string>();
	        int i = 1;
            for (; i < strings.Count; i++)
            {
                var prevstr = strings[i - 1];
                var curstr = strings[i];

                if (Encoding.UTF8.GetByteCount(curstr) > 15)
                    continue;

                var cur = new String15(curstr);

                AreEqualEx(cur, cur);
                AreEqualEx(0, cur.CompareTo(cur));

                // prepare with the previous string.
                if (Encoding.UTF8.GetByteCount(prevstr) > 15)
                    continue;

                var prev = new String15(prevstr);

	            if (Math.Sign(cur.CompareTo(prev)) != Math.Sign(StringComparer.Ordinal.Compare(curstr, prevstr)))
		            errors.Add($"Comparison failed. cur='{curstr}', prev='{prevstr}'.");
	            if (Math.Sign(prev.CompareTo(cur)) != Math.Sign(StringComparer.Ordinal.Compare(prevstr, curstr)))
		            errors.Add($"Comparison failed. cur='{curstr}', prev='{prevstr}'.");

	            if (errors.Count > 100)
                {
                    Console.WriteLine("Reached error limit after {0} iterations, breaking.", i);
                    break;
                }
            }


	        if (errors.Count == 0)
	            Console.WriteLine("no errors");
	        else
	        {
	            Console.WriteLine("Errors:");
	            Console.WriteLine(errors.StringJoin("\r\n"));
	            Assert.Fail($"{errors.Count} tests failed.");
	        }
        }

		[Test]
        public void String15Ex_Comparison_Ex()
	    {
            var strings = GenerateStrings(24).ToList();
            var errors = new List<string>();
	        int i = 1;
            for (; i < strings.Count; i++)
            {
                var prevstr = strings[i - 1];
                var curstr = strings[i];

                var cur = new String15Ex(curstr);

                AreEqualEx(cur, cur);
                AreEqualEx(0, cur.CompareTo(cur));

                // compare with the previous string.
                var prev = new String15Ex(prevstr);

	            if (Math.Sign(cur.CompareTo(prev)) != Math.Sign(StringComparer.Ordinal.Compare(curstr, prevstr)))
		            errors.Add($"Comparison failed. cur='{curstr}', prev='{prevstr}'.");
	            if (Math.Sign(prev.CompareTo(cur)) != Math.Sign(StringComparer.Ordinal.Compare(prevstr, curstr)))
		            errors.Add($"Comparison failed. cur='{curstr}', prev='{prevstr}'.");

	            if (errors.Count > 100)
                {
                    Console.WriteLine("Reached error limit after {0} iterations, breaking.", i);
                    break;
                }
            }


	        if (errors.Count == 0)
	            Console.WriteLine("no errors");
	        else
	        {
	            Console.WriteLine("Errors:");
	            Console.WriteLine(errors.StringJoin("\r\n"));
	            Assert.Fail($"{errors.Count} tests failed.");
	        }
        }

		enum ComparisonResult
		{
			LessThan = -1,
			Equal = 0,
			GreaterThan = 1,
		}

		[Test]
        public void String15_Construction()
	    {
            var strings = GenerateStrings().ToList();
            var errors = new List<string>();
	        int i = 0;
            for (; i < strings.Count; i++)
            {
                var curstr = strings[i];

                if (Encoding.UTF8.GetByteCount(curstr) > 15)
                {
                    try
                    {
                        var _ = new String15(curstr);
                        errors.Add("String too long, but no error: " + curstr);
                    }
                    catch (ArgumentException)
                    {
                    }
                    continue;
                }

                var cur = new String15(curstr);

                AreEqualEx(cur, cur);

                string back;
                try
                {
                    back = cur.ToString();
                }
                catch (Exception e)
                {
                    errors.Add("Failed ToString() for string '" + curstr + "': " + e.Message);
                    goto checkerrors;
                }
                if (back != curstr)
                    errors.Add($"Got wrong string back. cur='{curstr}', got back='{back}'.");


                checkerrors:
                if (errors.Count > 100)
                {
                    Console.WriteLine("Reached error limit after {0} iterations, breaking.", i);
                    break;
                }
            }

	        if (errors.Count == 0)
	            Console.WriteLine("no errors");
	        else
	        {
	            Console.WriteLine("Errors:");
	            Console.WriteLine(errors.StringJoin("\r\n"));
	            Assert.Fail($"{errors.Count} tests failed.");
	        }
        }

        [Test]
		public unsafe void String15_AsciiToCharReplaced()
		{
			var str = new String15("abcdefghijklmno");
			var buf = stackalloc char[32];
			IntrinsicOps.AsciiToCharReplaced(str._long2, str._long1, (IntPtr)buf);
			var strback = new string(buf, 0, str.Length);
			AreEqualEx("abcdefghijklmno", strback);
		}

		[Test]
		public void String15Ex_Basic()
		{
			TestStringType(s => new String15Ex(s), s => s.Length, null, null);
		}

		void TestStringType<T>(Func<string, T> ctor, Func<T, int> getLength, Func<T, char[], int, int> copyTo, int? maxLength)
		{
			{
				// in and out, length.
				const string s1_str = "AsciiString";
				var s1_vt = ctor(s1_str);
				string s1_str_back = s1_vt.ToString();
				AreEqualEx(s1_str.Length, getLength(s1_vt));
				AreEqualEx(s1_str, s1_str_back);
			}
			{
				// in and out, length. two-byte code points.
				const string s1_str = "Måne";
				var s1_vt = ctor(s1_str);
				string s1_str_back = s1_vt.ToString();
				AreEqualEx(s1_str.Length, getLength(s1_vt));
				AreEqualEx(s1_str, s1_str_back);
			}
			{
				// in and out, length. three-byte code points.
				const string s1_str = "some €";
				var s1_vt = ctor(s1_str);
				string s1_str_back = s1_vt.ToString();
				AreEqualEx(s1_str.Length, getLength(s1_vt));
				AreEqualEx(s1_str, s1_str_back);
			}
			if (copyTo != null)
			{
				// in and out, CopyTo. two-byte code points.
				const string s1_str = "Måne";
				var s1_vt = ctor(s1_str);
				var s1_str_back = "01234567890123456789".ToCharArray();
				var len = copyTo(s1_vt, s1_str_back, 1);
				AreEqualEx(s1_str.Length, len);
				AreEqualEx("Måne", new string(s1_str_back, 1, len));
				AreEqualEx("0Måne567890123456789", new string(s1_str_back));
			}
			else
				Console.WriteLine("Cannot test CopyTo: no implementation was given.");
			{
				ctor("I will fit precisely into some string".Substring(0, maxLength ?? 15));
				if (maxLength != null)
					Throws<ArgumentException>(() => new String15("I am a bit too long".Substring(0, maxLength.Value + 1)));
				else
					ctor("I am a pretty long text string. No problem.");
			}
			{
				// lighed.
				AreEqualEx(ctor("hej"), ctor("hej"));
				AreNotEqualEx(ctor("hej"), ctor("hejs"));
				AreNotEqualEx(ctor("hej"), ctor("Hej"));
				AreNotEqualEx(ctor("hej"), ctor("hEj"));
				AreNotEqualEx(ctor("hej"), ctor("heJ"));
				if (maxLength == null)
				{
					AreEqualEx(
						ctor("I am a pretty long text string. No problem whatsoever."),
						ctor("I am a pretty long text string. No problem whatsoever."));
					AreNotEqualEx(
						ctor("I am a Pretty long text string. No problem whatsoever."),
						ctor("I am a pretty long text string. No problem whatsoever."));
					AreNotEqualEx(
						ctor("I am a pretty long text string. No problem whatsoever."),
						ctor("I am a pretty long text string. No problem whatsoever"));
					AreNotEqualEx(
						ctor("I am a"),
						ctor("I am a pretty long text string. No problem whatsoever."));
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
				AreEqualEx(new {
					shortLength = 3,
					longDest = (string)null,
				}, new {
					shortLength,
					longDest,
				});
				AreEqualEx("hej", new string(chars, 0, shortLength));
				((IStringContentsUnsafe)ctor("Måne")).GetContents(chars, shortDestBufferSize, out shortLength, out longDest);
				AreEqualEx(new {
					shortLength = 4,
					longDest = (string)null,
				}, new {
					shortLength,
					longDest,
				});
				AreEqualEx("Måne", new string(chars, 0, shortLength));
			}

			{
				// string takes up less space than one long.
				var str = ctor("s");
				var long1 = (ulong)typeof(T).GetField("_long1", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(str);
				var long2 = (ulong)typeof(T).GetField("_long2", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(str);
				// Comparisons might be messed up if trailing bytes are not cleared.
				Assert.Less(IntrinsicOps.PopulationCountSoftware(long1), 8, "Too many bits in long1 - the trailing bytes aren't cleared?");
				Assert.Less(IntrinsicOps.PopulationCountSoftware(long2), 8, "Too many bits in long2 - the trailing bytes aren't cleared?");
			}
			{
				// string takes up more space than one long.
				var str = ctor("01234567s");
				var long1 = (ulong)typeof(T).GetField("_long1", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(str);
				var long2 = (ulong)typeof(T).GetField("_long2", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance).GetValue(str);
				// Comparisons might be messed up if trailing bytes are not cleared.
				Assert.Less(IntrinsicOps.PopulationCountSoftware(long1), 8+8, "Too many bits in long1 - the trailing bytes aren't cleared?");
			}
		}


	    static IEnumerable<string> GenerateStrings(int maxlen = 16)
	    {
	        var buf = new char[maxlen];

            int targetcount = 100 * 1000;
	        var rand = new Random(42);
            for (int sno = 0; sno < targetcount; sno++)
            {
                var charcount = rand.Next(1, maxlen);

                for (int i = 0; i < charcount; i++)
                {
                    var bytelen = rand.Next(1, 3);
                    char c;
                    if (bytelen == 1)
                        c = 'i';
                    else if (bytelen == 2)
                        c = 'ø';
                    else if (bytelen == 3)
                        c = '€';
                    else throw new Exception("bad bytelen");

                    buf[i] = c;
                }

                yield return new string(buf, 0, charcount);
            }
	    }

		static void BMCycles(string name, Func<long> doit)
		{
			doit();
			var repetitions = 10;
			var measurements = new List<long>(repetitions);
			long iterations = 0;
			for (int rep = 0; rep < repetitions; rep++)
			{
				var sw = ThreadCycleStopWatch.StartNew();
				iterations = doit();
				var elapsed = sw.GetCurrentCycles();
				measurements.Add(elapsed);
			}
			{
				var elapsed = (long)measurements.Average();
				//if (outputMarkdownTable)
				//	Console.WriteLine("|" + name + " | " + (elapsed / iterations) + " |");
				//else
				Console.WriteLine($"{name}: {elapsed / iterations} cycles/iter.");
			}
		}


		[Test]
		public void BenchmarkOperations()
		{
			var cnt = 500000;
			string shortString = "abcde".Substring(int.Parse("0"));
			string shortStringMb = "abcdñ".Substring(int.Parse("0"));
			string shortStringComparand = "12345";
			string longerString = "abcde012345678".Substring(int.Parse("0"));
			string longerStringMb = "abcdñ012345678".Substring(int.Parse("0"));
			string longerStringComparand = "a3435012aab678".Substring(int.Parse("0"));

			// Constructor
			{
				BMCycles("ctor(), String15, short, ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
					{
						var string15 = new String15(shortString);
						sideeffect ^= i;
					}
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("ctor(), String15, short, non-ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
					{
						var string15 = new String15(shortStringMb);
						sideeffect ^= i;
					}
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("ctor(), String, short", () => {
					long sideeffect = 0;
					var arr = shortString.ToArray();
					for (int i = 0; i < cnt; i++)
					{
						var str = new string(arr, 0, arr.Length);
						sideeffect ^= str.Length;
					}
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("ctor(), String15, longer, ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
					{
						var string15 = new String15(longerString);
						sideeffect ^= i;
					}
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("ctor(), String15, longer, non-ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
					{
						var string15 = new String15(longerStringMb);
						sideeffect ^= i;
					}
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("ctor(), String, longer", () => {
					long sideeffect = 0;
					var arr = longerString.ToArray();
					for (int i = 0; i < cnt; i++)
					{
						var str = new string(arr, 0, arr.Length);
						sideeffect ^= str.Length;
					}
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// GetHashCode
			{
				BMCycles("GetHashCode(), String, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += shortString.GetHashCode();
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15("abcde");
				BMCycles("GetHashCode(), String15, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.GetHashCode();
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("GetHashCode(), String, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += longerString.GetHashCode();
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(longerString);
				BMCycles("GetHashCode(), String15, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.GetHashCode();
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// Equality
			{
				BMCycles("Equals(), String, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += shortString.Equals(shortStringComparand) ? 1 : 0;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(shortString);
				var s152 = new String15(shortStringComparand);

				BMCycles("Equals(), String15, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.Equals(s152) ? 1 : 0;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("Equals(), String, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += longerString.Equals(longerStringComparand) ? 1 : 0;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(longerString);
				var s152 = new String15(longerStringComparand);

				BMCycles("Equals(), String15, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.Equals(s152) ? 1 : 0;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// Comparison.
			{
				BMCycles("CompareTo(), String, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += StringComparer.Ordinal.Compare(shortString, shortStringComparand);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(shortString);
				var s152 = new String15(shortStringComparand);

				BMCycles("CompareTo(), String15, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.CompareTo(s152);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				BMCycles("CompareTo(), String, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += StringComparer.Ordinal.Compare(longerString, longerStringComparand);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(longerString);
				var s152 = new String15(longerStringComparand);
				BMCycles("CompareTo(), String15, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.CompareTo(s152);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// ToString()
			{
				var s15 = new String15(shortString);
				BMCycles("ToString(), String15, short", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.ToString().Length;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(longerString);
				BMCycles("ToString(), String15, longer", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.ToString().Length;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// CopyTo()
			{
				var s15 = new String15(shortString);
				var dest = new char[20];
				BMCycles("CopyTo(), String15, short, ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.CopyTo(dest, 1);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(shortStringMb);
				var dest = new char[20];
				BMCycles("CopyTo(), String15, short, non-ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.CopyTo(dest, 1);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(longerString);
				var dest = new char[20];
				BMCycles("CopyTo(), String15, longer, ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.CopyTo(dest, 1);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var s15 = new String15(longerStringMb);
				var dest = new char[20];
				BMCycles("CopyTo(), String15, longer, non-ascii", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += s15.CopyTo(dest, 1);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// property get
			{
				var obj = new {s15 = new String15(shortString)};
				BMCycles("Get from property, String15, inlined", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += (long) obj.s15._long1;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var obj = new StringWrapper(shortString, new String15(shortString));
				BMCycles("Get from property, String15, not inlined", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += (long) obj.Str15._long1;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var obj = new {s = shortString };
				BMCycles("Get from property, String, inlined", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += obj.s.Length;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var obj = new StringWrapper(shortString, new String15(shortString));
				BMCycles("Get from property, String, not inlined", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += obj.Str.Length;
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}

			// Call method
			{
				var s15 = new String15(shortString);
				BMCycles("Pass as argument, String15", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += NonInlinedMethod(s15);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
			{
				var str = shortString;
				BMCycles("Pass as argument, String", () => {
					long sideeffect = 0;
					for (int i = 0; i < cnt; i++)
						sideeffect += NonInlinedMethod(str);
					AssertSideeffectNone(sideeffect);
					return cnt;
				});
			}
		}
	

		[MethodImpl(MethodImplOptions.NoInlining)]
		static int NonInlinedMethod(string str) => 42;

		[MethodImpl(MethodImplOptions.NoInlining)]
		static int NonInlinedMethod(String15 str) => 42;

		#region StringWrapper

		private class StringWrapper
		{
			private readonly string _str;

			public string Str
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get { return _str; }
			}

			private readonly String15 _str15;

			public String15 Str15
			{
				[MethodImpl(MethodImplOptions.NoInlining)] get { return _str15; }
			}

			public StringWrapper(string str, String15 str15)
			{
				_str = str;
				_str15 = str15;
			}
		}

		#endregion

		/// <summary>
		/// Benchmarks garbage collection with and without custom string types.
		/// </summary>
		[Test]
		public void GCBenchmark()
		{
			{
				// Approx 2 GB data, record format without String15 as follows:
				// - string: 10 chars, 100% present (8 + 12 + 10*2 = 40 bytes).
				// - string: 10 chars, 100% present (8 + 12 + 10*2 = 40 bytes).
				// - string: 200 chars, 5% present with 50 chars (8 + 12 + 50*2 = 120 bytes).
				// - value type fields: 32 bytes.
				// Per obj: 16 + 40 + 40 + 6 + 32 = 134 bytes, ~3 objects.
				// 2e9 / 134 = 15 million records.

				Func<object> build = () => {
					var str1 = "string1234".ToCharArray();
					var str2 = "string1234".ToCharArray();
					var str3 = new string('x', 50).ToCharArray();
					var rand = new Random(42);
					var objects_ = Enumerable.Range(0, 15 * 1000 * 1000).
						Select(_ => new SomeRecordWithString(1, 2, 3, 4,
							new string(str1, 0, str1.Length), new string(str2, 0, str2.Length), rand.Next(0, 99) < 5 ? new string(str3, 0, str3.Length) : null)).
						ToList();
					return objects_;
				};

				BMGarbageCollection("GC.Collect, SomeRecordWithString", build);
			}

			{
				// Record format with String15 as follows:
				// - string: 10 chars, 100% present (16 bytes).
				// - string: 10 chars, 100% present (16 bytes).
				// - string: 200 chars, 5% present with 50 chars (8 + 12 + 50*2 = 120 bytes).
				// - value type fields: 32 bytes.
				// Per obj: 16 + 16 + 16 + 6 + 32 = 86 bytes, ~1 objects.
				// 15 million records * 86 = 1.3 GB.


				Func<object> build = () => {
					var str1 = "string1234";
					var str2 = "string1234";
					var str3 = new string('x', 50).ToCharArray();
					var rand = new Random(42);
					var objects_ = Enumerable.Range(0, 15 * 1000 * 1000).
						Select(_ => new SomeRecordWithString15(1, 2, 3, 4,
							new String15(str1), new String15(str2), rand.Next(0, 99) < 5 ? new string(str3, 0, str3.Length) : null)).
						ToList();
					return objects_;
				};

				BMGarbageCollection("GC.Collect, SomeRecordWithString15", build);
			}
			{
				// Avoid string entirely:
				//
				// Record format with String15 as follows:
				// - string: 10 chars, 100% present (16 bytes).
				// - string: 10 chars, 100% present (16 bytes).
				// - string: 10 chars, 100% present (16 bytes).
				// - value type fields: 32 bytes.
				// Per obj: 16 + 16 + 16 + 16 + 32 = 96 bytes, ~1 objects.
				// 15 million records * 86 = 1.4 GB.


				Func<object> build = () => {
					var str1 = "string1234";
					var str2 = "string1234";
					var str3 = "string1234";
					var rand = new Random(42);
					var objects_ = Enumerable.Range(0, 15 * 1000 * 1000).
						Select(_ => new SomeRecordWithString15Only(1, 2, 3, 4,
							new String15(str1), new String15(str2), new String15(str3))).
						ToList();
					return objects_;
				};

				BMGarbageCollection("GC.Collect, SomeRecordWithString15Only", build);
			}
		}

		private static void BMGarbageCollection(string title, Func<object> build)
		{
			var objects = build();
			GC.Collect();

			var ms = Enumerable.Range(0, 3).Select(_ => {
				var sw = Stopwatch.StartNew();
				GC.Collect();
				return sw.ElapsedMilliseconds;
			}).Average();
			Console.WriteLine(title + ": " + Math.Round(ms) + " ms");


			GC.KeepAlive(objects);
			GC.Collect();
		}

		#region SomeRecordWithString

		private sealed class SomeRecordWithString
		{
			public long Id { get; }
			public long Long1 { get; }
			public long Long2 { get; }
			public long Long3 { get; }

			public string String1 { get; }
			public string String2 { get; }
			public string String3 { get; }

			public SomeRecordWithString(long id, long long1, long long2, long long3, string string1, string string2,
				string string3)
			{
				Id = id;
				Long1 = long1;
				Long2 = long2;
				Long3 = long3;
				String1 = string1;
				String2 = string2;
				String3 = string3;
			}
		}

		#endregion

		#region SomeRecordWithString15

		private sealed class SomeRecordWithString15
		{
			public long Id { get; }
			public long Long1 { get; }
			public long Long2 { get; }
			public long Long3 { get; }

			public String15 String1 { get; }
			public String15 String2 { get; }
			public string String3 { get; }

			public SomeRecordWithString15(long id, long long1, long long2, long long3, String15 string1, String15 string2,
				string string3)
			{
				Id = id;
				Long1 = long1;
				Long2 = long2;
				Long3 = long3;
				String1 = string1;
				String2 = string2;
				String3 = string3;
			}
		}

		#endregion

		#region SomeRecordWithString15Only

		private sealed class SomeRecordWithString15Only
		{
			public long Id { get; }
			public long Long1 { get; }
			public long Long2 { get; }
			public long Long3 { get; }

			public String15 String1 { get; }
			public String15 String2 { get; }
			public String15 String3 { get; }

			public SomeRecordWithString15Only(long id, long long1, long long2, long long3, String15 string1, String15 string2,
				String15 string3)
			{
				Id = id;
				Long1 = long1;
				Long2 = long2;
				Long3 = long3;
				String1 = string1;
				String2 = string2;
				String3 = string3;
			}
		}

		#endregion


		#region Serialization

		[Serializable]
		public class SomeRecordForSerializationString15
		{
			public String15 String15 { get; set; }

			public SomeRecordForSerializationString15(String15 string15)
			{
				String15 = string15;
			}

			public SomeRecordForSerializationString15()
			{
				
			}
		}

		[Test]
		public void SerializationString15_BinaryFormatter()
		{
			var record = new SomeRecordForSerializationString15(new String15("my string"));

			var ms = new MemoryStream();
			new BinaryFormatter().Serialize(ms, record);
			ms.Position = 0;

			var deser = (SomeRecordForSerializationString15) new BinaryFormatter().Deserialize(ms);
			AreEqualEx("my string", deser.String15.ToString());
		}

		[Test]
		public void SerializationString15_XmlSerializer()
		{
			var record = new SomeRecordForSerializationString15(new String15("my string"));

			var ser = new XmlSerializer(typeof (SomeRecordForSerializationString15));
			var sw = new StringWriter();
			ser.Serialize(sw, record);

			var deser = (SomeRecordForSerializationString15) ser.Deserialize(new StringReader(sw.GetStringBuilder().ToString()));
			AreEqualEx("my string", deser.String15.ToString());
		}

		[Serializable]
		public class SomeRecordForSerializationString15Ex
		{
			public String15Ex String15 { get; set; }

			public SomeRecordForSerializationString15Ex(String15Ex string15)
			{
				String15 = string15;
			}

			public SomeRecordForSerializationString15Ex()
			{
				
			}
		}

		[Test]
		public void SerializationString15Ex_BinaryFormatter()
		{
			{
				// Short string
				var record = new SomeRecordForSerializationString15Ex(new String15Ex("my string"));

				var ms = new MemoryStream();
				new BinaryFormatter().Serialize(ms, record);
				ms.Position = 0;

				var deser = (SomeRecordForSerializationString15Ex) new BinaryFormatter().Deserialize(ms);
				AreEqualEx("my string", deser.String15.ToString());
			}
			{
				// Longer string
				var record = new SomeRecordForSerializationString15Ex(new String15Ex("a longer string that does not fit into the long fields."));

				var ms = new MemoryStream();
				new BinaryFormatter().Serialize(ms, record);
				ms.Position = 0;

				var deser = (SomeRecordForSerializationString15Ex) new BinaryFormatter().Deserialize(ms);
				AreEqualEx("a longer string that does not fit into the long fields.", deser.String15.ToString());
			}
		}

		[Test]
		public void SerializationString15Ex_XmlSerializer()
		{
			var record = new SomeRecordForSerializationString15Ex(new String15Ex("my string"));

			var ser = new XmlSerializer(typeof (SomeRecordForSerializationString15Ex));
			var sw = new StringWriter();
			ser.Serialize(sw, record);

			var deser = (SomeRecordForSerializationString15Ex) ser.Deserialize(new StringReader(sw.GetStringBuilder().ToString()));
			AreEqualEx("my string", deser.String15.ToString());
		}

		#endregion

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static void AssertSideeffectNone(long sideeffect)
		{
		}

		static void AreEqualEx<T>(T expected, T actual)
		{
			if (typeof(T) == typeof(ulong))
				Assert.AreEqual(expected, actual, " hex: expected {0:X}, got {1:X}.", expected, actual);
			else
				Assert.AreEqual(expected, actual, " expected {0}, got {1}.", expected, actual);
		}

		static void AreNotEqualEx<T>(T expected, T actual)
		{
			if (typeof(T) == typeof(ulong))
				Assert.AreNotEqual(expected, actual, " hex: expected {0:X}, got {1:X}.", expected, actual);
			else
				Assert.AreNotEqual(expected, actual, " expected {0}, got {1}.", expected, actual);
		}
	}
}