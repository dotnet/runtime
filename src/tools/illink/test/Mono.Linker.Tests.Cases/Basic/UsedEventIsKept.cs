using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	[KeptDelegateCacheField ("0", nameof (Tmp_Bar))]
	class UsedEventIsKept
	{
		public static void Main ()
		{
			var tmp = new Foo ();

			tmp.Bar += Tmp_Bar;
			tmp.Fire ();
			tmp.Bar -= Tmp_Bar;
		}

		[Kept]
		private static void Tmp_Bar ()
		{
		}

		[KeptMember (".ctor()")]
		public class Foo
		{
			[Kept]
			[KeptBaseType (typeof (MulticastDelegate))]
			[KeptMember (".ctor(System.Object,System.IntPtr)")]
			[KeptMember ("Invoke()")]
			public delegate void CustomDelegate ();

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event CustomDelegate Bar;

			[Kept]
			public void Fire ()
			{
				Bar ();
			}
		}
	}
}
