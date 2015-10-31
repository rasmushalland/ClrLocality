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
			if (1 == 1)
			{

				Console.WriteLine("forb");
				MachineCodeHandler.EnsurePrepared(typeof(IntrinsicOps));
				Console.WriteLine("{0:X}", IntrinsicOps.PopulationCount(0x010203L));
				Console.WriteLine("done");
				Console.ReadLine();
				//PopCntTest.Test();
			}
		}

	}
}