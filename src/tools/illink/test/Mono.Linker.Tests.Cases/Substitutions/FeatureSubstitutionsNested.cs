using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsGlobalTrue.xml")]
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsGlobalFalse.xml")]
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsNested.xml")]
	[SetupLinkerArgument ("--feature", "GlobalCondition", "true")]
	[SetupLinkerArgument ("--feature", "AssemblyCondition", "false")]
	[SetupLinkerArgument ("--feature", "TypeCondition", "true")]
	[SetupLinkerArgument ("--feature", "MethodCondition", "false")]
	[SetupLinkerArgument ("--feature", "FieldCondition", "true")]
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
			"stsfld",
			"ret"
		})]
		static FeatureSubstitutionsNested ()
		{
		}
	}
}