using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies {
	[NotATestCase]
	public class ReferenceImplementationLibrary {
	}

#if INCLUDE_REFERENCE_IMPL
	public class ImplementationLibrary {
		public string GetSomeValue ()
		{
			return null;
		}
	}
#endif
}
