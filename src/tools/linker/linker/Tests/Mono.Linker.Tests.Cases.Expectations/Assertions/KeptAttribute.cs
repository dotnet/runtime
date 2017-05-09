using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.All, Inherited = false)]
	public class KeptAttribute : BaseExpectedLinkedBehaviorAttribute {
	}
}