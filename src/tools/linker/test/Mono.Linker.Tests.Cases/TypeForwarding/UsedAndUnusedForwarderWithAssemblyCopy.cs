using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// link - This assembly, Unused.dll
	// copy - Forwarder.dll, Implementation.dll
	[SetupLinkerDefaultAction ("copy")]
	[SetupLinkerAction ("link", "Unused")]
	[SetupLinkerAction ("link", "test")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Unused.dll", new[] { "Dependencies/AnotherLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary_2.cs" }, references: new[] { "Implementation.dll", "Unused.dll" })]

	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	// The whole assembly is kept as is, since it is marked with the `copy` action.
	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[KeptTypeInAssembly ("Forwarder.dll", "Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.AnotherLibrary`1")]
	[KeptReferencesInAssembly ("Forwarder.dll", new[] { "System.Runtime", "Implementation", "Unused" })]
	// Even though `Forwarder` references this assembly, none of its members are marked (none is used) and, since `Unused`
	// has `link` action, it is removed.
	[RemovedAssembly ("Unused.dll")]
	class UsedAndUnusedForwarderWithAssemblyCopy
	{
		static void Main ()
		{
			new ImplementationLibrary ().GetSomeValue ();
		}
	}
}
