using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public class ExpectedLocalsSequenceAttribute : BaseInAssemblyAttribute {
		public ExpectedLocalsSequenceAttribute (string [] types)
		{
			if (types == null)
				throw new ArgumentNullException ();
		}

		public ExpectedLocalsSequenceAttribute (Type [] types)
		{
			if (types == null)
				throw new ArgumentNullException ();
		}
	}
}