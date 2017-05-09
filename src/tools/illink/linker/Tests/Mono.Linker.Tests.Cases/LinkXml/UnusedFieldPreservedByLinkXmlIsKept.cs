using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedFieldPreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
			[Kept]
			private int _preserved;

			private int _notPreserved;
		}
	}
}