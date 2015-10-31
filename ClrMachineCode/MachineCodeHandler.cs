using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClrMachineCode
{
	/// <summary>
	/// This class sets up the machine code of static methods. Use <see cref="EnsurePrepared"/>.
	/// </summary>
	public static class MachineCodeHandler
	{
		internal static readonly TraceSource TraceSource = new TraceSource("ClrMachineCode");

		static readonly Dictionary<TypeCode, object> DefaultParameterValues = new Dictionary<TypeCode, object> {
			{TypeCode.SByte, default(sbyte) },
			{TypeCode.Byte, default(byte) },
			{TypeCode.Int16, default(short) },
			{TypeCode.UInt16, default(ushort) },
			{TypeCode.Int32, default(int) },
			{TypeCode.UInt32, default(uint) },
			{TypeCode.Int64, default(long) },
			{TypeCode.UInt64, default(ulong) },
			{TypeCode.Single, default(float) },
			{TypeCode.Double, default(double) },
			{TypeCode.Boolean, default(int) },
		};

		private static readonly ConcurrentDictionary<Type, DBNull> PreparedTypes = new ConcurrentDictionary<Type, DBNull>(); 

		public static void EnsurePrepared(Type type)
		{
			DBNull @null;
			if (PreparedTypes.TryGetValue(type, out @null))
				return;

			lock (PreparedTypes)
			{
				PrepareClass(type);
				PreparedTypes.GetOrAdd(type, DBNull.Value);
			}
		}

		public static void PrepareClass(Type type)
		{
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).
				Where(mi => mi.IsDefined(typeof(MachineCodeAttribute))).
				//Where(mi => mi.Name == nameof(IntrinsicOps.SwapBytes) && mi.ReturnType == typeof(uint)).
				ToList();

			foreach (var mi in methods)
			{
				// Call the method, so it gets jit'ed to somewhere we can overwrite.
				var args = mi.GetParameters()
					.Select(pi => {
						object val;
						if (!DefaultParameterValues.TryGetValue(Type.GetTypeCode(pi.ParameterType), out val))
							throw new NotSupportedException("Argument of type " + pi.ParameterType.FullName + " is not supported.");
						return val;
					})
					.ToList();
				try
				{
					mi.Invoke(null, args.ToArray());
				}
				catch (TargetInvocationException e)
				{
					if (e.InnerException is InvalidOperationException)
					{
						// ok, assume it simple means no fallback.
					}
					else
						throw new ApplicationException("Error while invoking method " + mi.Name + ": " + e.InnerException.Message, e);
				}

				// now overwrite it.
				var ia = mi.GetCustomAttribute<MachineCodeAttribute>();
				if (string.IsNullOrEmpty(ia.MachineCode))
					throw new ArgumentException("Machine code is empty string.");
				var machinecode = HexDecode(ia.MachineCode);
				var fp = mi.MethodHandle.GetFunctionPointer();
				if (fp == IntPtr.Zero)
					throw new ApplicationException("Could not get function pointer for " + mi.Name + " - null was returned.");
				TraceSource.TraceInformation("Replacing method {0} at address {1:X}.", mi, fp);
				TraceSource.Flush();
				Marshal.Copy(machinecode, 0, fp, machinecode.Length);
			}
		}

		private static byte[] HexDecode(string hex)
		{
			if ((hex.Length % 2) != 0)
				throw new ArgumentException("Invalid machine code: Length must be multiple of two.");

			var bytes = new byte[hex.Length/2];
			for (int i = 0; i < hex.Length; i += 2)
			{
				var v1 = GetHexValue(hex[i]);
				var v2 = GetHexValue(hex[i + 1]);
				if (v1 == null || v2 == null)
					throw new ArgumentException("Invalid hex characters.");
				bytes[i/2] = (byte) (v1.Value << 4 | v2.Value);
			}
			return bytes;
		}

		private static int? GetHexValue(char c1)
		{
			if (c1 >= '0' && c1 <= '9')
				return c1 - '0';
			else if (c1 >= 'A' && c1 <= 'F')
				return c1 - 'A' + 10;
			else if (c1 >= 'a' && c1 <= 'f')
				return c1 - 'a' + 10;
			return null;
		}
	}
}
