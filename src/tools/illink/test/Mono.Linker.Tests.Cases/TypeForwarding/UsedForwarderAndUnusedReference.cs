using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Forwarder.dll and Implementation.dll

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	// Add an unused reference which gets resolved while removing the reference to the type forwarder
	[SetupCompileBefore ("UnusedLibrary.dll", new[] { "Dependencies/AnotherLibrary.cs" })]

	[RemovedAssembly ("Forwarder.dll")]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	[RemovedAssembly ("UnusedLibrary.dll")]
	class UsedForwarderAndUnusedReference
	{
		static void Main ()
		{
			Console.WriteLine (new ImplementationLibrary ().GetSomeValue ());
		}

		static void Unused ()
		{
			var _ = new AnotherLibrary<int> ();
		}
	}
}
