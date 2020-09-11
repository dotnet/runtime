using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
#if !NETCOREAPP
	[IgnoreTestCase ("Requires support for default interface methods")]
#endif
	class GenericDefaultInterfaceMethods
	{
		public static void Main ()
		{
#if NETCOREAPP
			((IFoo<int>) new Bar ()).Method (12);
			((IFoo<int>) new Baz ()).Method (12);
#endif
		}

#if NETCOREAPP
		[Kept]
		interface IFoo<T>
		{
			[Kept]
			void Method (T x);

		}

		[Kept]
		[KeptInterface (typeof (IFoo<>), "T")]
		interface IBar<T> : IFoo<T>
		{
			[Kept]
			void IFoo<T>.Method (T x)
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IBar<int>))]
		[KeptInterface (typeof (IFoo<int>))]
		class Bar : IBar<int>
		{
			[Kept]
			public Bar () { }
		}

		[Kept]
		[KeptInterface (typeof (IFoo<object>))]
		interface IBaz : IFoo<object>
		{
			[Kept]
			void IFoo<object>.Method (object o)
			{
			}
		}

		[Kept]
		[KeptInterface (typeof (IBaz))]
		[KeptInterface (typeof (IFoo<object>))]
		class Baz : IBaz
		{
			[Kept]
			public Baz ()
			{
			}
		}
#endif
	}
}
