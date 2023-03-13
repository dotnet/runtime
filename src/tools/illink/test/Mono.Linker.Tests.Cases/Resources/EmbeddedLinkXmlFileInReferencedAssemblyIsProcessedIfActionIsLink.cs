using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileBefore (
		"EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.dll",
		new[] { "Dependencies/EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.cs" },
		resources: new object[] { "Dependencies/EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.xml" })]
	[SetupLinkerAction ("link", "EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1")]
	[IgnoreDescriptors (false)]

	[RemovedResourceInAssembly ("EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.dll", "EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.xml")]
	[KeptMemberInAssembly ("EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.dll", typeof (EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1), "Unused()")]
	public class EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink
	{
		public static void Main ()
		{
			EmbeddedLinkXmlFileInReferencedAssemblyIsProcessedIfActionIsLink_Lib1.Used ();
		}
	}
}