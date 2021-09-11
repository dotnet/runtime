using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class UnusedDefaultInterfaceImplementation
	{
		public static void Main ()
		{
#if SUPPORTS_DEFAULT_INTERFACE_METHODS
			((IFoo) new Foo ()).InterfaceMethod ();
#endif
		}

#if SUPPORTS_DEFAULT_INTERFACE_METHODS
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
#endif
	}
}
