using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType
{
	class StructUsedFromConcreteTypeHasInterfaceMethodRemoved
	{
		public static void Main ()
		{
			A a = new A ();
			a.Foo ();
		}

		[Kept]
		struct A : IFoo
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
