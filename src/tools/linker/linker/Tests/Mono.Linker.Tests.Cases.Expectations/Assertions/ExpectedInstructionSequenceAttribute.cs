using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public class ExpectedInstructionSequenceAttribute : BaseInAssemblyAttribute {
		public ExpectedInstructionSequenceAttribute (string[] opCodes)
		{
			if (opCodes == null)
				throw new ArgumentNullException ();
		}
	}
}