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
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/TypeHierarchyBasic.il" })]
	[KeptMemberInAssembly ("library.dll", "InterfaceScenario1", "Method()")]
	[KeptMemberInAssembly ("library.dll", "BaseScenario1", "Method()")]
	[KeptTypeInAssembly ("library.dll", "DerivedScenario1")]
	[KeptMemberInAssembly ("library.dll", "InterfaceScenario2", "Method()")]
	[KeptMemberInAssembly ("library.dll", "BaseScenario2", "Method()")]
	[KeptTypeInAssembly ("library.dll", "DerivedScenario2")]
	[KeptMemberInAssembly ("library.dll", "InterfaceScenario3", "Method()")]
	[KeptTypeInAssembly ("library.dll", "BaseScenario3")]
	[KeptMemberInAssembly ("library.dll", "DerivedScenario3", "MethodImplOnDerived()")]

	class TypeHierarchyBasic
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			TestEntrypoint.Test();
#endif
		}
	}
}
