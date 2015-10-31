using System;
using System.Linq;
using System.Text;

namespace ClrMachineCode
{
	public interface IStringContentsUnsafe
	{
		/// <summary>
		/// Kopier indhold til <paramref name="shortDestBuffer"/>, hvis der er plads, og indholdet ikke i forvejen ligger i en <see cref="String"/>.
		/// Hvis det gør, lægges denne streng istedet i <paramref name="longDest"/>.
		/// </summary>
		unsafe void GetContents(char* shortDestBuffer, int shortDestBufferSize, out int shortLength, out string longDest);
	}

	/// <summary>
	/// Tekststreng, der kan holde op til 15 utf-8 code units.
	/// </summary>
	/// <remarks>
	/// tostring: PUNPCKHBW ? http://x86.renejeschke.de/html/file_module_x86_id_267.html
	/// fromstring: PSHUFB ? http://www.tptp.cc/mirrors/siyobik.info/instruction/PSHUFB.html
	/// derudover PINSRD m.fl..
	/// </remarks>
	public struct String15 : IEquatable<String15>, IComparable<String15>, IStringContentsUnsafe
	{
		private const int LengthPos = 0;
		/// <summary>
		/// Har slut af teksstreng og laengdebyte.
		/// </summary>
		private ulong _long1;
		/// <summary>
		/// Har start af teksstreng.
		/// </summary>
		private readonly ulong _long2;

		public String15(string s)
		{
			_long1 = 0;
			_long2 = 0;

			UnsafeStringUtility.Utf8EncodeToLongs(s, out _long1, out _long2, true);
		}

		public override string ToString() => UnsafeStringUtility.Utf8Decode(_long1, _long2, LengthPos);

		public int Length => UnsafeStringUtility.GetLength_Utf16CodeUnits((ulong)_long1, (ulong)_long2, LengthPos);

		public unsafe void GetContents(char* shortDestBuffer, int shortDestBufferSize, out int shortLength, out string longDest)
		{
			longDest = null;
			shortLength = UnsafeStringUtility.GetContents(shortDestBuffer, shortDestBufferSize, _long1, _long2, LengthPos);
		}

		public int CompareTo(String15 other)
		{

			{
				var d1 = (uint) (_long2 >> 32) - (uint) (other._long2 >> 32);
				if (d1 != 0)
					return (int) d1;

				var d2 = (uint) _long2 - (uint) other._long2;
				if (d2 != 0)
					return (int) d2;

				var d3 = (uint) (_long1 >> 32) - (uint) (other._long1 >> 32);
				if (d3 != 0)
					return (int) d3;

				var d4 = (uint) _long1 - (uint) other._long1;
				return (int) d4;

			}
			//{
			//	var diff1 = _long2 - other._long2;
			//	if (diff1 != 0)
			//		return diff1 >> 32 != 0 ? (int) (diff1 >> 32) : (int) diff1;
			//	var diff2 = _long1 - other._long1;
			//	return diff2 >> 32 != 0 ? (int) (diff2 >> 32) : (int) diff2;
			//}
		}

		#region Equality

		public bool Equals(String15 other) => _long1 == other._long1 && _long2 == other._long2;

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is String15 && Equals((String15) obj);
		}

		public override int GetHashCode() => unchecked((_long1.GetHashCode()*397) ^ _long2.GetHashCode());

		public static bool operator ==(String15 left, String15 right) => left.Equals(right);

		public static bool operator !=(String15 left, String15 right) => !left.Equals(right);

