using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	public class NeverInstantiatedTypeWithOverridesFromObject {
		public static void Main ()
		{
			typeof (Foo).ToString ();
		}

		[Kept]
		class Foo {
			[Kept] // Future improvements will allow this to be removed
			~Foo ()
			{
				// Finalize shouldn't be empty
				DoCleanupStuff ();
			}

			[Kept]
			void DoCleanupStuff ()
			{
			}

			[Kept] // Future improvements will allow this to be removed
			public override bool Equals (object obj)
			{
				return false;
			}

			[Kept] // Future improvements will allow this to be removed
			public override string ToString ()
			{
				return null;
			}

			[Kept] // Future improvements will allow this to be removed
			public override int GetHashCode ()
			{
				return 0;
			}
		}
	}
}