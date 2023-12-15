using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	class ClassUsedFromInterfaceHasInterfaceMethodKept
	{
		public static void Main ()
		{
			IFoo a = new A ();
			a.Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class A : IFoo
		{
			[Kept]
			public void Foo ()
			{
			}
		}

		[Kept]
		public interface IFoo
		{
			[Kept]
			void Foo ();
		}
	}
}
