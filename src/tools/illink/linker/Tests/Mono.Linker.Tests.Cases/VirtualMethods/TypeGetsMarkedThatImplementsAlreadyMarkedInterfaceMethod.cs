using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class TypeGetsMarkedThatImplementsAlreadyMarkedInterfaceMethod {
		public static void Main ()
		{
			IFoo i = new A ();
			i.Foo ();
		}

		interface IFoo {
			[Kept]
			void Foo ();
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class B : IFoo {
			[Kept]
			public void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class A : IFoo {
			[Kept]
			public void Foo ()
			{
				new B (); /*this will cause us to mark B, but will we be smart enough to realize B.Foo implements the already marked IFoo.Foo?*/
			}
		}
	}
}