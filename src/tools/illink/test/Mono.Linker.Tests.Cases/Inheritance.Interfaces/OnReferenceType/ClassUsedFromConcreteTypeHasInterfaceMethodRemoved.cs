using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	class ClassUsedFromConcreteTypeHasInterfaceMethodRemoved
	{
		public static void Main ()
		{
			A a = new A ();
			a.Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class A : IFoo
		{
			[Kept]
			public void Foo ()
			{
			}
		}

		public interface IFoo
		{
			void Foo ();
		}
	}
}
