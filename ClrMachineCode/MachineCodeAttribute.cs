using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrBasics
{
	/// <summary>
	/// This attribute is used to mark methods with their machine code implementations.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class MachineCodeAttribute : Attribute
	{
		public string MachineCode { get; set; }
		public ArchitectureExtension RequiredExtensions { get; set; }
		public BaseArchitecture BaseArchitecture { get; }
		public string Assembly { get; set; }

		public MachineCodeAttribute(BaseArchitecture baseArchitecture, string machineCode, ArchitectureExtension requiredExtensions)
		{
			BaseArchitecture = baseArchitecture;
			MachineCode = machineCode;
			RequiredExtensions = requiredExtensions;
		}
	}

	public enum BaseArchitecture
	{
		None,
		x86,
		x64,
	}

	[Flags]
	public enum ArchitectureExtension
	{
		None,
		/// <summary>
		/// x86 popcnt instruction.
		/// </summary>
		PopCnt,
		Sse41,
		Sse42,
	}
}
