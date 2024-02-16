

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DimProvidedByUnreferencedIfaceInHierarchy.il" })]
	[SkipILVerify]

#if IL_ASSEMBLY_AVAILABLE
	[KeptMemberInAssembly ("library.dll", typeof(Program.IBar), "Program.IFoo.Method()")]
	[KeptMemberInAssembly ("library.dll", typeof(Program.IFoo), "Method()")]
	[KeptTypeInAssembly ("library.dll", typeof(Program.IBar))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.IBar), "library.dll", typeof (Program.IFoo))]
	// https://github.com/dotnet/runtime/issues/98536
	//[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.MyFoo), "library.dll", typeof (Program.IBaz))]
	//[KeptTypeInAssembly ("library.dll", typeof(Program.IBaz))]
	//[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.IBaz), "library.dll", typeof (Program.IBar))]
	[KeptMemberInAssembly ("library.dll", typeof(Program), "CallMethod(Program/IFoo)")]
#endif
	class DimProvidedByUnreferencedIfaceInHierarchy
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Program.IFoo foo = new Program.MyFoo ();
			Program.CallMethod(foo);
#endif
		}
	}
}



// public static class Program
// {
// 	[Kept]
// 	interface IFoo
// 	{
// 		void Method();
// 	}

// 	[Kept]
// 	interface IBar : IFoo
// 	{
// 		[Kept]
// 		void IFoo.Method() { }
// 	}

// 	[Kept]
// 	interface IBaz: IBar /* not IFoo */
// 	{
// 	}

// 	[Kept]
// 	[KeptInterface(typeof(IBaz))]
// 	class MyFoo : IBaz /* not IBar, not IFoo */
// 	{ }

// 	static void CallMethod(IFoo foo)
// 	{
// 		foo.Method();
// 	}
// }
