using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FeatureSettings
{
	[SetupLinkerSubstitutionFile ("FeatureSubstitutions.xml")]
	[SetupLinkerArgument ("--feature", "OptionalFeature", "false")]
	public class FeatureSubstitutions
	{
		[Kept]
		static bool IsOptionalFeatureEnabled {
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.0",
				"ret",
			})]
			get;
		}

		public static void Main ()
		{
			TestOptionalFeature ();
			_ = IsDefaultFeatureEnabled;
		}

		[Kept]
		[ExpectBodyModified]
		[ExpectedInstructionSequence (new[] {
			"call",
			"brfalse",
			"call",
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

		[Kept]
		static bool IsDefaultFeatureEnabled {
			[Kept]
			[ExpectedInstructionSequence (new[] {
				"ldc.i4.1",
				"ret",
			})]
			get => throw new NotImplementedException ();
		}
	}
}
