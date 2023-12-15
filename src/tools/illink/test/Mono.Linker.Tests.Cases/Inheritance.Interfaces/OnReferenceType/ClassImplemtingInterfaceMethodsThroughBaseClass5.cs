using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	class ClassImplemtingInterfaceMethodsThroughBaseClass5
	{
		public static void Main ()
		{
			new A ();
		}

		interface IFoo
		{
			void Foo ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			public void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (B))]
		class A : B, IFoo
		{
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
