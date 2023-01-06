using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileWithTypePreserve1.xml", "ILLink.Descriptors.xml")]
	[SetupCompileBefore ("library.dll",
		new string[] { "Dependencies/EmbeddedLinkXmlFileWithTypePreserve_Lib.cs" },
		resources: new object[] {
			new string[] { "Dependencies/EmbeddedLinkXmlFileWithTypePreserve2.xml", "ILLink.Descriptors.xml" }
	})]
	[IgnoreDescriptors (false)]
	[KeptAssembly ("library.dll")]
	public class EmbeddedLinkXmlFileWithTypePreserve
	{
		public static void Main ()
		{
			EmbeddedLinkXmlFileWithTypePreserve_Lib.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class PreservedType
		{
			[Kept]
			static bool field;

			[Kept]
			static void Method () { }
		}
	}
}
