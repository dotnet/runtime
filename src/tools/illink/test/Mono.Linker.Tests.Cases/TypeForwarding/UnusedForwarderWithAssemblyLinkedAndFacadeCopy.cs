using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Implementation.dll
	// copy - Forwarder.dll
	[SetupLinkerDefaultAction ("link")]
	[SetupLinkerAction ("copy", "Forwarder")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary_3.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary_3.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary_3.cs" }, references: new[] { "Implementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary3B")]
	[KeptTypeInAssembly ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.RealClassInForwarder3")]
	[KeptTypeInAssembly ("Implementation.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary3B")]
	[RemovedForwarder ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary3A")]
	class UnusedForwarderWithAssemblyLinkedAndFacadeCopy
	{
		static void Main ()
		{
			new ImplementationLibrary3B ();
		}
	}
}
