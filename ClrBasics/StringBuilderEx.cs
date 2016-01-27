using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrBasics
{
	public sealed class StringBuilderEx
	{
		readonly StringBuilder _sb = new StringBuilder();

		public static StringBuilderEx operator +(StringBuilderEx sbe, string value)
		{
			sbe._sb.Append(value);
			return sbe;
		}

		public static StringBuilderEx operator +(StringBuilderEx sbe, int value)
		{
			sbe._sb.Append(value);
			return sbe;
		}

		public override string ToString() => _sb.ToString();
	}
}
