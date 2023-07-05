using System;

namespace Mono.Linker.Tests.Cases.Attributes.Dependencies
{
	public class AssemblyAttributeKeptInComplexCase_Lib
	{
		public class OtherAssemblyAttribute : Attribute
		{
		}

		public static void MethodThatWillBeUsed ()
		{
		}
	}
}
