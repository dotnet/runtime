using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileResource ("Dependencies/EmbeddedSubstitutions.xml", "ILLink.Substitutions.xml")]
	[IgnoreSubstitutions (false)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.Substitutions.xml")]
	public class EmbeddedSubstitutions
	{
		public static void Main ()
		{
			ConvertToThrowMethod ();
			ConvertToThrowMethod2 ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldstr 'Linked away'",
			"newobj System.Void System.NotSupportedException::.ctor(System.String)",
			"throw",
		})]
		public static void ConvertToThrowMethod ()
		{
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldstr 'Linked away'",
			"newobj System.Void System.NotSupportedException::.ctor(System.String)",
			"throw",
		})]
		public static void ConvertToThrowMethod2 ()
		{
		}
	}
}
