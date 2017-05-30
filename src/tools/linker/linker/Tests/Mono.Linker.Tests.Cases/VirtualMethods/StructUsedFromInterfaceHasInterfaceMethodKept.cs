using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods
{
	class StructUsedFromInterfaceHasInterfaceMethodKept {
		public static void Main ()
		{
			IFoo a = new A ();
			a.Foo ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		struct A : IFoo {
			[Kept]
			public void Foo ()
			{
			}
		}

		[Kept]
		public interface IFoo {
			[Kept]
			void Foo ();
		}
	}
}
