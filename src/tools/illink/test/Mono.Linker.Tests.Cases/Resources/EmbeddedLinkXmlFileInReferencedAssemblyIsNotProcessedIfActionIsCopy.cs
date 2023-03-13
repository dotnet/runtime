using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileBefore (
		"EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.dll",
		new[] { "Dependencies/EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.cs" },
		resources: new object[] { "Dependencies/EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.xml" })]
	[SetupLinkerAction ("copy", "EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1")]
	[IgnoreDescriptors (false)]

	[KeptResourceInAssembly ("EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.dll", "EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.xml")]
	[KeptMemberInAssembly ("EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.dll", typeof (EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1), "Unused()")]
	public class EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy
	{
		public static void Main ()
		{
			EmbeddedLinkXmlFileInReferencedAssemblyIsNotProcessedIfActionIsCopy_Lib1.Used ();
		}
	}
}