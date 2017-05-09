using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	/// <summary>
	/// Verifies that an assembly does not exist in the output directory
	/// </summary>
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class RemovedAssemblyAttribute : BaseExpectedLinkedBehaviorAttribute {
		public readonly string FileName;

		public RemovedAssemblyAttribute (string fileName)
		{
			FileName = fileName;
		}
	}
}