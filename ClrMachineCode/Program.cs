using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace ClrMachineCode
{
	internal class Program
	{
		/// <summary>
		/// mov eax, ecx
		/// add eax, 5
		/// ret
		/// </summary>
		private static readonly byte[] code_addFive = {0x89, 0xC8, 0x83, 0xC0, 0x05, 0xC3};

		/// <summary>
		/// popcnt eax, ecx
		/// ret
		/// </summary>
		private static readonly byte[] code_popCnt = { 0xF3, 0x0F, 0xB8, 0xC1, 0xC3 };

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
				var codebytes = code_popCnt;
				//var codebytes = code_addFive;


				var newFunctionAddress = VirtualAlloc(IntPtr.Zero, new IntPtr(8192), MEM_COMMIT | MEM_RESERVE,
					PAGE_EXECUTE_READWRITE);

				if (newFunctionAddress == IntPtr.Zero)
					Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
				Marshal.Copy(codebytes, 0, newFunctionAddress, codebytes.Length);

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