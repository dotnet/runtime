using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[TestCaseRequirements (TestRunCharacteristics.SupportsDefaultInterfaceMethods, "Requires support for default interface methods")]
	class SimpleDefaultInterfaceMethod
	{
		public static void Main ()
		{
#if SUPPORTS_DEFAULT_INTERFACE_METHODS
			((IBasic) new Basic ()).DoSomething ();
#endif
		}

#if SUPPORTS_DEFAULT_INTERFACE_METHODS
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
