using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[NoLinkedOutput]
	[SetupLinkerSubstitutionFile ("FeatureSubstitutionsInvalid.xml")]
	[SetupLinkerArgument ("--feature", "NoValueFeature", "true")]
	[LogContains ("Feature NoValueFeature does not specify a 'featurevalue' attribute")]
	[LogContains ("warning IL2012: Could not find field 'NonExistentField' in type 'Mono.Linker.Tests.Cases.Substitutions.FeatureSubstitutionsInvalid/Foo'")]
	[LogContains ("warning IL2009: Could not find method 'NonExistentMethod' in type 'Mono.Linker.Tests.Cases.Substitutions.FeatureSubstitutionsInvalid/Foo'")]
	[LogContains ("warning IL2016: Could not find event 'NonExistentEvent' in type 'Mono.Linker.Tests.Cases.Substitutions.FeatureSubstitutionsInvalid/Foo'")]
	[LogContains ("warning IL2017: Could not find property 'NonExistentProperty' in type 'Mono.Linker.Tests.Cases.Substitutions.FeatureSubstitutionsInvalid/Foo'")]
	[LogContains ("warning IL2018: Could not find the get accessor of property 'NoGetter' in type 'Mono.Linker.Tests.Cases.Substitutions.FeatureSubstitutionsInvalid/Foo'")]
	[LogContains ("warning IL2019: Could not find the set accessor of property 'NoSetter' in type 'Mono.Linker.Tests.Cases.Substitutions.FeatureSubstitutionsInvalid/Foo'")]
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

		[Kept]
		class Foo
		{
			int _field;
			int NoSetter { get; }
			int NoGetter { set { _field = value; } }
		}
	}
}
