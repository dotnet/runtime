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
	[RemovedMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks",
		new string[] { "NeverReached()" })]
	[ExpectedInstructionSequenceOnMemberInAssembly ("library.dll",
		"Mono.Linker.Tests.Cases.UnreachableBlock.Dependencies.AssemblyWithUnreachableBlocks",
		"TestProperty()",
		new string[] {
			"ldc.i4.0",
			"brfalse.s il_3",
			"ret",
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