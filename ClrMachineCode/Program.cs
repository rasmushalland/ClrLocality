using System;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;

namespace ClrMachineCode
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			if (1 == 2)
			{

				Console.WriteLine("forb");
				MachineCodeHandler.EnsurePrepared(typeof(IntrinsicOps));
				Console.WriteLine("{0:X}", IntrinsicOps.PopulationCountReplaced(0x010203L));
				Console.WriteLine("done");
				Console.ReadLine();
				//PopCntTest.Test();
			}
			else if (1 == 1)
			{
				Console.WriteLine("attach");
				//Console.ReadLine();
				Debugger.Launch();
				Console.WriteLine(new Struct1 { val1 = 0x0102, val2 = 0x0304 }.GetVal1());
				//StructFunc(new Struct1 {val1 = 0x0102, val2 = 0x0304});

				//Console.WriteLine("forb");
				//MachineCodeHandler.EnsurePrepared(typeof(IntrinsicOps));
				//Console.WriteLine("{0:X}", IntrinsicOps.PopulationCount(0x010203L));
				//Console.WriteLine("done");
				//PopCntTest.Test();
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		static int StructFunc(Struct1 arg)
		{
			return (int) arg.val1;
		}

		struct Struct1
		{
			public long val1;
			public long val2;

			[MethodImpl(MethodImplOptions.NoInlining)]
			public int GetVal1() => (int) val1;

		}
	}
}