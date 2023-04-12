using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("InitField.xml")]
	[CreatedMember (".cctor()")]
	public class InitField
	{
		[Kept]
		static readonly bool BoolValue;

		public static void Main ()
		{
			TestField_1 ();
		}

		[Kept]
		static bool TestField_1 ()
		{
			return BoolValue;
		}
	}
}
