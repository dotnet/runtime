using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	// Actions:
	// copy - This assembly
	// link - Forwarder.dll and Implementation.dll
	[SetupLinkerAction ("copy", "test")]
	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	[KeptAssembly ("Forwarder.dll")]
	[KeptTypeInAssembly ("Implementation.dll", typeof (ImplementationLibrary.ImplementationLibraryNestedType))]
	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary.ImplementationLibraryNestedType))]

	[Kept]
	[KeptMember (".ctor()")]
	public class UsedForwarderInCopyAssemblyKeptByUsedNestedType
	{
		public static void Main ()
		{
			new ImplementationLibrary.ImplementationLibraryNestedType ();
		}
	}
}