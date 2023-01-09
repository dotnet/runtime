using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerTrimMode ("link")]
	// Need to skip due to `Runtime critical type System.Reflection.CustomAttributeData not found` failure
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
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