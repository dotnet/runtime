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

			tmp.AddRemoveFireCalled += Tmp_Bar;
			tmp.Fire ();
			tmp.AddRemoveFireCalled -= Tmp_Bar;

			tmp.AddCalled += (s, e) => { };
			tmp.RemoveCalled -= (s, e) => { };
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
			public event CustomDelegate AddRemoveFireCalled;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler AddCalled;

			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler RemoveCalled;

			// Invoke without any Add or Remove calls will fail with a nullref, so nothing needs to be kept
			[KeptBackingField]
			public event EventHandler InvokeCalled;

			[Kept]
			public void Fire ()
			{
				AddRemoveFireCalled ();
				InvokeCalled (null, null);
			}
		}
	}
}
