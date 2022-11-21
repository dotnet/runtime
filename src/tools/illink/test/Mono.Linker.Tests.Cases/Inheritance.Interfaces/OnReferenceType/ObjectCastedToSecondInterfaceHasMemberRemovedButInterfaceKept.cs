using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	public class ObjectCastedToSecondInterfaceHasMemberRemovedButInterfaceKept
	{
		public static void Main ()
		{
			IFoo i = new A ();
			i.Foo ();
			var b = (IBar) i;
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Foo ();
		}

		[Kept]
		interface IBar
		{
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		[KeptInterface (typeof (IBar))]
		class A : IBar, IFoo
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