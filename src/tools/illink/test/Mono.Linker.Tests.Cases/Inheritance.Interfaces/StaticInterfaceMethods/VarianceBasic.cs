using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
#if !NETCOREAPP
	[IgnoreTestCase("Only for .NET Core for some reason")]
#endif
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/VarianceBasic.il" })]

	[KeptTypeInAssembly ("library.dll", "InterfaceScenario1`1")]
	[KeptTypeInAssembly ("library.dll", "InterfaceScenario2`1")]
	[KeptMemberInAssembly ("library.dll", "BaseScenario1", "Method()")]
	[KeptMemberInAssembly ("library.dll", "BaseScenario2", "Method()", "Method2()", "Method_Obj()", "Method2_Obj()")]
	[KeptTypeInAssembly ("library.dll", "DerivedScenario1")]
	[KeptMemberInAssembly ("library.dll", "DerivedScenario2", "Method()", "Method2()")]

	class VarianceBasic
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			TestEntrypoint.Test();
#endif
		}
	}
}
