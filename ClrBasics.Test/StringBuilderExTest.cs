using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace ClrBasics.Test
{
	[TestFixture]
	public class StringBuilderExTest : UnitTestBase
	{
		[Test]
		public void StringBuilderExTestBasic()
		{
			var sb = new StringBuilderEx();
			sb += "hey" + 123 + "you";

			AreEqual("hey123you", sb.ToString());
		}

		[Test]
		public void StringBuilderExTestBenchmark()
		{
			int count = 1 * 1000 * 1000;

			// *********************
			// few, short
			// *********************

			BM("StringBuilderEx, few, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = new StringBuilderEx();
					sb += "hey" + 123 + "you";
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				return count;
			});
			BM("String.Concat, few, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var res = "hey" + 123 + "you";
					sideeffect += res != null ? 1 : 0;
				}
				return count;
			});
			BM("StringBuilder, few, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = new StringBuilder();
					sb.Append("hey");
					sb.Append(123);
					sb.Append("you");
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				return count;
			});

			// *********************
			// 50 strings, short
			// *********************

			// lower iteration count because string concat is slow.
			count = 100 * 1000;

			BM("StringBuilderEx, 50 strings, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = new StringBuilderEx();
					for (int n = 0; n < 50; n++)
						sb += "hey there";
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				AreEqual(count, sideeffect);
				return count;
			});

			BM("String.Concat, 50 strings, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = "";
					for (int n = 0; n < 50; n++)
						sb += "hey there";
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				AreEqual(count, sideeffect);
				return count;
			});
			BM("StringBuilder, 50 strings, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = new StringBuilder();
					for (int n = 0; n < 50; n++)
						sb.Append("hey there");
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				AreEqual(count, sideeffect);
				return count;
			});

			// *********************
			// 50 strings + int, short
			// *********************

			// lower iteration count because string concat is slow.
			count = 100 * 1000;

			BM("StringBuilderEx, 50 strings + int, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = new StringBuilderEx();
					for (int n = 0; n < 50; n++)
						sb += "hey there" + 123;
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				AreEqual(count, sideeffect);
				return count;
			});

			BM("String.Concat, 50 strings + int, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = "";
					for (int n = 0; n < 50; n++)
						sb += "hey there" + 123;
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				AreEqual(count, sideeffect);
				return count;
			});
			BM("StringBuilder, 50 strings + int, short", () => {
				var sideeffect = 0;
				for (var i = 0; i < count; i++)
				{
					var sb = new StringBuilder();
					for (int n = 0; n < 50; n++)
					{
						sb.Append("hey there");
						sb.Append(123);
					}
					sideeffect += sb.ToString() != null ? 1 : 0;
				}
				AreEqual(count, sideeffect);
				return count;
			});
		}
	}
}
