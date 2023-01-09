using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	public class UsedOverrideOfVirtualMethodIsKept
	{
		public static void Main ()
		{
			var tmp = new B ();
			tmp.Call ();
		}

		[KeptMember (".ctor()")]
		class Base
		{
			[Kept]
			public virtual void Call ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class B : Base
		{
			[Kept]
			public override void Call ()
			{
			}
		}
	}
}