using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses
{
	public class UsedOverrideOfAbstractMethodIsKept
	{
		public static void Main ()
		{
			var tmp = new B ();
			tmp.Call ();
		}

		[KeptMember (".ctor()")]
		abstract class Base
		{
			[Kept]
			public abstract void Call ();
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