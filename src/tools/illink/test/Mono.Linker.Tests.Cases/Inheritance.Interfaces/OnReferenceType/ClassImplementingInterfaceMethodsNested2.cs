using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class ClassImplementingInterfaceMethodsNested2
	{
		public static void Main ()
		{
			IBar b = new A ();
			IFoo f = b;
			f.Foo ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Foo ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		interface IBar : IFoo
		{
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IBar))]
		[KeptInterface (typeof (IFoo))]
		class A : IBar
		{
			[Kept]
			public void Foo ()
			{
			}

			public void Bar ()
			{
			}
		}
	}
}