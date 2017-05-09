using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions {
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public sealed class KeptMemberAttribute : KeptAttribute {
		public readonly string Name;

		public KeptMemberAttribute (string name)
		{
			Name = name;
		}
	}
}