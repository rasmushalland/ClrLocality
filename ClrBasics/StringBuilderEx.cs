using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrBasics
{
	public sealed class StringBuilderEx
	{
		private readonly StringBuilder b = new StringBuilder();
		private string[] stringsToAppend = new string[11];
		private int stringsToAppendCount;

		public static StringBuilderEx operator +(StringBuilderEx sbe, string value)
		{
			if (value == null)
				return sbe;
			sbe.stringsToAppend[sbe.stringsToAppendCount] = value;
			sbe.stringsToAppendCount++;

			if (sbe.stringsToAppendCount >= 10)
				sbe.Flush();
			return sbe;
		}

		private void Flush()
		{
			var l = 0;
//			for (int i = 0; i < stringsToAppendCount; i++)
//				l += stringsToAppend[i].Length;
//			b.EnsureCapacity(b.Length + l);
			for (int i = 0; i < stringsToAppendCount; i++)
				b.Append(stringsToAppend[i]);
			stringsToAppendCount = 0;
		}

		public static StringBuilderEx operator +(StringBuilderEx sbe, int value)
		{
			sbe.b.Append(value);
			return sbe;
		}

		public override string ToString()
		{
			Flush();
			return b.ToString();
		}
	}
}