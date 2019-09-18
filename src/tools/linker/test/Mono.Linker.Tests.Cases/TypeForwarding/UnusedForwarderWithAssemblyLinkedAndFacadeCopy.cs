using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Implementation.dll
	// copy - Forwarder.dll
	// --keep-facades
	[SetupLinkerUserAction ("link")]
	[KeepTypeForwarderOnlyAssemblies ("true")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary_3.cs" }, references: new[] { "Implementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ReferenceImplementationLibrary")]
	[RemovedForwarder ("Forwarder.dll", "ImplementationLibrary")]
	[RemovedAssembly ("Implementation.dll")]
	class UnusedForwarderWithAssemblyLinkedAndFacadeCopy
	{
		static void Main ()
		{
			new ReferenceImplementationLibrary();
		}
	}
}
