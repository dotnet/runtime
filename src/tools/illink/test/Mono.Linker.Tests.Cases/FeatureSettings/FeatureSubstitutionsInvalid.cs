using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.FeatureSettings
{
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsInvalid.xml")]
	[SetupLinkerArgument ("--feature", "NoValueFeature", "true")]
	[LogContains ("FeatureSubstitutionsInvalid.xml'. Feature 'NoValueFeature' does not specify a 'featurevalue' attribute")]
	[LogContains ("FeatureSubstitutionsInvalid.xml'. Unsupported non-boolean feature definition 'NonBooleanFeature'")]
	[LogContains ("FeatureSubstitutionsInvalid.xml'. Unsupported value for featuredefault attribute")]
	[LogContains ("warning IL2012: Could not find field 'NonExistentField' on type 'Mono.Linker.Tests.Cases.FeatureSettings.FeatureSubstitutionsInvalid.Foo'")]
	[LogContains ("warning IL2009: Could not find method 'NonExistentMethod' on type 'Mono.Linker.Tests.Cases.FeatureSettings.FeatureSubstitutionsInvalid.Foo'")]
	[NoLinkedOutput]
	public class FeatureSubstitutionsInvalid
	{
		public static void Main ()
		{
			NoValueFeatureMethod ();
			NonBooleanFeatureMethod ();
			InvalidDefaultFeatureMethod ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"throw"
		})]
		static bool NoValueFeatureMethod () => throw null;

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"throw"
		})]
		static bool NonBooleanFeatureMethod () => throw null;

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldnull",
			"throw"
		})]
		static bool InvalidDefaultFeatureMethod () => throw null;

		class Foo
		{
			int _field;
			int NoSetter { get; }
			int NoGetter { set { _field = value; } }
		}
	}
}
