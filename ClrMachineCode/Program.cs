using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace ClrMachineCode
{
	[SuppressUnmanagedCodeSecurity]
	internal delegate int Int32Func(int arg);
	[SuppressUnmanagedCodeSecurity]
	internal delegate int Int64Func(ulong arg);

	class PopCntTest
	{
		const ulong m1 = 0x5555555555555555; //binary: 0101...
		const ulong m2 = 0x3333333333333333; //binary: 00110011..
		const ulong m4 = 0x0f0f0f0f0f0f0f0f; //binary:  4 zeros,  4 ones ...
		const ulong m8 = 0x00ff00ff00ff00ff; //binary:  8 zeros,  8 ones ...
		const ulong m16 = 0x0000ffff0000ffff; //binary: 16 zeros, 16 ones ...
		const ulong m32 = 0x00000000ffffffff; //binary: 32 zeros, 32 ones
		const ulong hff = 0xffffffffffffffff; //binary: all ones
		const ulong h01 = 0x0101010101010101; //the sum of 256 to the power of 0,1,2,3...

		//This uses fewer arithmetic operations than any other known  
		//implementation on machines with slow multiplication.
		//It uses 17 arithmetic operations.
		static int popcount_2(ulong x)
		{
			x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
			x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
			x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits 
			x += x >> 8;  //put count of each 16 bits into their lowest 8 bits
			x += x >> 16;  //put count of each 32 bits into their lowest 8 bits
			x += x >> 32;  //put count of each 64 bits into their lowest 8 bits
			return (int)(x & 0x7f);
		}

		//This uses fewer arithmetic operations than any other known  
		//implementation on machines with fast multiplication.
		//It uses 12 arithmetic operations, one of which is a multiply.
		static int popcount_3(ulong x)
		{
			x -= (x >> 1) & m1;             //put count of each 2 bits into those 2 bits
			x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
			x = (x + (x >> 4)) & m4;        //put count of each 8 bits into those 8 bits 
			return (int)((x * h01) >> 56);  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
		}

		static public void Test()
		{


			long defaultCnt = 1000 * 1000;

			{
				var nativePopCnt = Program.CreateInt32Func(Program.code_popCnt32);
				nativePopCnt(12);
				nativePopCnt(12);
				var cnt = defaultCnt * 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += nativePopCnt(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt32-native: {elapsed / defaultCnt} cycles/iter.");
			}
			{
				var nativePopCnt = Program.CreateInt64Func(Program.code_popCnt64);
				nativePopCnt(12);
				var cnt = defaultCnt;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += nativePopCnt(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64-native: {elapsed / defaultCnt} cycles/iter.");
			}
			{
				popcount_2(12);
				var sideeffect = 0L;
				var cnt = defaultCnt;

				var sw = ThreadCycleStopWatch.StartNew();
				for (long i = 0; i < cnt; i++)
					sideeffect += popcount_2(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64 2: {elapsed / defaultCnt} cycles/iter.");
			}
			{
				popcount_3(12);
				var sideeffect = 0L;
				var cnt = defaultCnt;

				var sw = ThreadCycleStopWatch.StartNew();
				for (long i = 0; i < cnt; i++)
					sideeffect += popcount_3(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64 3: {elapsed / defaultCnt} cycles/iter.");
			}
			{
				var del = new Int64Func(popcount_3);
				del(12);
				var sideeffect = 0L;
				var cnt = defaultCnt;

				var sw = ThreadCycleStopWatch.StartNew();
				for (long i = 0; i < cnt; i++)
					sideeffect += del(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64 3, delegate: {elapsed / defaultCnt} cycles/iter.");
			}


			Console.ReadLine();
		}

		private static void AssertSideeffect(long sideeffect, long cnt)
		{
			//Console.WriteLine(sideeffect);
			Trace.Assert(sideeffect == cnt * 2);
		}
	}

	internal class Program
	{
		/// <summary>
		/// mov eax, ecx
		/// add eax, 5
		/// ret
		/// 
		/// 
		/// 
		/// https://defuse.ca/online-x86-assembler.htm#disassembly
		/// </summary>
		private static readonly byte[] code_addFive = { 0x89, 0xC8, 0x83, 0xC0, 0x05, 0xC3 };

		/// <summary>
		/// popcnt eax, ecx
		/// ret
		/// </summary>
		public static readonly byte[] code_popCnt32 = { 0xF3, 0x0F, 0xB8, 0xC1, 0xC3 };
		/// <summary>
		/// popcnt rax, rcx
		/// ret
		/// </summary>
		public static readonly byte[] code_popCnt64 = { 0xF3, 0x48, 0x0F, 0xB8, 0xC1, 0xC3 };

		private static void Main(string[] args)
		{
			if (1 == 2)
			{
				var val = MyFunc(23);
				var val2 = MyFunc(23);
				Console.WriteLine(val);
			}
			else if (1 == 2)
			{
				var codebytes = code_popCnt32;
				var del = CreateInt32Func(codebytes);
				Console.WriteLine();
				var rv = del(12);
				Console.WriteLine(rv);
			}
			else if (1 == 2)
			{
				var del = CreateInt64Func(code_popCnt64);
				Console.WriteLine();
				var rv = del(12);
				Console.WriteLine(rv);
			}
			else if (1 == 1)
			{
				PopCntTest.Test();
			}
		}

		public static Int32Func CreateInt32Func(byte[] codebytes)
		{
			var newFunctionAddress = VirtualAlloc(IntPtr.Zero, new IntPtr(100), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

			if (newFunctionAddress == IntPtr.Zero)
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			Marshal.Copy(codebytes, 0, newFunctionAddress, codebytes.Length);

			var del = (Int32Func)Marshal.GetDelegateForFunctionPointer(newFunctionAddress, typeof(Int32Func));
			return del;
		}
		public static Int64Func CreateInt64Func(byte[] codebytes)
		{
			var newFunctionAddress = VirtualAlloc(IntPtr.Zero, new IntPtr(100), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

			if (newFunctionAddress == IntPtr.Zero)
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			Marshal.Copy(codebytes, 0, newFunctionAddress, codebytes.Length);

			var del = (Int64Func)Marshal.GetDelegateForFunctionPointer(newFunctionAddress, typeof(Int64Func));
			return del;
		}


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