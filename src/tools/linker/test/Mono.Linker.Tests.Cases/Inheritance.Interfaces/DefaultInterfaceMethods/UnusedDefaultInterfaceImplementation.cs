using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
#if NETCOREAPP
	class UnusedDefaultInterfaceImplementation
	{
		public static void Main ()
		{
			((IFoo) new Foo ()).InterfaceMethod ();
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void InterfaceMethod ();
		}

		interface IDefaultImpl : IFoo
		{
			void IFoo.InterfaceMethod ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IDefaultImpl
		{
			[Kept]
			public Foo () { }

			[Kept]
			public void InterfaceMethod ()
			{
			}
		}
	}
#endif
}
