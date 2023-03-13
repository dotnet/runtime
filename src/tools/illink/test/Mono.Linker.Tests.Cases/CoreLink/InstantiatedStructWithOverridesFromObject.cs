using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerTrimMode ("link")]
	// Need to skip due to `Runtime critical type System.Reflection.CustomAttributeData not found` failure
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	public class InstantiatedStructWithOverridesFromObject
	{
		public static void Main ()
		{
			HelperToUseFoo (new Foo ());
			HelperToUseMethodsOnObject ();
		}

		[Kept]
		static void HelperToUseFoo (Foo f)
		{
		}

		[Kept]
		static void HelperToUseMethodsOnObject ()
		{
			var o = new object ();
			var e = o.Equals (null);
			var s = o.ToString ();
			var c = o.GetHashCode ();
		}

		[Kept]
		struct Foo
		{
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