using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedTypeWithPreserveNothingAndPreserveMembers {
		public static void Main ()
		{
		}

		[Kept]
		class Unused {
			[Kept]
			public int Field1;

			private int Field2;

			[Kept]
			public void Method1 ()
			{
			}

			private void Method2 ()
			{
			}
		}
	}
}