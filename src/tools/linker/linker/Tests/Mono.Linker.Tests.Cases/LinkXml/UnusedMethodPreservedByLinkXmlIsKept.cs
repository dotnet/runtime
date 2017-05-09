using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedMethodPreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
			[Kept]
			private void PreservedMethod ()
			{
			}

			private void NotPreservedMethod ()
			{
			}
		}
	}
}