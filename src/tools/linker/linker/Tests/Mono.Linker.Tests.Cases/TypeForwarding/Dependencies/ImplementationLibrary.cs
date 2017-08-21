using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: AssemblyVersion ("2.0")]

namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies {
	public class ImplementationLibrary {
		public string GetSomeValue ()
		{
			return "Hello";
		}
	}
}
