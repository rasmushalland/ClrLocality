using System;
using System.Runtime.CompilerServices;
using System.Text;

namespace ClrBasics
{
	static class UnsafeStringUtility
	{
		public static unsafe int GetContents(char* shortDestBuffer, int shortDestBufferSize, ulong long1, ulong long2, int lengthPos)
		{
			var bytecount = GetLength_Bytes(long1, long2, lengthPos);
			var buf1 = stackalloc byte[16];
			var lbuf1 = (ulong*)buf1;
			lbuf1[0] = long1;
			lbuf1[1] = long2;

			var buf2 = stackalloc byte[16];
			for (int i = 0; i < bytecount; i++)
				buf2[i] = buf1[15 - i];
			// Burde nok tjekke her, om der er plads nok.
			return Encoding.UTF8.GetChars(buf2, bytecount, shortDestBuffer, shortDestBufferSize);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int GetLength_Utf16CodeUnits(ulong long1, ulong long2, int lengthPos)
		{
			var b = (byte)long1;
			var l = (b & StringLengthBitMask) >> StringLengthBitOffset;
			return l;
		}

		public static int GetLength_Bytes(ulong long1, ulong long2, int lengthPos)
		{
			var b = (byte)long1;
			var l = (b & ByteCountBitMask) >> ByteCountBitOffset;
			return l;
		}

		/// <summary>
		/// Expected to be the low bits in the byte.
		/// </summary>
		private const int ByteCountBitOffset = 0;
		private const int ByteCountBitMask = 0xf << ByteCountBitOffset;
		/// <summary>
		/// Expected to be to the left of ByteCount in the byte.
		/// </summary>
		private const int StringLengthBitOffset = 4;
		private const int StringLengthBitMask = 0xf << StringLengthBitOffset;

		public static unsafe string Utf8Decode(ulong long1, ulong long2, int lengthPos)
		{
			var chars = stackalloc char[16];
			var charcount = Utf8DecodeTo(long1, long2, lengthPos, chars, true);
			return new string(chars, 0, charcount);
		}

		public static unsafe int Utf8DecodeTo(ulong long1, ulong long2, int lengthPos, char[] dest, int index)
		{
			if (dest == null)
				throw new ArgumentNullException(nameof(dest));
			var length = GetLength_Utf16CodeUnits(long1, long2, lengthPos);
			if (index < 0 || index + length >= dest.Length)
				throw new ArgumentOutOfRangeException(nameof(index));

			fixed (char* chars = dest)
				return Utf8DecodeTo(long1, long2, lengthPos, chars + index, index + 16 < dest.Length);
		}

		private const bool CanUseIntrinsics = true;

		private static unsafe int Utf8DecodeTo(ulong long1, ulong long2, int lengthPos, char* chars, bool mayOverwrite16Chars)
		{
			const ulong highbits = 0x8080808080808080;
			bool is7bitAscii = (long1 & (highbits << 8)) == 0 && (long2 & highbits) == 0;
			var charcount1 = GetLength_Utf16CodeUnits(long1, long2, lengthPos);
			if (mayOverwrite16Chars && is7bitAscii && CanUseIntrinsics)
			{
				// all us-ascii. Use intrinsic.
				IntrinsicOps.AsciiToCharReplaced(long2, long1, (IntPtr)chars);
				return charcount1;
			}

			var bytes = stackalloc byte[16];
			var lp = (ulong*)bytes;
			lp[0] = long1;
			lp[1] = long2;
			byte bytecount = (byte)GetLength_Bytes(long1, long2, lengthPos);

			if (mayOverwrite16Chars && is7bitAscii)
			{
				// all us-ascii. Skip the checks.
				for (int i = 0; i < (bytecount & 0x7f); i += 2)
				{
					chars[i] = (char)bytes[15 - i];
					chars[i + 1] = (char)bytes[15 - (i + 1)];
				}
				if ((bytecount & 1) != 0)
					chars[bytecount - 1] = (char)bytes[15 - (bytecount - 1)];

				return bytecount;
			}

			int charcount = 0;
			for (int bi = 0; bi < bytecount; bi++)
			{
				byte thisbyte = bytes[15 - bi];
				if (thisbyte <= 127)
				{
					chars[charcount++] = (char)thisbyte;
				}
				else if (thisbyte >> 5 == 6)
				{
					// 110xxxxx 10xxxxxx
					var nextbyte = bytes[15 - ++bi];
					if (nextbyte >> 6 != 2)
						throw new ArgumentException("Invalid code unit.");
					var c = ((thisbyte & 0x1f) << 6) | (nextbyte & 0x3f);
					chars[charcount++] = (char)c;
				}
				else if (thisbyte >> 4 == 14)
				{
					// 1110xxxx 10xxxxxx 10xxxxxx
					var byte2 = bytes[15 - ++bi];
					var byte3 = bytes[15 - ++bi];
					if (byte2 >> 6 != 2 || byte3 >> 6 != 2)
						throw new ArgumentException("Invalid code unit.");
					var c = ((thisbyte & 0x1f) << 12) | ((byte2 & 0x3f) << 6) | (byte3 & 0x3f);
					chars[charcount++] = (char)c;
				}
				else
					throw new NotImplementedException("Four byte encodings are not implemented.");
			}
			return charcount;
		}

		public static unsafe bool Utf8EncodeToLongs(string s, out ulong long1, out ulong long2, bool throwIfTooLong)
		{
			fixed (char* cp = s)
			{
				const int maxLength = 15;
				var useOwn = true;
				int bytecount;
				byte lengthByte;
				if (CanUseIntrinsics && s.Length <= maxLength)
				{
					const uint mask = 0xff80ff80U;
					var ip = (uint*)cp;
					var is7bitAscii = (ip[0] & mask) == 0 && (ip[1] & mask) == 0 &&
									  (ip[2] & mask) == 0 && (ip[3] & mask) == 0 &&
									  (s.Length <= 8 || (ip[0] & mask) == 0 && (ip[1] & mask) == 0 &&
									   (ip[2] & mask) == 0 && (ip[3] & mask) == 0);
					if (is7bitAscii)
					{
						IntrinsicOps.CharToAsciiReplaced(cp, out long1, out long2);

						var charlength = s.Length;
						if (charlength < 8)
						{
							var toClear = (8 - charlength)*8;
							long2 = long2 >> toClear << toClear;
							long1 = 0;
						}
						else if (charlength < 15)
						{
							var toClear = (15 - charlength)*8;
							long1 = long1 >> toClear << toClear;
						}
						lengthByte = ComputeLengthByte(charlength, charlength);
						long1 = (long1 & ~0xffUL) | lengthByte;
						return true;
					}
				}

				var lbuf = stackalloc ulong[3];
				var buf = (byte*)lbuf;
				lbuf[0] = 0;
				lbuf[1] = 0;

				var bi = 0;
				for (int ci = 0; ci < s.Length; ci++)
				{
					var c = s[ci];
					if (c <= 0x7f)
					{
						buf[15 - bi] = (byte)c;
						bi++;
					}
					else if (c <= 0x7ff)
					{
						buf[15 - bi] = (byte)(0xc0 | (c >> 6));
						buf[15 - (bi + 1)] = (byte)(0x80 | (c & 0x3f));
						bi += 2;
					}
					else if (c <= 0xffff)
					{
						buf[15 - bi] = (byte)(0xe0 | (c >> 12));
						buf[15 - (bi + 1)] = (byte)(0x80 | ((c >> 6) & 0x3f));
						buf[15 - (bi + 2)] = (byte)(0x80 | (c & 0x3f));
						bi += 3;
					}
					else
						throw new NotImplementedException("This unicode code point range is not yet supported.");
					if (bi > maxLength)
					{
						if (throwIfTooLong)
							throw new ArgumentException("The utf-8 encoding of the string is too long.");
						long1 = 0;
						long2 = 0;
						return false;
					}
				}
				bytecount = bi;

				if (bytecount > maxLength)
				{
					if (throwIfTooLong)
						throw new ArgumentException("The utf-8 encoding of the string is too long.");
					long1 = 0;
					long2 = 0;
					return false;
				}

				lengthByte = ComputeLengthByte(s.Length, bytecount);

				buf[0] = lengthByte;
				long1 = lbuf[0];
				long2 = lbuf[1];

				return true;
			}
		}

		private static byte ComputeLengthByte(int charCount, int bytecount)
		{
			return (byte)((byte)(charCount << StringLengthBitOffset) | (byte)bytecount << ByteCountBitOffset);
		}
	}
}