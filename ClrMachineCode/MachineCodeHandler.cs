using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClrMachineCode
{
	/// <summary>
	/// This class sets up the machine code of static methods. Use <see cref="PrepareClass"/>.
	/// </summary>
	static class MachineCodeHandler
	{
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

		public static void PrepareClass(Type type)
		{
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).
				Where(mi => mi.IsDefined(typeof(MachineCodeAttribute))).
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
					if (e.InnerException is NotImplementedException)
					{
						// ok, assume it simple means no fallback.
					}
					else
						throw new ApplicationException("Error while invoking method " + mi.Name + ": " + e.InnerException.Message, e);
				}

				// now overwrite it.
				var ia = mi.GetCustomAttribute<MachineCodeAttribute>();
				foreach (var ch in ia.MachineCode)
				{
					if ((ch & 0xff00) != 0)
						throw new ArgumentException("All characters of MachineCode string must be single byte.");
				}

				var machinecode = ia.MachineCode.Select(ch => (byte)ch).ToArray();
				var fp = mi.MethodHandle.GetFunctionPointer();
				Marshal.Copy(machinecode, 0, fp, machinecode.Length);
			}
		}
	}
}
