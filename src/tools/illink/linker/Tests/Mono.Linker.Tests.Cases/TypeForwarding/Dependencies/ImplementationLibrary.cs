using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies {
	[NotATestCase]
	public class ImplementationLibrary {
		public string GetSomeValue ()
		{
			return "Hello";
		}
	}
}
