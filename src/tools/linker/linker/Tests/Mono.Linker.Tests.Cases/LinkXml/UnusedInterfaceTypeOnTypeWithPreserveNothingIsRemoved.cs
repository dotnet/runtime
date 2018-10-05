using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	public class UnusedInterfaceTypeOnTypeWithPreserveNothingIsRemoved {
		public static void Main ()
		{
		}

		interface IFoo {
		}
		
		[Kept]
		class Bar : IFoo {
		}
	}
}