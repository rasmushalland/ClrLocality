using System;
using System.Runtime.CompilerServices;

namespace ClrBasics
{
	/// <summary>
	/// https://defuse.ca/online-x86-assembler.htm#disassembly
	/// </summary>
	public static class IntrinsicOps
	{
		#region ulong SwapBytes(ulong arg)

		public static ulong SwapBytes(ulong arg)
		{
			return SwapBytes_UseReplaced_64 ? SwapBytesReplaced(arg) : SwapBytesSoftware(arg);
		}

		public static readonly bool SwapBytes_UseReplaced_64 =
			MachineCodeHandler.UseReplaced(() => SwapBytesReplaced(default(ulong)));

		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x64, "4889C80FC948C1E12048C1E8200FC84801C8C3", ArchitectureExtension.None, Assembly = @"
mov rax, rcx
bswap ecx
shl rcx, 0x20

shr rax, 0x20
bswap eax
add rax, rcx
ret")]
		public static ulong SwapBytesReplaced(ulong arg)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		public static ulong SwapBytesSoftware(ulong arg)
		{
			return ((ulong)SwapBytesSoftware((uint)arg) << 32) |
				   SwapBytesSoftware((uint)(arg >> 32));
		}

		#endregion

		#region uint SwapBytes(uint arg)

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint SwapBytes(uint arg)
		{
			return SwapBytes_UseReplaced_32 ? SwapBytesReplaced(arg) : SwapBytesSoftware(arg);
		}

		public static readonly bool SwapBytes_UseReplaced_32 =
			MachineCodeHandler.UseReplaced(() => SwapBytesReplaced(default(uint)));


		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x86, "0FC989C8C3", ArchitectureExtension.None, Assembly = @"
bswap ecx
mov eax, ecx
ret")]
		public static uint SwapBytesReplaced(uint arg)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		public static uint SwapBytesSoftware(uint arg)
		{
			return arg << 24 |
			       ((arg & 0x0000ff00) << 8) |
			       ((arg & 0x00ff0000) >> 8) |
			       (arg >> 24);
		}

		#endregion

		#region PopulationCount

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopulationCount(ulong arg)
		{
			return PopulationCount_UseReplaced_64 ? PopulationCountReplaced(arg) : PopulationCountSoftware(arg);
		}

		public static readonly bool PopulationCount_UseReplaced_64 =
			MachineCodeHandler.UseReplaced(() => PopulationCountReplaced(default(ulong)));


		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x64, "F3480FB8C1C3", ArchitectureExtension.PopCnt, Assembly = @"
popcnt rax, rcx
ret")]
		public static int PopulationCountReplaced(ulong arg)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int PopulationCount(uint arg)
		{
			return PopulationCount_UseReplaced_32 ? PopulationCountReplaced(arg) : PopulationCountSoftware(arg);
		}

		public static readonly bool PopulationCount_UseReplaced_32 =
			MachineCodeHandler.UseReplaced(() => PopulationCountReplaced(default(uint)));

		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x86, "F30FB8C1C3", ArchitectureExtension.PopCnt, Assembly = @"
popcnt eax, ecx
ret")]
		public static int PopulationCountReplaced(uint arg)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		#endregion

		#region PopulationCountSoftware

		private const ulong m1 = 0x5555555555555555; //binary: 0101...
		private const ulong m2 = 0x3333333333333333; //binary: 00110011..
		private const ulong m4 = 0x0f0f0f0f0f0f0f0f; //binary:  4 zeros,  4 ones ...
		private const ulong m8 = 0x00ff00ff00ff00ff; //binary:  8 zeros,  8 ones ...
		private const ulong m16 = 0x0000ffff0000ffff; //binary: 16 zeros, 16 ones ...
		private const ulong m32 = 0x00000000ffffffff; //binary: 32 zeros, 32 ones
		private const ulong hff = 0xffffffffffffffff; //binary: all ones
		private const ulong h01 = 0x0101010101010101; //the sum of 256 to the power of 0,1,2,3...

		// This uses fewer arithmetic operations than any other known  
		// implementation on machines with fast multiplication.
		// It uses 12 arithmetic operations, one of which is a multiply.
		public static int PopulationCountSoftware(ulong x)
		{
			x -= (x >> 1) & m1; //put count of each 2 bits into those 2 bits
			x = (x & m2) + ((x >> 2) & m2); //put count of each 4 bits into those 4 bits 
			x = (x + (x >> 4)) & m4; //put count of each 8 bits into those 8 bits 
			return (int) ((x*h01) >> 56); //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
		}

		public static int PopulationCountSoftware(uint x)
		{
			x -= (x >> 1) & 0x55555555;             //put count of each 2 bits into those 2 bits
			x = (x & 0x33333333) + ((x >> 2) & 0x33333333); //put count of each 4 bits into those 4 bits 
			x = (x + (x >> 4)) & 0x0f0f0f0f;        //put count of each 8 bits into those 8 bits 
			return (int)((x * 0x01010101) >> 24);  //returns left 8 bits of x + (x<<8) + (x<<16) + (x<<24) + ... 
		}

