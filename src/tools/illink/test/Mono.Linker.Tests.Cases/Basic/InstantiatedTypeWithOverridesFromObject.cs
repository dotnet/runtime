using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	public class InstantiatedTypeWithOverridesFromObject
	{
		public static void Main ()
		{
			var f = new Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo
		{
			[Kept]
			~Foo ()
			{
				// Finalize shouldn't be empty
				DoCleanupStuff ();
			}

			[Kept]
			void DoCleanupStuff ()
			{
			}

			[Kept]
			public override bool Equals (object obj)
			{
				return false;
			}

			[Kept]
			public override string ToString ()
			{
				return null;
			}

			[Kept]
			public override int GetHashCode ()
			{
				return 0;
			}
		}
	}
}