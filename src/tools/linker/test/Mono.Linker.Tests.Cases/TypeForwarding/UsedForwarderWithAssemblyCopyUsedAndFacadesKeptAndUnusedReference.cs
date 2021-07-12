using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly
	// copyused - Forwarder.dll, Implementation.dll, and UnusedImplementation.dll
	// --keep-facades
	[SetupLinkerAction ("link", "test")]
	[SetupLinkerDefaultAction ("copyused")]
	[KeepTypeForwarderOnlyAssemblies ("true")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationUsedAndUnusedLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("UnusedImplementation.dll", new[] { "Dependencies/UnusedImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibraryWithUnusedReference.cs" }, references: new[] { "Implementation.dll", "UnusedImplementation.dll" })]

	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[KeptTypeInAssembly ("Forwarder.dll", typeof (UnusedImplementationLibrary))]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	[RemovedAssemblyReference ("test", "Forwarder")]
	[RemovedAssembly ("UnusedImplementation.dll")]
	class UsedForwarderWithAssemblyCopyUsedAndFacadesKeptAndUnusedReference
	{
		static void Main ()
		{
			new ImplementationLibrary ().GetSomeValue ();
		}
	}
}