		#endregion
	}

	/// <summary>
	/// Tekststreng, der kan holde op til 15 utf-8 code units inline. Længere strenge beholdes.
	/// </summary>
	public struct String15Ex : IStringContentsUnsafe, IEquatable<String15Ex>
	{
		private const int LengthPos = 0;
		private ulong _long1;
		private readonly ulong _long2;
		private readonly string _string;

		public String15Ex(string s)
		{
			_long1 = 0;
			_long2 = 0;
			if (s.Length > 15)
			{
				_string = s;
				return;
			}

			if (UnsafeStringUtility.Utf8EncodeToLongs(s, out _long1, out _long2, false))
			{
				_string = null;
				return;
			}
			_long1 = 0;
			_long2 = 0;
			_string = s;
		}

		public override string ToString()
		{
			if (_string != null)
				return _string;
			return UnsafeStringUtility.Utf8Decode(_long1, _long2, LengthPos);
		}

		public int Length
		{
			get
			{
				if (_string != null)
					return _string.Length;
				return UnsafeStringUtility.GetLength_Utf16CodeUnits((ulong)_long1, (ulong)_long2, LengthPos);
			}
		}

		public unsafe void GetContents(char* shortDestBuffer, int shortDestBufferSize, out int shortLength, out string longDest)
		{
			if (_string != null)
			{
				shortLength = 0;
				longDest = _string;
				return;
			}
			longDest = null;
			shortLength = UnsafeStringUtility.GetContents(shortDestBuffer, shortDestBufferSize, _long1, _long2, LengthPos);
		}

		#region Equality

		public bool Equals(String15Ex other) => _long1 == other._long1 && _long2 == other._long2 && string.Equals(_string, other._string);

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			return obj is String15Ex && Equals((String15Ex) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = _long1.GetHashCode();
				hashCode = (hashCode*397) ^ _long2.GetHashCode();
				hashCode = (hashCode*397) ^ (_string != null ? _string.GetHashCode() : 0);
				return hashCode;
			}
		}

		public static bool operator ==(String15Ex left, String15Ex right) => left.Equals(right);

		public static bool operator !=(String15Ex left, String15Ex right) => !left.Equals(right);

		#endregion
	}

	static class UnsafeStringUtility
	{
		public unsafe static int GetContents(char* shortDestBuffer, int shortDestBufferSize, ulong long1, ulong long2, int lengthPos)
		{
			var bytecount = GetLength_Bytes(long1, long2, lengthPos);
			var buf1 = stackalloc byte[16];
			var lbuf1 = (ulong*) buf1;
			lbuf1[0] = long1;
			lbuf1[1] = long2;

			var buf2 = stackalloc byte[16];
			for (int i = 0; i < bytecount; i++)
				buf2[i] = buf1[15 - i];
			// Burde nok tjekke her, om der er plads nok.
			return Encoding.UTF8.GetChars(buf2, bytecount, shortDestBuffer, shortDestBufferSize);
		}

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
		/// Forventes at staa til hoejre i byten.
		/// </summary>
		private const int ByteCountBitOffset = 0;
		private const int ByteCountBitMask = 0xf << ByteCountBitOffset;
		/// <summary>
		/// Forventes at staa til venstre for ByteCount i byten.
		/// </summary>
		private const int StringLengthBitOffset = 4;
		private const int StringLengthBitMask = 0xf << StringLengthBitOffset;

		public static unsafe string Utf8Decode(ulong long1, ulong long2, int lengthPos)
		{
			var bytes = stackalloc byte[16];
			var lp = (ulong*)bytes;
			lp[0] = long1;
			lp[1] = long2;
			var rv = new string('\0', 15);

			fixed (char* chars = rv)
			{
				byte bytecount = (byte) ((bytes[lengthPos] & ByteCountBitMask) >> ByteCountBitOffset);
				int charcount = 0;
				for (int bi = 0; bi < bytecount; bi++)
				{
					byte thisbyte = bytes[15 - bi];
					if (thisbyte <= 127)
					{
						chars[charcount++] = (char)thisbyte;
					}
					else if ((thisbyte >> 5) == 6)
					{
						byte nextbyte = bytes[15 - ++bi];
						if (((nextbyte >> 6) != 2))
							throw new ArgumentException("Ugyldig code unit.");
						int c = ((thisbyte & 0x1f) << 6) | (nextbyte & 0x3f);
						chars[charcount++] = (char)c;
					}
					else
						throw new NotImplementedException("hold op.");
				}
				SetLength(rv, charcount);
				return rv;
			}
		}

		private static unsafe void SetLength(string s, int length)
		{
			fixed (char* p = s)
			{
				var pi = (int*)p;
				if (length < 0 || length > pi[-2])
					throw (new ArgumentOutOfRangeException(nameof(length)));
				pi[-1] = length;
				p[length] = '\0';
			}
		}



		public static unsafe bool Utf8EncodeToLongs(string s, out ulong long1, out ulong long2, bool throwIfTooLong)
		{
			var buf = stackalloc byte[17];
			var lbuf = (long*)buf;
			lbuf[0] = 0;
			lbuf[1] = 0;
			int bytecount;
			fixed (char* cp = s)
			{
				bytecount = Encoding.UTF8.GetBytes(cp, s.Length, buf, 17);
				const int maxLength = 15;
				if (bytecount > maxLength)
				{
					if (throwIfTooLong)
						throw new ArgumentException("Strengens utf-8-repræsentation er for lang.");
					long1 = 0;
					long2 = 0;
					return false;
				}
			}

			// Vend byteraekkefoelge om, da vi er little endian og gerne vil kunne ordne effektivt.
			var buf2 = stackalloc byte[17];
			var lbuf2 = (ulong*)buf2;
			lbuf2[0] = 0;
			lbuf2[1] = 0;
			for (int i = 0; i < bytecount; i++)
				buf2[15 - i] = buf[i];
			buf2[0] = (byte) ((byte)(s.Length << StringLengthBitOffset) | (byte) bytecount << ByteCountBitOffset);


			long1 = lbuf2[0];
			long2 = lbuf2[1];

			return true;
		}
	}
}