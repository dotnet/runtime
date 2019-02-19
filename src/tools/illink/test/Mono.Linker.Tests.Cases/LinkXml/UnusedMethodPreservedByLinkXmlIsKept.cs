using System.Collections.Generic;
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

			[Kept]
			private void PreservedMethod2 (int arg1, string arg2)
			{
			}

			[Kept]
			private void PreservedMethod3 ()
			{
			}

			[Kept]
			private void PreservedMethod4 (List<int> arg1)
			{
			}

			private void NotPreservedMethod ()
			{
			}
		}
	}
}