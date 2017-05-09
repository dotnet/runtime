using System;
using System.Diagnostics;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class KeptInterfaceAttribute : KeptAttribute
	{
		public readonly Type InterfaceType;

		public KeptInterfaceAttribute (Type interfaceType)
		{
			InterfaceType = interfaceType;
		}
	}
}