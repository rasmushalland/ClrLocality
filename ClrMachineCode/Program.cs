using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ClrMachineCode
{
	internal class Program
	{
		/// <summary>
		/// mov eax, ecx
		/// add eax, 5
		/// ret
		/// </summary>
		private static byte[] code = {0x89, 0xC8, 0x83, 0xC0, 0x05, 0xC3};

		private static void Main(string[] args)
		{
			if (false)
			{
				var val = MyFunc(23);
				var val2 = MyFunc(23);
				Console.WriteLine(val);
			}
			else
			{
				var newFunctionAddress = VirtualAlloc(IntPtr.Zero, new IntPtr(8192), MEM_COMMIT | MEM_RESERVE,
					PAGE_EXECUTE_READWRITE);

				if (newFunctionAddress == IntPtr.Zero)
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
				Marshal.Copy(code, 0, newFunctionAddress, code.Length);

				var del = (Mydel)Marshal.GetDelegateForFunctionPointer(newFunctionAddress, typeof (Mydel));
				Console.WriteLine();
				var rv = del(12);
				Console.WriteLine();
				Console.WriteLine(rv);
			}
		}
		internal delegate int Mydel(int arg);


		[MethodImpl(MethodImplOptions.NoInlining)]
		private static int MyFunc(int arg) => arg + 5;

		#region Native Interop

		private const uint MEM_COMMIT = 0x1000;

		private const uint MEM_RESERVE = 0x2000;

		private const uint MEM_RELEASE = 0x8000;

		private const uint PAGE_EXECUTE_READWRITE = 0x40;

		[DllImport("kernel32", SetLastError = true)]
		private static extern IntPtr VirtualAlloc(IntPtr startAddress, IntPtr size, uint allocationType, uint protectionType);

		[DllImport("kernel32", SetLastError = true)]
		private static extern IntPtr VirtualFree(IntPtr address, IntPtr size, uint freeType);

		#endregion
	}
}