using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_COMPILED")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/NoInstanceCtorAndAssemblyPreserveAll_Lib.il" })]

	// Interfaces should be removed because preserve fields would not normally cause an instance ctor to be marked.  This means that no instance ctor
	// sweeping logic should kick in and remove interfaces
	[RemovedInterfaceOnTypeInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IFoo")]
	[RemovedInterfaceOnTypeInAssemblyAttribute ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IBar")]

	// Methods are removed because we only preserved fields
	[RemovedMemberInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"Foo()")]
	[RemovedMemberInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"Bar()")]
	[SetupLinkerDescriptorFile ("NoInstanceCtorAndTypePreserveFieldsWithInterfacesMarked.xml")]

	// pedump reports this is valid with the following error.
	//
	// Assertion at metadata.c:1073, condition `index < meta->heap_blob.size' not met
	//
	// Not worried about it since this is already a niche edge case.  Let's just skip verify for pedump
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	public class NoInstanceCtorAndTypePreserveFieldsWithInterfacesMarked
	{
		public static void Main ()
		{
#if IL_ASSEMBLY_COMPILED
			var tmp = typeof (Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib.IFoo).ToString ();
#endif
		}
	}
}