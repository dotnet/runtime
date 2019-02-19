using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public class ExpectedExceptionHandlerSequenceAttribute : BaseInAssemblyAttribute {
		public ExpectedExceptionHandlerSequenceAttribute (string[] types)
		{
			if (types == null)
				throw new ArgumentNullException ();
		}
	}
}