using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Event, AllowMultiple = false, Inherited = false)]
	public sealed class KeptBackingFieldAttribute : KeptAttribute {
	}
}
