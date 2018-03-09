using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources {
	[SetupCompileBefore (
		"library.dll",
		new [] { "Dependencies/EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1.cs" },
		resources: new [] {"Dependencies/EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1_NotMatchingName.xml"})]
	[IncludeBlacklistStep (true)]

	[KeptResourceInAssembly ("library.dll", "EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1_NotMatchingName.xml")]
	[RemovedMemberInAssembly ("library.dll", typeof (EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1), "Unused()")]
	public class EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly {
		public static void Main ()
		{
			EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1.Used ();
		}
	}
}