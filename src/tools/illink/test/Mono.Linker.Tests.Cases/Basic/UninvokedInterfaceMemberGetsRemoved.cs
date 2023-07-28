using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UninvokedInterfaceMemberGetsRemoved
	{
		public static void Main ()
		{
			I i = new B ();
			var tmp = i.ToString ();
		}

		[Kept]
		interface I
		{
			void Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface (typeof (I))]
		class B : I
		{
			public void Method ()
			{
			}
		}
	}
}