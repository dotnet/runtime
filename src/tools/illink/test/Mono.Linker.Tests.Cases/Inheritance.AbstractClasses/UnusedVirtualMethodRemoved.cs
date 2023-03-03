using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses
{
	public class UnusedVirtualMethodRemoved
	{
		public static void Main ()
		{
			var tmp = new B ();
		}

		[KeptMember (".ctor()")]
		abstract class Base
		{
			public virtual void Call ()
			{
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class B : Base
		{
			public override void Call ()
			{
			}
		}
	}
}