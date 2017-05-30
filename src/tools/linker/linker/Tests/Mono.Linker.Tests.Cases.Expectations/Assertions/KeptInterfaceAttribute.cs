using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true, Inherited = false)]
	public class KeptInterfaceAttribute : KeptAttribute
	{

		public KeptInterfaceAttribute (Type interfaceType)
		{
			if (interfaceType == null)
				throw new ArgumentNullException (nameof (interfaceType));
		}
	}
}