		#endregion

		#region LeadingZeroCount

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int LeadingZeroCount(uint arg)
		{
			return LeadingZeroCount_UseReplaced_32 ? LeadingZeroCountReplaced(arg) : LeadingZeroCountSoftware(arg);
		}

		public static readonly bool LeadingZeroCount_UseReplaced_32 =
			MachineCodeHandler.UseReplaced(() => LeadingZeroCountReplaced(default(uint)));

		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x86, "F30FBDC1C3", ArchitectureExtension.Lzcnt, Assembly = @"
popcnt eax, ecx
ret")]
		public static int LeadingZeroCountReplaced(uint arg)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		public static int LeadingZeroCountSoftware(uint arg)
		{
			var x = arg;
			x |= x >> 1;
			x |= x >> 2;
			x |= x >> 4;
			x |= x >> 8;
			x |= x >> 16;
			return sizeof(int)*8 - Ones(x);
		}
		static int Ones(uint x)
		{
			x -= ((x >> 1) & 0x55555555);
			x = (((x >> 2) & 0x33333333) + (x & 0x33333333));
			x = (((x >> 4) + x) & 0x0f0f0f0f);
			x += (x >> 8);
			x += (x >> 16);
			return (int) (x & 0x0000003f);
		}

		#endregion

		#region AsciiToChar

		/// <summary>
		/// PUNPCKHBW: http://x86.renejeschke.de/html/file_module_x86_id_267.html
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x64, "480FC966480F6EC10F16C04831C966480F6EC90F16C9660F68C1F3410F7F00480FCA66480F6EC20F16C0660F68C14983C010F3410F7F00C3", ArchitectureExtension.Sse42, Assembly = @"
# move arg0 to upper half of xmm0
movd xmm0, rcx
movlhps xmm0, xmm0

# move zero to upper half of xmm1
xor rcx, rcx
movd xmm1, rcx
movlhps xmm1, xmm1

# unpack and store for return
punpckhbw xmm0, xmm1
movdqu [r8], xmm0

# move arg1 to upper half of xmm0
movd xmm0, rdx
movlhps xmm0, xmm0

# unpack and store for return
punpckhbw xmm0, xmm1
add r8, 0x10
movdqu [r8], xmm0

ret")]
		public static void AsciiToCharReplaced(ulong arg1, ulong arg2, IntPtr ret)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		#endregion

		#region CharToAscii

		/// <summary>
		/// PUNPCKHBW: http://x86.renejeschke.de/html/file_module_x86_id_267.html
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x64, "49B90E0C0A080604020066490F6EC9F30F6F01660F3800C166480F7EC0498900F30F6F4110660F3800C166480F7EC0488902C3", ArchitectureExtension.Sse42, Assembly = @"
mov r9, 0x00020406080a0c0e # shuffle mask
movd xmm1, r9

movdqu xmm0, [rcx]
pshufb xmm0, xmm1
movd rax, xmm0
mov [r8], rax

movdqu xmm0, [rcx + 0x10]
pshufb xmm0, xmm1
movd rax, xmm0
mov [rdx], rax

ret")]
		public static unsafe void CharToAsciiReplaced(char* charsIn, out ulong long1, out ulong long2)
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		#endregion

		#region Pause

		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x86, "F390C3", ArchitectureExtension.None, Assembly = @"
pause
ret")]
		public static void PauseReplaced()
		{
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		/// <summary>
		/// https://groups.google.com/forum/#!topic/mechanical-sympathy/Fy7vbnxIamA
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Pause()
		{
			if (Pause_UseReplaced)
				PauseReplaced();
		}

		public static readonly bool Pause_UseReplaced = MachineCodeHandler.UseReplaced(() => PauseReplaced());

		#endregion


		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Nop(int arg)
		{
		}
		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Nop(ulong arg)
		{
		}
	}

	/// <summary>
	/// This class is for determining cpu features. It is separate because it is simpler/less error prone to prepare it this way.
	/// </summary>
	static class CpuIDOps
	{
		/// <summary>
		/// Intel® 64 and IA-32 Architectures Developer's Manual p. 3-179.
		/// </summary>
		[MethodImpl(MethodImplOptions.NoInlining)]
		[MachineCode(BaseArchitecture.x86, "5389C80FA289C85BC3", ArchitectureExtension.None, Assembly = @"
push rbx
mov eax, ecx
cpuid 
mov eax, ecx
pop rbx
ret")]
		public static int CPUIDEcxReplaced(uint eax)
		{
			IntrinsicOps.Nop(42);
			IntrinsicOps.Nop(42);
			IntrinsicOps.Nop(42);
			IntrinsicOps.Nop(42);
			IntrinsicOps.Nop(42);
			throw new InvalidOperationException("Should have been replaced.");
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static void Nop(int arg)
		{
		}
	}
}
