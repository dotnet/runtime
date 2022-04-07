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
	[RemovedResourceInAssembly ("test.exe", "ResourceFileRemoveWhenTrue.txt")]
	[KeptResource ("ResourceFileRemoveWhenFalse.txt")]
	public class FeatureSubstitutionsNested
	{
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.1",
			"pop",
			"ldc.i4.0",
			"pop",
			"ldc.i4.1",
			"pop",
			"ldc.i4.0",
			"pop",
			"ldsfld System.Boolean Mono.Linker.Tests.Cases.FeatureSettings.FeatureSubstitutionsNested::FieldConditionField",
			"pop",
			"ret",
		})]
		public static void Main ()
		{
			GlobalConditionMethod ();
			AssemblyConditionMethod ();
			TypeConditionMethod ();
			MethodConditionMethod ();
			_ = FieldConditionField;
		}

		static bool GlobalConditionMethod ()
		{
			throw new NotImplementedException ();
		}

		static bool AssemblyConditionMethod ()
		{
			throw new NotImplementedException ();
		}

		static bool TypeConditionMethod ()
		{
			throw new NotImplementedException ();
		}

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