using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptAllTypesAndMembersInAssemblyAttribute : BaseInAssemblyAttribute {
		public KeptAllTypesAndMembersInAssemblyAttribute (string assemblyFileName)
		{
			if (string.IsNullOrEmpty (assemblyFileName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (assemblyFileName));
		}
	}
}