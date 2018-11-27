using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor {
	public class UnusedTypeWithPreserveFields {
		public static void Main ()
		{
		}

		interface IFoo {
			void Foo ();
		}

		interface IBar {
			void Bar ();
		}

		[Kept]
		class A : IBar, IFoo {
			public void Foo ()
			{
			}

			public void Bar ()
			{
			}
		}
	}
}