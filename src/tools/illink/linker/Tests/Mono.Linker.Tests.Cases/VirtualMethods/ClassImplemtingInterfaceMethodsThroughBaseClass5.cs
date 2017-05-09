using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class ClassImplemtingInterfaceMethodsThroughBaseClass5 {
		public static void Main ()
		{
			new A ();
		}

		[Kept]
		interface IFoo {
			void Foo ();
		}

		[KeptMember (".ctor()")]
		class B {
			public void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (B))]
		[KeptInterface (typeof (IFoo))]
		class A : B, IFoo {
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
