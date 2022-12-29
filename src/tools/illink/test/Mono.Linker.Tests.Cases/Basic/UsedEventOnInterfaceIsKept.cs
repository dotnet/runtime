using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	[KeptDelegateCacheField ("0", nameof (Bar_Ping))]
	class UsedEventOnInterfaceIsKept
	{
		static void Main ()
		{
			IFoo bar = new Bar ();
			IFoo jar = new Jar ();

			bar.Ping += Bar_Ping;
		}

		[Kept]
		private static void Bar_Ping (object sender, EventArgs e)
		{
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			event EventHandler Ping;
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Bar : IFoo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Ping;
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Jar : IFoo
		{
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Ping;
		}
	}
}
