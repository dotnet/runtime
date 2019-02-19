using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedNestedTypePreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Unused {
		}
	}
}