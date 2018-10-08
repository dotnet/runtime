using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
	public class KeptFixedBufferAttribute : KeptAttribute {
	}
}