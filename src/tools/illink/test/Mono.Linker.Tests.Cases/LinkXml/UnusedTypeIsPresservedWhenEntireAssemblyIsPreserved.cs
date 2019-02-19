using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {

	[KeptMember(".ctor()")]
	class UnusedTypeIsPresservedWhenEntireAssemblyIsPreserved {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Unused {
			[Kept]
			void Foo ()
			{
			}
		}
	}
}
