using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	[KeptDelegateCacheField ("0")]
	[KeptDelegateCacheField ("1")]
	class UsedEventIsKept {
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
		public class Foo {
			[Kept]
			[KeptBaseType (typeof(MulticastDelegate))]
			[KeptMember (".ctor(System.Object,System.IntPtr)")]
			[KeptMember ("Invoke()")]
			[KeptMember ("BeginInvoke(System.AsyncCallback,System.Object)")]
			[KeptMember ("EndInvoke(System.IAsyncResult)")]
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
