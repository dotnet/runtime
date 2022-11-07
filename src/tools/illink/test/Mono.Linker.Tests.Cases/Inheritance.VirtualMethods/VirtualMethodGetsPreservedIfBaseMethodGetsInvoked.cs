using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	class VirtualMethodGetsPreservedIfBaseMethodGetsInvoked
	{
		public static void Main ()
		{
			new A ();
			new B ().Foo ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			[Kept]
			public virtual void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (B))]
		class A : B
		{
			[KeptBy (typeof (A), "OverrideOnInstantiatedType")]
			public override void Foo ()
			{
			}
		}
	}
}
