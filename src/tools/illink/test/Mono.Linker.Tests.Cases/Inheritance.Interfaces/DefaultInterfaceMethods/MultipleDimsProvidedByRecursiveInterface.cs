

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MultipleDimsProvidedByRecursiveInterface.il" })]
	[SkipILVerify]

#if IL_ASSEMBLY_AVAILABLE
	// Both DIMs on I01 and I00 should be kept because one is not more specific than another.
	[KeptMemberInAssembly ("library.dll", typeof(Program.I0), "Method()")]
	[KeptTypeInAssembly ("library.dll", typeof(Program.I00))]
	[KeptMemberInAssembly ("library.dll", typeof(Program.I00), "Program.I0.Method()")]
	// Bug: DIM resolution doesn't look at recursive interfaces
	//[KeptMemberInAssembly ("library.dll", typeof(Program.I01), "Program.I0.Method()")]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I00), "library.dll", typeof (Program.I0))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.MyFoo), "library.dll", typeof (Program.I000))]
	[KeptTypeInAssembly ("library.dll", typeof(Program.I000))]
	[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I000), "library.dll", typeof (Program.I00))]
	// Bug: DIM resolution doesn't look at recursive interfaces
	//[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.MyFoo), "library.dll", typeof (Program.I010))]
	//[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I010), "library.dll", typeof (Program.I01))]
	//[KeptInterfaceOnTypeInAssembly ("library.dll", typeof (Program.I01), "library.dll", typeof (Program.I0))]
#endif
	class MultipleDimsProvidedByRecursiveInterface
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			Program.I0 foo = new Program.MyFoo ();
			CallMethod(foo);
#endif
		}
#if IL_ASSEMBLY_AVAILABLE
		[Kept]
		static void CallMethod(Program.I0 foo)
		{
			foo.Method();
		}
#endif
	}
}



// public static class Program
// {
// 	[Kept]
// 	interface I0
// 	{
// 		void Method();
// 	}

// 	[Kept]
// 	interface I00 : I0
// 	{
// 		[Kept]
// 		void I0.Method() { }
// 	}

// 	[Kept]
// 	interface I000: I00 /* not I0 */
// 	{
// 	}

// 	[Kept]
// 	interface I01 : I0
// 	{
// 		[Kept]
// 		void I0.Method() { }
// 	}

// 	[Kept]
// 	interface I010: I01 /* not I0 */
// 	{
// 	}

// 	[Kept]
// 	[KeptInterface(typeof(I000))]
// 	class MyFoo : I000, I010
// 	{ }

// }
