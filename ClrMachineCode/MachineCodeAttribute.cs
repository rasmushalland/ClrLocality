using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrMachineCode
{
	/// <summary>
	/// This attribute is used to mark methods with their machine code implementations.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public sealed class MachineCodeAttribute : Attribute
	{
		public string MachineCode { get; set; }
		public BaseArchitecture BaseArchitecture { get; }
		public string Assembly { get; set; }

		public MachineCodeAttribute(BaseArchitecture baseArchitecture, string machineCode)
		{
			MachineCode = machineCode;
			BaseArchitecture = baseArchitecture;
		}
	}

	public enum BaseArchitecture
	{
		None,
		x86,
		x64,
	}
	public enum ArchitectureExtension
	{
		None,
		/// <summary>
		/// x86 popcnt instruction.
		/// </summary>
		PopCnt,
	}
}
