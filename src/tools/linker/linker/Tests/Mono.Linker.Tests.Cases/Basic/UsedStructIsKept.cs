using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UsedStructIsKept {
		public static void Main()
		{
			A a;
			a.MethodVerifiedByKeptMember ();
		}

		[Kept]
		// This KeptMember is here to make sure the test framework's support of KeptMember on value types is working correctly
		[KeptMember ("MethodVerifiedByKeptMember()")]
		struct A {
			public void MethodVerifiedByKeptMember ()
			{
			}
		}
	}
}
