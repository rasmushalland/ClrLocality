using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace ClrMachineCode
{
	/// <summary>
	/// SuppressUnmanagedCodeSecurity gør ca dobbelt så hurtig for native. Ender på 70-80 cycler - managed 3 er ca 15-20.
	/// 
	/// UnmanagedFunctionPointer giver ikke noget.
	/// </summary>
	/// <returns></returns>
	[SuppressUnmanagedCodeSecurity]
	internal delegate int Int32Func(uint arg);

	[SuppressUnmanagedCodeSecurity]
	internal delegate int Int64Func(ulong arg);

	[SuppressUnmanagedCodeSecurity]
	internal delegate ulong UInt64Func(ulong arg);


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
		static int popcount_3_32(uint x)
		{
			x -= (x >> 1) & 0x55555555;             //put count of each 2 bits into those 2 bits
			x = (x & 0x33333333) + ((x >> 2) & 0x33333333); //put count of each 4 bits into those 4 bits 
			x = (x + (x >> 4)) & 0x0f0f0f0f;        //put count of each 8 bits into those 8 bits 
			return (int)((x * 0x01010101) >> 24);  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
		}

		static public void Test()
		{
			Program.ReturnArgument("hej her");


			{
				// Erstat maskinkoden af PopCnt32Dummy med popcnt.
				Program.PopCnt32Dummy(123);
				var popCnt32Dummy = ((Func<int, int>) Program.PopCnt32Dummy).Method.MethodHandle.GetFunctionPointer();
				Marshal.Copy(Program.code_popCnt32, 0, popCnt32Dummy, Program.code_popCnt32.Length);
			}
			{
				// Erstat maskinkoden af PopCnt64Dummy med popcnt.
				Program.PopCnt64Dummy(123);
				var popCnt64Dummy = ((Func<ulong, int>) Program.PopCnt64Dummy).Method.MethodHandle.GetFunctionPointer();
				Marshal.Copy(Program.code_popCnt64, 0, popCnt64Dummy, Program.code_popCnt64.Length);
			}


			const long defaultCnt = 1000 * 1000;

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
				Console.WriteLine($"Elapsed, popcnt32-native: {elapsed / cnt} cycles/iter.");
			}
			{
				var nativePopCnt = Program.CreateInt32Func(Program.code_popCnt32);
				nativePopCnt(12);
				nativePopCnt(12);
				var cnt = defaultCnt * 1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += Program.PopCnt32Dummy(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt32-native, replaced: {elapsed / cnt} cycles/iter.");
			}
			{
				var nativePopCnt = Program.CreateInt64Func(Program.code_popCnt64);
				nativePopCnt(12);
				var cnt = defaultCnt<<1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += Program.ReturnArgument(nativePopCnt(12));
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64-native: {elapsed / cnt} cycles/iter.");
			}
			{
				var nativePopCnt = Program.CreateInt64Func(Program.code_popCnt64);
				nativePopCnt(12);
				var cnt = defaultCnt<<1;

				var sw = ThreadCycleStopWatch.StartNew();
				var sideeffect = 0L;
				for (long i = 0; i < cnt; i++)
					sideeffect += Program.PopCnt64Dummy(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64-native, replaced: {elapsed / cnt} cycles/iter.");
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
				Console.WriteLine($"Elapsed, popcnt64 2: {elapsed / cnt} cycles/iter.");
			}
			{
				popcount_3(12);
				var sideeffect = 0L;
				var cnt = defaultCnt << 1;

				var sw = ThreadCycleStopWatch.StartNew();
				for (long i = 0; i < cnt; i++)
					sideeffect += popcount_3(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt64 3: {elapsed / cnt} cycles/iter.");
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
				Console.WriteLine($"Elapsed, popcnt64 3, delegate: {elapsed / cnt} cycles/iter.");
			}
			{
				var del = new Int32Func(popcount_3_32);
				del(12);
				var sideeffect = 0L;
				var cnt = defaultCnt;

				var sw = ThreadCycleStopWatch.StartNew();
				for (long i = 0; i < cnt; i++)
					sideeffect += del(12);
				var elapsed = sw.GetCurrentCycles();
				AssertSideeffect(sideeffect, cnt);
				Console.WriteLine($"Elapsed, popcnt32 3, delegate: {elapsed / cnt} cycles/iter.");
			}
			//Console.WriteLine("returnargptr: " + popCntDummy.ToString("X"));

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
		/// <summary>
		/// bswap ecx
		/// mov eax, bcx
		/// ret
		/// </summary>
		public static readonly byte[] code_bswap32 = { 0x0F, 0xC9, 0x89, 0xC8, 0xC3 };
		/// <summary>
		/// mov rax, rcx ; flyt arg til rax, og vend bytes i ecx
		/// bswap ecx
		/// shl rcx, 0x20
		/// 
		/// shr rax, 0x20
		/// bswap eax
		/// add rax, rcx
		/// ret
		/// </summary>
		public static readonly byte[] code_bswap64 = { 0x48, 0x89, 0xC8, 0x0F, 0xC9, 0x48, 0xC1, 0xE1, 0x20, 0x48, 0xC1, 0xE8, 0x20, 0x0F, 0xC8, 0x48, 0x01, 0xC8, 0xC3 };
		/// <summary>
		/// mov edx,0
		/// call qword ptr[edx]
		/// </summary>
		public static readonly byte[] code_callNullPointer = { 0xBA, 0x00, 0x00, 0x00, 0x00, 0x67, 0xFF, 0x12 };
		/// <summary>
		/// mov edx,0
		/// call qword ptr[edx]
		/// </summary>
		public static readonly byte[] code_getCallerCallerAddress = { 0xBA, 0x00, 0x00, 0x00, 0x00, 0x67, 0xFF, 0x12 };

		private static void Main(string[] args)
		{
			Console.WriteLine(long.Parse("2") + 5);
			if (1 == 2)
			{
				var val = AddFive(23);
				var val2 = AddFive(23);
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
				Console.ReadLine();
				var del = CreateInt32Func(code_callNullPointer);
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
			else if (1 == 2)
			{
				var del = CreateInt32Func(code_bswap32);
				Console.WriteLine();
				var rv = del(0x01020304);
				Trace.Assert(rv == 0x04030201);
				Console.WriteLine(rv.ToString("X"));
			}
			else if (1 == 1)
			{
				var del = CreateDelegateFunc<UInt64Func>(code_bswap64);
				Console.WriteLine();
				var arg = 0x0102030405060708UL;
				var rv = del(arg);
				Trace.Assert(rv == 0x0807060504030201);
				Console.WriteLine(rv.ToString("X"));
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
			// kunne alernativt bruge VirtualProtect til at goere koden eksekverbar: http://stackoverflow.com/questions/5893024/why-is-calli-faster-than-a-delegate-call

			var newFunctionAddress = VirtualAlloc(IntPtr.Zero, new IntPtr(100), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

			if (newFunctionAddress == IntPtr.Zero)
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			Marshal.Copy(codebytes, 0, newFunctionAddress, codebytes.Length);

			var del = (Int64Func)Marshal.GetDelegateForFunctionPointer(newFunctionAddress, typeof(Int64Func));
			return del;
		}
		public static TDelegate CreateDelegateFunc<TDelegate>(byte[] codebytes)
		{
			var newFunctionAddress = VirtualAlloc(IntPtr.Zero, new IntPtr(100), MEM_COMMIT | MEM_RESERVE, PAGE_EXECUTE_READWRITE);

			if (newFunctionAddress == IntPtr.Zero)
				Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
			Marshal.Copy(codebytes, 0, newFunctionAddress, codebytes.Length);

			var del = (TDelegate)(object)Marshal.GetDelegateForFunctionPointer(newFunctionAddress, typeof(TDelegate));
			return del;
		}


		[MethodImpl(MethodImplOptions.NoInlining)]
		private static int AddFive(int arg) => arg + 5;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static int ReturnArgument(int arg) => arg;
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static string ReturnArgument(string arg) => arg;

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static int PopCnt32Dummy(int arg) => 0xf0f0f0;
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static int PopCnt64Dummy(ulong arg) => 0xf0f0f0;


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