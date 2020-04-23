using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsInvalid.xml")]
	[SetupLinkerArgument ("--feature", "NoValueFeature", "true")]
	[LogContains ("Feature NoValueFeature does not specify a \"featurevalue\" attribute")]
	public class FeatureSubstitutionsInvalid
	{
		public static void Main ()
		{
			NoValueFeatureMethod ();
			NonBooleanFeatureMethod ();
			BooleanFeatureMethod ();
		}

		[Kept]
		static void NoValueFeatureMethod ()
		{
		}

		[Kept]
		static void NonBooleanFeatureMethod ()
		{
		}

		[Kept]
		static void BooleanFeatureMethod ()
		{
		}
	}
}
