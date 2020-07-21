using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
#if NETCOREAPP
	class DefaultInterfaceMethodCallIntoClass
	{
		public static void Main ()
		{
			((IBase) new Derived ()).Frob ();
		}

		[Kept]
		interface IBase
		{
			[Kept]
			void Frob ();
		}

		[Kept]
		[KeptInterface (typeof (IBase))]
		interface IDerived : IBase
		{
			[Kept]
			void IBase.Frob ()
			{
				Actual ();
			}

			[Kept]
			void Actual ();
		}

		[Kept]
		[KeptInterface (typeof (IDerived))]
		[KeptInterface (typeof (IBase))]
		class Derived : IDerived
		{
			[Kept]
			public Derived () { }

			[Kept]
			public void Actual () { }
		}
	}
#endif
}
