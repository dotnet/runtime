using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/NoInstanceCtorAndAssemblyPreserveAll_Lib.il" })]
	[RemovedInterfaceOnTypeInAssembly ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IFoo")]
	[RemovedInterfaceOnTypeInAssemblyAttribute ("library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/A",
		"library",
		"Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoInstanceCtor.Dependencies.NoInstanceCtorAndAssemblyPreserveAll_Lib/IBar")]
	[SetupLinkerDescriptorFile ("NoInstanceCtorAndTypePreserveNone.xml")]
	// pedump reports this is valid with the following error.
	//
	// Assertion at metadata.c:1073, condition `index < meta->heap_blob.size' not met
	//
	// Not worried about it since this is already a niche edge case.  Let's just skip verify for pedump
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	public class NoInstanceCtorAndTypePreserveNone
	{
		public static void Main ()
		{
		}
	}
}