using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Substitutions.Dependencies;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileBefore ("library.dll",
		new string[] { "Dependencies/ReferencedField.cs" },
		resources: new object[] {
			new string[] { "Dependencies/ReferencedField.xml", "ILLink.Substitutions.xml" }
		})]
	[IgnoreSubstitutions (false)]
	[ExpectedInstructionSequenceOnMemberInAssembly ("library.dll", typeof (ReferencedField), ".cctor()", new[] {
		"nop",
		"ldc.i4.0",
		"pop",
		"ldc.i4.1",
		"stsfld System.Boolean Mono.Linker.Tests.Cases.Substitutions.Dependencies.ReferencedField::BoolValue",
		"ret"
	})]
	public class EmbeddedFieldSubstitutionsInReferencedAssembly
	{

		public static void Main ()
		{
			var _ = ReferencedField.BoolValue;
		}
	}
}
