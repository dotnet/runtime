using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsStaticInterfaceMethods, "Requires a framework that supports static interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/GenericsBasic_Ldftn.il" })]
	[KeptTypeInAssembly ("library.dll", "IFaceNonGeneric")]
	[KeptMemberInAssembly ("library.dll", "NonGenericClass", "IFaceNonGeneric.NormalMethod()", "IFaceNonGeneric.GenericMethod<#1>()")]

	class GenericsBasic_Ldftn
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			TestEntrypoint.Test();
#endif
		}
	}
}
