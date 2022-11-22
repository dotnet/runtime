using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("InitFieldExistingCctor.xml")]
	[SetupCompileArgument ("/optimize+")]
	public class InitFieldExistingCctor
	{
		[Kept]
		[ExpectedInstructionSequence (new[] {
				"ldc.i4.s 0xa",
				"pop",
				"ldc.i4 0x5",
				"stsfld System.Int32 Mono.Linker.Tests.Cases.Substitutions.InitFieldExistingCctor::IntValue",
				"ret"
			})]
		static InitFieldExistingCctor ()
		{
			IntValue = 10;
		}

		[Kept]
		static readonly int IntValue;

		public static void Main ()
		{
			TestField_1 ();
		}

		[Kept]
		static int TestField_1 ()
		{
			return IntValue;
		}
	}
}
