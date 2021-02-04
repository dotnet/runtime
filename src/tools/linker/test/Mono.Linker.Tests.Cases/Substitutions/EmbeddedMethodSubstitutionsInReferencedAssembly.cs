using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Substitutions.Dependencies;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileBefore ("library.dll",
		new string[] { "Dependencies/ReferencedMethod.cs" },
		resources: new object[] {
			new string[] { "Dependencies/ReferencedMethod.xml", "ILLink.Substitutions.xml" }
		})]
	[IgnoreSubstitutions (false)]
	[ExpectedInstructionSequenceOnMemberInAssembly ("library.dll", typeof (ReferencedMethod), "ConvertToThrowMethod()", new[] {
		"ldstr",
		"newobj",
		"throw"
	})]
	public class EmbeddedMethodSubstitutionsInReferencedAssembly
	{

		public static void Main ()
		{
			ReferencedMethod.ConvertToThrowMethod ();
		}
	}
}
