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

		public MachineCodeAttribute(string machineCode)
		{
			MachineCode = machineCode;
		}
	}
}
