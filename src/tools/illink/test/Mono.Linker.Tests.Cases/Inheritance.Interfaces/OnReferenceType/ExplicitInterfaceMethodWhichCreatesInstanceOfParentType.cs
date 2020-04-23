using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class ExplicitInterfaceMethodWhichCreatesInstanceOfParentType
	{
		public static void Main ()
		{
			IFoo b = new B ();
			b.Method ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class B : IFoo
		{
			[Kept]
			public void Method ()
			{
			}
		}

		class C : IFoo
		{
			void IFoo.Method () { new C (); }
		}
	}
}