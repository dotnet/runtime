using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedTypePreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	class UnusedTypePreservedByLinkXmlIsKeptUnusedType {
	}
}