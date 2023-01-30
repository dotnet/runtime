using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	public class NeverInstantiatedTypeWithOverridesFromObject
	{
		public static void Main ()
		{
			typeof (Foo).ToString ();
		}

		[Kept]
		class Foo
		{
			~Foo ()
			{
				// Finalize shouldn't be empty
				DoCleanupStuff ();
			}

			void DoCleanupStuff ()
			{
			}

			public override bool Equals (object obj)
			{
				return false;
			}

			public override string ToString ()
			{
				return null;
			}

			public override int GetHashCode ()
			{
				return 0;
			}
		}
	}
}