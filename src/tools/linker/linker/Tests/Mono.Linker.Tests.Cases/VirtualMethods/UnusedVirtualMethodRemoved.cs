using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class UnusedVirtualMethodRemoved {
		public static void Main ()
		{
			new Base ().Call ();
		}

		[KeptMember (".ctor()")]
		class Base {
			[Kept]
			public virtual void Call ()
			{
			}
		}

		class B : Base {
			public override void Call ()
			{
			}
		}
	}
}