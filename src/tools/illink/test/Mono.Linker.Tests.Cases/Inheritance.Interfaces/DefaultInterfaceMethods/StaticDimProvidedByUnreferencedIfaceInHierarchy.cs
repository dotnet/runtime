

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/StaticDimProvidedByUnreferencedIfaceInHierarchy.il" })]
	[SkipILVerify]

#if IL_ASSEMBLY_AVAILABLE
	[KeptMemberInAssembly ("library.dll", typeof(Program), "CallMethod<#1>()")]
	[KeptTypeInAssembly ("library.dll", typeof(Program.IBase))]
	[KeptMemberInAssembly ("library.dll", typeof(Program.IBase), "Method()")]
	[KeptTypeInAssembly ("library.dll", typeof(Program.I4))]
	// https://github.com/dotnet/runtime/issues/98536
	[KeptTypeInAssembly ("library.dll", typeof(Program.I2))]
	[KeptMemberInAssembly ("library.dll", typeof(Program.I2), "Program.IBase.Method()")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I2), "library.dll", typeof (Program.IBase))]
	[KeptTypeInAssembly ("library.dll", typeof(Program.I3))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I3), "library.dll", typeof (Program.I2))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I4), "library.dll", typeof (Program.I3))]
#endif
	class StaticDimProvidedByUnreferencedIfaceInHierarchy
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Program.CallMethod<Program.I4>();
#endif
		}
	}
}



// public static class Program
// {
//	[Kept]
//	interface IBase
//	{
//		[Kept]
// 		static abstract void Method();
// 	}

//	[Kept]
//	[KeptInterface(typeof(IBase)]
//	interface I2 : IBase
//	{
//		[Kept]
//		static void IBase.Method() { }
//	}

//	[Kept]
// 	[KeptInterface(typeof(I2)]
// 	interface I3 : I2 { }

//	[Kept]
//	[KeptInterface(typeof(I3)]
//	interface I4 : I3 { }

//	[Kept]
//	static void CallMethod<T>() where T : IBase
//	{
//		T.Method();
//	}
// }
