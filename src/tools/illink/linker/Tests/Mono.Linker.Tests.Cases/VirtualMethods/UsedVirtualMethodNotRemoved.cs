using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	public class UsedVirtualMethodNotRemoved {
		public static void Main ()
		{
			new B ();
			new Base ().Call ();
		}

		[KeptMember (".ctor()")]
		class Base {
			[Kept]
			public virtual void Call ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (Base))]
		class B : Base {
			[Kept]
			public override void Call ()
			{
			}
		}
	}
}