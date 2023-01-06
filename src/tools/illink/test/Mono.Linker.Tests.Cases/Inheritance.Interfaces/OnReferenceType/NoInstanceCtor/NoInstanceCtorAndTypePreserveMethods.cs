using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/NoInstanceCtorAndAssemblyPreserveAll_Lib.il" })]

	// The interfaces should be removed because the interface types are not marked
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
	[SetupLinkerDescriptorFile ("NoInstanceCtorAndTypePreserveMethods.xml")]
	public class NoInstanceCtorAndTypePreserveMethods
	{
		public static void Main ()
		{
		}
	}
}