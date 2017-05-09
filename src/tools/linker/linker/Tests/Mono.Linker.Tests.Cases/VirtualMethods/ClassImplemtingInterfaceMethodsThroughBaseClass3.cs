using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class ClassImplemtingInterfaceMethodsThroughBaseClass3 {
		public static void Main ()
		{
			new B ().Foo ();
		}

		interface IFoo {
			void Foo ();
		}

		[KeptMember (".ctor()")]
		class B {
			[Kept]
			public void Foo ()
			{
			}
		}

		class A : B, IFoo {
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
