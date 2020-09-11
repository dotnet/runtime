using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
#if !NETCOREAPP
	[IgnoreTestCase ("Requires support for default interface methods")]
#endif
	class SimpleDefaultInterfaceMethod
	{
		public static void Main ()
		{
#if NETCOREAPP
			((IBasic) new Basic ()).DoSomething ();
#endif
		}

#if NETCOREAPP
		[Kept]
		interface IBasic
		{
			[Kept]
			void DoSomething ()
			{
				DoOtherThing ();
			}

			void UnusedMethodWithDefaultImplementation ()
			{
			}

			[Kept]
			sealed void DoOtherThing ()
			{
			}

			sealed void UnusedNonvirtualMethod ()
			{
			}
		}

		interface IUnusedInterface
		{
			void UnusedDefaultImplementation ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IBasic))]
		class Basic : IBasic, IUnusedInterface
		{
			[Kept]
			public Basic () { }
		}
#endif
	}
}
