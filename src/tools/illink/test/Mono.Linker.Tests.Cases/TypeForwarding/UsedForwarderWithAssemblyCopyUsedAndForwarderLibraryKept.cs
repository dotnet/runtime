using System;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SetupLinkerDefaultAction ("copyused")]

	[SetupCompileBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]

	// The typeref to the type forwarder is updated, so the type forwarder is removed (and the assemblyref along with it)
	[RemovedTypeInAssembly ("Forwarder.dll", typeof (ImplementationLibrary))]
	[RemovedAssemblyReference ("test", "Forwarder")]
	// But other members of the forwarder assembly are kept
	[KeptTypeInAssembly ("Forwarder.dll", typeof (ImplementationStruct))]

	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	[KeptMember (".ctor()")]
	class UsedForwarderWithAssemblyCopyUsedAndForwarderLibraryKept
	{
		static void Main ()
		{
			// Preserve a member of the forwarder library to ensure the forwarder assembly is kept
			var t1 = Type.GetType ("Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationStruct, Forwarder");

			// Include a direct typeref to the forwarder that will get its scope updated
			var t = typeof (ReferencesForwarder);
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (ImplementationLibrary))]
		public class ReferencesForwarder : ImplementationLibrary
		{
		}
	}
}
