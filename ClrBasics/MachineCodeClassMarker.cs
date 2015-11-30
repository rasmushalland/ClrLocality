using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClrBasics
{
	/// <summary>
	/// Initialize a static, readonly instance of this class on classes containing <see cref="MachineCodeAttribute"/> to trigger automatic initialization just in time.
	/// </summary>
	static public class MachineCodeClassMarker
	{
		public static object EnsurePrepared(Type type)
		{
			MachineCodeHandler.EnsurePrepared(type);
			return null;
		}
	}
}
