using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	public class UnusedInterfaceTypeOnTypeWithPreserveAllIsKept {
		public static void Main ()
		{
		}

		[Kept]
		interface IFoo {
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Bar : IFoo {
		}
	}
}