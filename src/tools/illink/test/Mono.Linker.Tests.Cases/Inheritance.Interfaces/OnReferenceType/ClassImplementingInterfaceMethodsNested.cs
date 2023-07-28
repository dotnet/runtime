using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class ClassImplementingInterfaceMethodsNested
	{
		public static void Main ()
		{
			IFoo i = new A ();
			i.Foo ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Foo ();
		}

		interface IBar : IFoo
		{
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
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