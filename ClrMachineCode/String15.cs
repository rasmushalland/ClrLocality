using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
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
	/// Value type string able to contain up to 15 utf-8 code units.
	/// </summary>
	public struct String15 : IEquatable<String15>, IComparable<String15>, IStringContentsUnsafe
	{
		private const int LengthPos = 0;
		/// <summary>
		/// Contains end of string and a length byte.
		/// </summary>
		public ulong _long1;
		/// <summary>
		/// Contains start of the string.
		/// </summary>
		public ulong _long2;

		public String15(string s)
		{
			_long1 = 0;
			_long2 = 0;

			UnsafeStringUtility.Utf8EncodeToLongs(s, out _long1, out _long2, true);
		}

		public override string ToString()
			=> UnsafeStringUtility.Utf8Decode(_long1, _long2, LengthPos);

		public int CopyTo(char[] buf, int index)
			=> UnsafeStringUtility.Utf8DecodeTo(_long1, _long2, LengthPos, buf, index);

		public int Length => UnsafeStringUtility.GetLength_Utf16CodeUnits(_long1, _long2, LengthPos);

		public unsafe void GetContents(char* shortDestBuffer, int shortDestBufferSize, out int shortLength, out string longDest)
		{
			longDest = null;
			shortLength = UnsafeStringUtility.GetContents(shortDestBuffer, shortDestBufferSize, _long1, _long2, LengthPos);
		}

		public int CompareTo(String15 other)
		{
			var d1 = (long) (_long2 >> 8) - (long) (other._long2 >> 8);
			if (d1 != 0)
				return Math.Sign(d1);

			var d2 = (long) (_long2 << 48 | _long1 >> 16) - (long) (other._long2 << 48 | other._long1 >> 16);
			if (d2 != 0)
				return Math.Sign(d2);

			var d3 = (long) (_long1 >> 8) - (long) (other._long1 >> 8);
			if (d3 != 0)
				return Math.Sign(d3);

			return 0;
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
	/// Value type string able to contain up to 15 utf-8 code units "inline". Longer strings are kept.
	/// 
	/// Not ready for production: Needs additional testing.
	/// </summary>
	internal struct String15Ex : IStringContentsUnsafe, IEquatable<String15Ex>
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
			=> _string ?? UnsafeStringUtility.Utf8Decode(_long1, _long2, LengthPos);

		public int Length
			=> _string?.Length ?? UnsafeStringUtility.GetLength_Utf16CodeUnits((ulong)_long1, (ulong)_long2, LengthPos);

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

		public bool Equals(String15Ex other)
			=> _long1 == other._long1 && _long2 == other._long2 && string.Equals(_string, other._string);

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
}