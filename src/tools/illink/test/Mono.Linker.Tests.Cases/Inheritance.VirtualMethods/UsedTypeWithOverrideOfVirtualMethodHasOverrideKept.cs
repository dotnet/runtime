using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	public class UsedTypeWithOverrideOfVirtualMethodHasOverrideKept
	{
		public static void Main ()
		{
			new B ();
			new Base ().Call ();
		}

		[KeptMember (".ctor()")]
		class Base
		{
			[Kept]
			public virtual void Call ()
			{
			}
		}

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