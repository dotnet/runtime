using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	/// <summary>
	/// Verifies that an assembly does exist in the output directory
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptAssemblyAttribute : KeptAttribute {

		public KeptAssemblyAttribute (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (fileName));
		}
	}
}