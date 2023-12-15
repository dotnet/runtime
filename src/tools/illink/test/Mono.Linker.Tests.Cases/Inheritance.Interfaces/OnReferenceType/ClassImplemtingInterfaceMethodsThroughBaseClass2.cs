using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	class ClassImplemtingInterfaceMethodsThroughBaseClass2
	{
		public static void Main ()
		{
			new B ();
			IFoo i = null;
			i.Foo ();
		}

		interface IFoo
		{
			[Kept]
			void Foo ();
		}

		[KeptMember (".ctor()")]
		class B
		{
			public void Foo ()
			{
			}
		}

		class A : B, IFoo
		{
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
