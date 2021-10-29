using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.UnreachableBlock
{
	[SetupCSharpCompilerToUse ("csc")]
	[SetupCompileArgument ("/optimize+")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SetupCompileBefore ("library.dll", new string[] { "Dependencies/ReferencedAssemblyWithUnreachableBlocks.cs" },
		addAsReference: false, additionalArguments: "/optimize+", compilerToUse: "csc")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks",
		new string[] { ".ctor()", "TestProperty()", "get_PropBool()" })]
	[RemovedMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks",
		new string[] { "NeverReached()" })]
	[ExpectedInstructionSequenceOnMemberInAssembly ("library.dll",
		"Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks",
		"TestProperty()",
		new string[] {
			"call System.Boolean Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks::get_PropBool()",
			"brfalse.s il_7",
			"ret"
		})]
	[Kept]
	public class WorksWithDynamicAssembly
	{
		public static void Main ()
		{
			DependOnAssemblyWithUnreachableBlocks ();
		}

		[Kept]
		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks", "library")]
		static void DependOnAssemblyWithUnreachableBlocks () { }
	}
}