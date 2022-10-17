using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FeatureSettings
{
	[SetupLinkerSubstitutionFile ("FeatureSubstitutions.xml")]
	[SetupLinkerArgument ("--feature", "OptionalFeature", "false")]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	public class FeatureSubstitutions
	{
		static bool IsOptionalFeatureEnabled {
			get;
		}

		[ExpectedInstructionSequence (new[] {
			"nop",
			"call System.Void Mono.Linker.Tests.Cases.FeatureSettings.FeatureSubstitutions::TestOptionalFeature()",
			"nop",
			"ldc.i4.1",
			"pop",
			"ret",
		})]
		public static void Main ()
		{
			TestOptionalFeature ();
			_ = IsDefaultFeatureEnabled;
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ldc.i4.0",
			"stloc.0",
			"ldloc.0",
			"brfalse.s il_6",
			"nop",
			"call System.Void Mono.Linker.Tests.Cases.FeatureSettings.FeatureSubstitutions::UseFallback()",
			"nop",
			"nop",
			"ret",
		})]
		static void TestOptionalFeature ()
		{
			if (IsOptionalFeatureEnabled) {
				UseOptionalFeature ();
			} else {
				UseFallback ();
			}
		}

		static void UseOptionalFeature ()
		{
		}

		[Kept]
		static void UseFallback ()
		{
		}

		static bool IsDefaultFeatureEnabled {
			get => throw new NotImplementedException ();
		}
	}
}
