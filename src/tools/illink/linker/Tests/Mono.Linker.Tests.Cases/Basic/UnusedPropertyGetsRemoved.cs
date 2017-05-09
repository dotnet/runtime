using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class UnusedPropertyGetsRemoved {
		public static void Main ()
		{
			new UnusedPropertyGetsRemoved.B ().Method ();
		}

		[KeptMember(".ctor()")]
		class B {
			public int Unused { get; set; }

			[Kept]
			public void Method ()
			{
			}
		}
	}
}