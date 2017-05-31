using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods
{
	class ClassUsedFromConcreteTypeHasInterfaceMethodRemoved {
		public static void Main ()
		{
			A a = new A ();
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
			void Foo ();
		}
	}
}
