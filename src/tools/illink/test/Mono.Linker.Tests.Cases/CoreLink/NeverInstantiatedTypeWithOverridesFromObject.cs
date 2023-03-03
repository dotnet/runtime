using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerTrimMode ("link")]
	// Need to skip due to `Runtime critical type System.Reflection.CustomAttributeData not found` failure
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
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