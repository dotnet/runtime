using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies
{
	[NotATestCase]
	public class LibraryUsingForwarder
	{
		public string GetValueFromOtherAssembly ()
		{
			return new ImplementationLibrary ().GetSomeValue ();
		}
	}
}
