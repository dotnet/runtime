using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class VirtualMethodGetsStrippedIfImplementingMethodGetsInvokedDirectly {
		public static void Main ()
		{
			new A ().Foo ();
		}

		[KeptMember (".ctor()")]
		class B {
			[Kept] // TODO: Would be nice to be removed
			public virtual void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (B))]
		class A : B {
			[Kept]
			public override void Foo ()
			{
			}
		}
	}
}
