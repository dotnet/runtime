using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	/// <summary>
	/// Verifies that an assembly does not exist in the output directory
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class RemovedAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {

		public RemovedAssemblyAttribute (string fileName)
		{
			if (string.IsNullOrEmpty (fileName))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (fileName));
		}
	}
}