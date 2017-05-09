using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class)]
	public class IgnoreTestCaseAttribute : Attribute {
		public readonly string Reason;

		public IgnoreTestCaseAttribute (string reason)
		{
			Reason = reason;
		}
	}
}