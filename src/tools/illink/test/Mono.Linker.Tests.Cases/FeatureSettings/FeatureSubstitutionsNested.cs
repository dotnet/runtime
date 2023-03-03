using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FeatureSettings
{
	[SetupCompileResource ("Dependencies/ResourceFile.txt", "ResourceFileRemoveWhenTrue.txt")]
	[SetupCompileResource ("Dependencies/ResourceFile.txt", "ResourceFileRemoveWhenFalse.txt")]
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsGlobalTrue.xml")]
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsGlobalFalse.xml")]
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsNested.xml")]
	[SetupLinkerArgument ("--feature", "GlobalCondition", "true")]
	[SetupLinkerArgument ("--feature", "AssemblyCondition", "false")]
	[SetupLinkerArgument ("--feature", "TypeCondition", "true")]
	[SetupLinkerArgument ("--feature", "MethodCondition", "false")]
	[SetupLinkerArgument ("--feature", "FieldCondition", "true")]
	[SetupLinkerArgument ("--feature", "ResourceCondition", "true")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[RemovedResourceInAssembly ("test.exe", "ResourceFileRemoveWhenTrue.txt")]
	[KeptResource ("ResourceFileRemoveWhenFalse.txt")]
	public class FeatureSubstitutionsNested
	{
		public static void Main ()
		{
			GlobalConditionMethod ();
			AssemblyConditionMethod ();
			TypeConditionMethod ();
			MethodConditionMethod ();
			_ = FieldConditionField;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.1",
			"ret",
		})]
		static bool GlobalConditionMethod ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"ret",
		})]
		static bool AssemblyConditionMethod ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.1",
			"ret",
		})]
		static bool TypeConditionMethod ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldc.i4.0",
			"ret",
		})]
		static bool MethodConditionMethod ()
		{
			throw new NotImplementedException ();
		}

		[Kept]
		static readonly bool FieldConditionField;

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.1",
			"stsfld System.Boolean Mono.Linker.Tests.Cases.FeatureSettings.FeatureSubstitutionsNested::FieldConditionField",
			"ret"
		})]
		static FeatureSubstitutionsNested ()
		{
		}
	}
}