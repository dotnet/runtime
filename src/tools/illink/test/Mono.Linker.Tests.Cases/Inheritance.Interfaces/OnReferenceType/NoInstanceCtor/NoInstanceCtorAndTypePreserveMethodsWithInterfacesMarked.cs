using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[Define ("IL_ASSEMBLY_COMPILED")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/NoInstanceCtorAndAssemblyPreserveAll_Lib.il" })]

	// Interfaces will be removed because there is no instance ctor that is marked.
	[RemovedInterfaceOnTypeInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IFoo")]
	[RemovedInterfaceOnTypeInAssemblyAttribute ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IBar")]

	// Methods should be kept because of the preserve methods
	[KeptMemberInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"Foo()")]
	[KeptMemberInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"Bar()")]
	[SetupLinkerDescriptorFile ("NoInstanceCtorAndTypePreserveMethodsWithInterfacesMarked.xml")]
	public class NoInstanceCtorAndTypePreserveMethodsWithInterfacesMarked
	{
		public static void Main ()
		{
			// We'll mark one interface in code and one via xml, the end result should be the same
#if IL_ASSEMBLY_COMPILED
			var tmp = typeof (Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib.IFoo).ToString ();
#endif
		}
	}
}