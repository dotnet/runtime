using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	public class IsOverrideOfAttribute : BaseTypeMapInfoAttribute
	{
		public IsOverrideOfAttribute (string methodFullName)
		{ }
	}
}
