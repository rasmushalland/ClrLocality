using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ClrBasics
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
	[Serializable]
	public struct String15 : IEquatable<String15>, IComparable<String15>, IStringContentsUnsafe, IXmlSerializable, ISerializable
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

		static public explicit operator String15(string str)
			=> new String15(str);

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

		#region IXmlSerializable

		public XmlSchema GetSchema() => null;

		public void ReadXml(XmlReader reader)
		{
			var s = reader.ReadElementString();
			UnsafeStringUtility.Utf8EncodeToLongs(s, out _long1, out _long2, true);
		}

		public void WriteXml(XmlWriter writer)
			=> writer.WriteString(ToString());

		#endregion

		public String15(SerializationInfo info, StreamingContext ctx) : this(info.GetString("s")) { }

		public void GetObjectData(SerializationInfo info, StreamingContext context)
			=> info.AddValue("s", ToString());
	}
}