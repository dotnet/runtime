using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkXml.Dependencies.EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupCompileBefore ("Base.dll",
		new[] { "Dependencies/EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod/Base.cs" })]
	[SetupCompileBefore ("Library1.dll",
		new[] { "Dependencies/EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod/Library1.cs" },
		new[] { "Base.dll" },
		resources: new object[] { "Dependencies/EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod/Library1.xml" })]
	[SetupCompileBefore ("Library2.dll",
		new[] { "Dependencies/EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod/Library2.cs" },
		new[] { "Base.dll" },
		addAsReference: false)]
	[IgnoreDescriptors (false)]

	[KeptMemberInAssembly ("Library1.dll", typeof (Library1), "VirtualMethodFromBase()")]
	[KeptMemberInAssembly ("Library1.dll", typeof (Library1Secondary), "VirtualMethodFromBase()")]

	// Library1's embedded link xml will preserve the Library2 type.  Because Library2 shares a base class with Library1
	// Library2's override should be kept as well
	[KeptMemberInAssembly ("Library2.dll", "Mono.Linker.Tests.Cases.LinkXml.Dependencies.EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod.Library2", "VirtualMethodFromBase()")]
	public class EmbeddedLinkXmlPreservesAdditionalAssemblyWithOverriddenMethod
	{
		public static void Main ()
		{
			var tmp = new Library1 ();
			tmp.VirtualMethodFromBase ();
		}
	}
}