using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Interface | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event, AllowMultiple = true, Inherited = false)]
	public class RemovedPseudoAttributeAttribute : BaseExpectedLinkedBehaviorAttribute {
		public RemovedPseudoAttributeAttribute (uint value) {
		}
	}
}