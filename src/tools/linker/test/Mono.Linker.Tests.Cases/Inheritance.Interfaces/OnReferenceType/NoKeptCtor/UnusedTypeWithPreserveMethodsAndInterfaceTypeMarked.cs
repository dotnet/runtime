using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor {
	public class UnusedTypeWithPreserveMethodsAndInterfaceTypeMarked {
		public static void Main ()
		{
			// We'll mark one interface in code and one via xml, the end result should be the same
			var tmp = typeof (IFoo).ToString ();
		}

		[Kept]
		interface IFoo {
			void Foo ();
		}

		[Kept]
		interface IBar {
			void Bar ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		[KeptInterface (typeof (IBar))]
		class A : IBar, IFoo {
			[Kept]
			public void Foo ()
			{
			}

			[Kept]
			public void Bar ()
			{
			}
		}
	}
}