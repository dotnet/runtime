using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Event, Inherited = false, AllowMultiple = false)]
	public class KeptEventRemoveMethodAttribute : KeptAttribute {
	}
}
