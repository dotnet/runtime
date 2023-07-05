using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UsedInterfaceIsKept
	{
		public static void Main ()
		{
			A a = new A ();
			var t = typeof (I).ToString ();
		}

		[Kept]
		[KeptInterface (typeof (I))]
		[KeptMember (".ctor()")]
		class A : I
		{
		}

		[Kept]
		interface I
		{
		}
	}
}
