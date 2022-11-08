using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	class InterfaceNeededOnUnrelatedInterfaceList
	{
		[Kept]
		static Foo s_foo;

		static void Main ()
		{
			object ob = new Bar ();
			((IBar) ob).Frob ();
			s_foo = null;
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Frob ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		interface IBar : IFoo
		{
		}

		[Kept]
		class Foo : IBar
		{
			void IFoo.Frob ()
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IBar))]
		[KeptInterface (typeof (IFoo))]
		class Bar : IBar
		{
			[Kept]
			public Bar () { }

			[Kept]
			void IFoo.Frob ()
			{
			}
		}
	}
}
