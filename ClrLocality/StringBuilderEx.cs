using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrLocality
{
	/// <summary>
	/// A syntactically more convenient <see cref="StringBuilder"/>.
	/// Could optimize further (e.g. more work to avoid allocations in various kinds of string building work).
	/// 
	/// However, it's not clear that this class provides enough advantages to justify its existence.
	/// </summary>
	public sealed class StringBuilderEx
	{
		private readonly StringBuilder _b = new StringBuilder();
		private readonly string[] _stringsToAppend = new string[11];
		private int _stringsToAppendCount;

		public static StringBuilderEx operator +(StringBuilderEx sbe, string value)
		{
			if (value == null)
				return sbe;
			sbe._stringsToAppend[sbe._stringsToAppendCount] = value;
			sbe._stringsToAppendCount++;

			if (sbe._stringsToAppendCount >= 10)
				sbe.Flush();
			return sbe;
		}

		private void Flush()
		{
//			var l = 0;
//			for (int i = 0; i < stringsToAppendCount; i++)
//				l += stringsToAppend[i].Length;
//			b.EnsureCapacity(b.Length + l);

			for (int i = 0; i < _stringsToAppendCount; i++)
				_b.Append(_stringsToAppend[i]);
			_stringsToAppendCount = 0;
		}

		public static StringBuilderEx operator +(StringBuilderEx sbe, int value)
		{
			sbe._b.Append(value);
			return sbe;
		}

		public override string ToString()
		{
			Flush();
			return _b.ToString();
		}
	}
}