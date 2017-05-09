using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class UnusedMethodGetsRemoved {
		public static void Main ()
		{
			new UnusedMethodGetsRemoved.B ().Method ();
		}

		[KeptMember (".ctor()")]
		class B {
			public void Unused ()
			{
			}

			[Kept]
			public void Method ()
			{
			}
		}
	}
}