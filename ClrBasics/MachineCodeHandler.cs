using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;

namespace ClrBasics
{
	/// <summary>
	/// This class sets up the machine code of static methods. Use <see cref="EnsurePrepared"/>.
	/// </summary>
	public static class MachineCodeHandler
	{
		internal static readonly TraceSource TraceSource = new TraceSource("ClrMachineCode");

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

		public static DBNull PrepareClass(Type type)
		{
			var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).
				Where(mi => mi.IsDefined(typeof(MachineCodeAttribute))).
				//Where(mi => mi.Name == nameof(IntrinsicOps.SwapBytes) && mi.ReturnType == typeof(uint)).
				// We need the CPUID methods to be prepared first because they are used to prepare the rest.
				OrderBy(mi => mi.Name.StartsWith("CPUID", StringComparison.OrdinalIgnoreCase) ? 0 : 1).
				ToList();

			foreach (var mi in methods)
			{
				RuntimeHelpers.PrepareMethod(mi.MethodHandle);

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

			return DBNull.Value;
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
			if (c1 >= 'A' && c1 <= 'F')
				return c1 - 'A' + 10;
			if (c1 >= 'a' && c1 <= 'f')
				return c1 - 'a' + 10;
			return null;
		}

		public static bool UseReplaced<TReturn>(Expression<Func<TReturn>> func)
		{
			var mce = func.Body as MethodCallExpression;
			if (mce == null)
				throw new ArgumentException("A " + nameof(MethodCallExpression) + " was expected. Got " + func.Body.GetType().Name + ".");

			var can = CanReplace(mce.Method);
			return can;
		}
		public static bool UseReplaced(Expression<Action> func)
		{
			var mce = func.Body as MethodCallExpression;
			if (mce == null)
				throw new ArgumentException("A " + nameof(MethodCallExpression) + " was expected. Got " + func.Body.GetType().Name + ".");

			var can = CanReplace(mce.Method);
			return can;
		}

		private static bool CanReplace(MethodInfo mi)
		{
			if (!IsEnvironmentSupported())
				return false;
			var ba = Environment.Is64BitProcess ? BaseArchitecture.x64 : BaseArchitecture.x86;

			var implementations = mi.GetCustomAttributes<MachineCodeAttribute>().ToList();
			var impl = GetImplementation(implementations, ba);
			return impl != null;
		}

		private static readonly DBNull CpuIDOps_Prepared = PrepareClass(typeof(CpuIDOps));
		private static readonly ArchitectureExtension ExtensionsPresent = GetArchitectureExtensionsPresent();

		[CanBeNull]
		static MachineCodeAttribute GetImplementation(List<MachineCodeAttribute> implementations, BaseArchitecture ba)
		{
			var impls = implementations
				.Where(mc => mc.BaseArchitecture == ba || (mc.BaseArchitecture == BaseArchitecture.x86 && ba == BaseArchitecture.x64))
				.ToList();
			if (impls.Count == 0)
				return null;

			if (impls.First().RequiredExtensions == ArchitectureExtension.None)
				return impls.First();

			return impls.FirstOrDefault(impl => (impl.RequiredExtensions & ExtensionsPresent) == impl.RequiredExtensions);
		}

		private static ArchitectureExtension GetArchitectureExtensionsPresent()
		{
			ArchitectureExtension extensionsPresent = ArchitectureExtension.None;

			var cpuId1Exc = CpuIDOps.CPUIDEcxReplaced(1);
			if (((cpuId1Exc >> (int) CPUID1FeatureBitsEcx.PopCnt) & 1) == 1)
				extensionsPresent |= ArchitectureExtension.PopCnt;
			if (((cpuId1Exc >> (int) CPUID1FeatureBitsEcx.Sse41) & 1) == 1)
				extensionsPresent |= ArchitectureExtension.Sse41;
			if (((cpuId1Exc >> (int) CPUID1FeatureBitsEcx.Sse42) & 1) == 1)
				extensionsPresent |= ArchitectureExtension.Sse42;
			if (((CpuIDOps.CPUIDEcxReplaced(0x80000001) >> (int) CPUIDXFeatureBitsEcx.Lzcnt_80000001h) & 1) == 1)
				extensionsPresent |= ArchitectureExtension.Lzcnt;
			return extensionsPresent;
		}

		private static bool IsEnvironmentSupported()
		{
			return Environment.OSVersion.Platform == PlatformID.Win32NT;
		}

		enum CPUID1FeatureBitsEcx
		{
			PopCnt = 23,
			Sse41 = 19,
			Sse42 = 20,
			AesNi = 25,
			Avx = 28,
		}

		enum CPUIDXFeatureBitsEcx
		{
			Lzcnt_80000001h = 5,
		}

		enum CPUIDFeatureBitsEdx
		{
			CLfsh = 19,
			Sse = 25,
			Sse2 = 26,
		}
	}
}
