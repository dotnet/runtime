using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType
{
	public class StructImplementingInterfaceMethodsNested
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

		[Kept]
		[KeptInterface (typeof (IFoo))]
		interface IBar : IFoo
		{
			void Bar ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		[KeptInterface (typeof (IBar))]
		struct A : IBar
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
