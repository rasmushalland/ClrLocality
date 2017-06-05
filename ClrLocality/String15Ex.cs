using System;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace ClrLocality
{
	/// <summary>
	/// Value type string able to contain up to 15 utf-8 code units "inline". Longer strings are kept.
	/// 
	/// Not ready for production: Needs additional testing.
	/// </summary>
	[Serializable]
	public struct String15Ex : IEquatable<String15Ex>, IComparable<String15Ex>, IStringContentsUnsafe, IXmlSerializable, ISerializable
	{
		private const int LengthPos = 0;
		private ulong _long1;
		private ulong _long2;
		private string _string;

		public String15Ex(string s)
		{
			_long1 = 0;
			_long2 = 0;
			_string = null;
			InitializeFrom(s);
		}

		private void InitializeFrom(string s)
		{
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

		static public explicit operator String15Ex(string str)
			=> new String15Ex(str);

		public override string ToString()
			=> _string ?? UnsafeStringUtility.Utf8Decode(_long1, _long2, LengthPos);

		public int Length
			=> _string?.Length ?? UnsafeStringUtility.GetLength_Utf16CodeUnits(_long1, _long2, LengthPos);

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
			return obj is String15Ex && Equals((String15Ex)obj);
		}

		public static bool operator ==(String15Ex left, String15Ex right) => left.Equals(right);

		public static bool operator !=(String15Ex left, String15Ex right) => !left.Equals(right);

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = _long1.GetHashCode();
				hashCode = (hashCode * 397) ^ _long2.GetHashCode();
				hashCode = (hashCode * 397) ^ (_string != null ? _string.GetHashCode() : 0);
				return hashCode;
			}
		}

		public int CompareTo(String15Ex other)
		{
			if (this._string == null && other._string == null)
			{
				var d1 = (long)(_long2 >> 8) - (long)(other._long2 >> 8);
				if (d1 != 0)
					return Math.Sign(d1);

				var d2 = (long)(_long2 << 48 | _long1 >> 16) - (long)(other._long2 << 48 | other._long1 >> 16);
				if (d2 != 0)
					return Math.Sign(d2);

				var d3 = (long)(_long1 >> 8) - (long)(other._long1 >> 8);
				if (d3 != 0)
					return Math.Sign(d3);

				return 0;
			}

			var stringthis = ToString();
			var stringother = other.ToString();

			return StringComparer.Ordinal.Compare(stringthis, stringother);
		}

		#endregion

		#region Serialization

		public void GetObjectData(SerializationInfo info, StreamingContext context)
			=> info.AddValue("s", ToString());

		public String15Ex(SerializationInfo info, StreamingContext ctx) : this(info.GetString("s"))
		{
		}


		public XmlSchema GetSchema() => null;

		public void ReadXml(XmlReader reader) => InitializeFrom(reader.ReadElementContentAsString());

		public void WriteXml(XmlWriter writer) => writer.WriteString(ToString());

		#endregion
	}
}