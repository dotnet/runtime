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
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/GenericsFull.il" })]
	[KeptTypeInAssembly ("library.dll", "IFaceNonGeneric")]
	[KeptTypeInAssembly ("library.dll", "IFaceGeneric`1")]
	[KeptTypeInAssembly ("library.dll", "IFaceCuriouslyRecurringGeneric`1")]
	[KeptMemberInAssembly ("library.dll", "NonGenericClass", "IFaceNonGeneric.NormalMethod()", "IFaceNonGeneric.GenericMethod<#1>()",
		"IFaceGeneric`1<string>.NormalMethod()", "IFaceGeneric`1<string>.GenericMethod<#1>()", "IFaceGeneric`1<object>.NormalMethod()", "IFaceGeneric`1<object>.GenericMethod<#1>()",
		"IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.NormalMethod()", "IFaceCuriouslyRecurringGeneric`1<class NonGenericClass>.GenericMethod<#1>()")]
	[KeptMemberInAssembly ("library.dll", "GenericClass`1", "IFaceNonGeneric.NormalMethod()", "IFaceNonGeneric.GenericMethod<#1>()",
		"IFaceGeneric`1<string>.NormalMethod()", "IFaceGeneric`1<string>.GenericMethod<#1>()", "IFaceGeneric`1<object>.NormalMethod()", "IFaceGeneric`1<object>.GenericMethod<#1>()",
		"IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.NormalMethod()", "IFaceCuriouslyRecurringGeneric`1<class GenericClass`1<!0>>.GenericMethod<#1>()")]
	[KeptMemberInAssembly ("library.dll", "NonGenericValuetype", "IFaceNonGeneric.NormalMethod()", "IFaceNonGeneric.GenericMethod<#1>()",
		"IFaceGeneric`1<string>.NormalMethod()", "IFaceGeneric`1<string>.GenericMethod<#1>()", "IFaceGeneric`1<object>.NormalMethod()", "IFaceGeneric`1<object>.GenericMethod<#1>()",
		"IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.NormalMethod()", "IFaceCuriouslyRecurringGeneric`1<valuetype NonGenericValuetype>.GenericMethod<#1>()")]
	[KeptMemberInAssembly ("library.dll", "GenericValuetype`1", "IFaceNonGeneric.NormalMethod()", "IFaceNonGeneric.GenericMethod<#1>()",
		"IFaceGeneric`1<string>.NormalMethod()", "IFaceGeneric`1<string>.GenericMethod<#1>()", "IFaceGeneric`1<object>.NormalMethod()", "IFaceGeneric`1<object>.GenericMethod<#1>()",
		"IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.NormalMethod()", "IFaceCuriouslyRecurringGeneric`1<valuetype GenericValuetype`1<!0>>.GenericMethod<#1>()")]

	class GenericsFull
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			TestEntrypoint.Test();
#endif
		}
	}
}
