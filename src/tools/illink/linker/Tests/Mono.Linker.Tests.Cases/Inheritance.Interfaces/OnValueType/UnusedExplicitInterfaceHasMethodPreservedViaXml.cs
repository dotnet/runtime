using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnValueType {
	public class UnusedExplicitInterfaceHasMethodPreservedViaXml {
		public static void Main ()
		{
			IFoo i = new A ();
			i.Foo ();
		}

		[Kept]
		interface IFoo {
			[Kept]
			void Foo ();
		}

		[Kept]
		interface IBar {
			[Kept]
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		[KeptInterface (typeof (IBar))]
		struct A : IBar, IFoo {
			[Kept]
			void IFoo.Foo ()
			{
			}

			[Kept]
			void IBar.Bar ()
			{
			}
		}
	}
}