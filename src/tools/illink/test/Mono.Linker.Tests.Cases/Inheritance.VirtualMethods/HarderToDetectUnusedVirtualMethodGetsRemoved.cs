using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	class HarderToDetectUnusedVirtualMethodGetsRemoved
	{
		public static void Main ()
		{
			new Base ().Call ();
		}

		static void DeadCode ()
		{
			new B ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Base
		{
			[Kept]
			public virtual void Call ()
			{
			}
		}

		class B : Base
		{
			public override void Call ()
			{
			}
		}
	}
}