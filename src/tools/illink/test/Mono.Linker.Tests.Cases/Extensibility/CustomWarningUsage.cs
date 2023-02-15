using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("CustomWarning.dll", new[] { "Dependencies/CustomWarning.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "CustomWarning,CustomWarning.dll")]
	[SetupLinkerArgument ("--notrimwarn")]
	[ExpectedNoWarnings]
	public class CustomWarningUsage
	{
		[ExpectedWarning ("IL2026", "--RUCMethod--", ProducedBy = ProducedBy.Analyzer)]
		public static void Main ()
		{
			new KnownTypeThatShouldWarn ();
			RUCMethod (); // Warning suppressed by --notrimwarn
		}

		[ExpectedWarning ("IL6200", "custom warning on type")]
		[Kept]
		[KeptMember (".ctor()")]
		public class KnownTypeThatShouldWarn
		{
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
		[RequiresUnreferencedCode ("--RUCMethod--")]
		static void RUCMethod () { }
	}
}