using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// copyused - This assembly
	[SetupLinkerAction ("copyused", "test")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
	[SetupCompileBefore ("ForwarderUnused.dll", new[] { "Dependencies/AnotherLibraryReferenceImplementation.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	// The used implementation is resolved during marking, causing the unused forwarder to be removed
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]
	// The unused implementation may not be resolved until SweepStep updates scopes of the copyused assembly
	[SetupCompileAfter ("Unused.dll", new[] { "Dependencies/AnotherLibrary.cs" })]
	[SetupCompileAfter ("ForwarderUnused.dll", new[] { "Dependencies/AnotherLibraryForwarder.cs" }, references: new[] { "Unused.dll" })]

	[RemovedAssembly ("Forwarder.dll")]
	[RemovedAssembly ("ForwarderUnused.dll")]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	[RemovedAssembly ("Unused.dll")]
	[KeptMember (".ctor()")]
	class UsedAndUnusedForwarderReferencedFromCopyUsedAssembly
	{
		static void Main ()
		{
			Used ();
		}

		[Kept]
		static void Used ()
		{
			new ImplementationLibrary ().GetSomeValue ();
		}

		[Kept]
		static void Unused ()
		{
			var _ = new AnotherLibrary<int> ();
		}
	}
}
