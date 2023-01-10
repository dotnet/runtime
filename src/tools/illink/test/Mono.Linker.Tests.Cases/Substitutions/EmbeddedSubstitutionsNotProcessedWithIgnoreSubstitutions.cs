using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileResource ("Dependencies/EmbeddedSubstitutionsNotProcessedWithIgnoreSubstitutions.xml", "ILLink.Substitutions.xml")]
	[IgnoreSubstitutions (true)]
	[StripSubstitutions (false)]
	[KeptResource ("ILLink.Substitutions.xml")]
	public class EmbeddedSubstitutionsNotProcessedWithIgnoreSubstitutions
	{
		public static void Main ()
		{
			ConvertToThrowMethod ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ret"
		})]
		public static void ConvertToThrowMethod ()
		{
		}
	}
}
