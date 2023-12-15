using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UnusedTypeWithPreserveNothingAndPreserveMembers.xml")]
	class UnusedTypeWithPreserveNothingAndPreserveMembers
	{
		public static void Main ()
		{
		}

		[Kept]
		class Unused
		{
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