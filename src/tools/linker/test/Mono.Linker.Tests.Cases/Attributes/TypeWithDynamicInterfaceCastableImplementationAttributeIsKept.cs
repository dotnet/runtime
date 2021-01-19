// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
#if !NETCOREAPP
	[IgnoreTestCase ("Requires support for default interface methods")]
#endif
	public class TypeWithDynamicInterfaceCastableImplementationAttributeIsKept
	{
		public static void Main ()
		{
#if NETCOREAPP
			Foo foo = new Foo ();
			GetBar (foo).Bar ();
			IReferenced baz = GetBaz (foo);
#endif
		}

#if NETCOREAPP
		[Kept]
		private static IReferencedAndCalled GetBar (object obj)
		{
			return (IReferencedAndCalled) obj;
		}

		[Kept]
		private static IReferenced GetBaz (object obj)
		{
			return (IReferenced) obj;
		}
#endif
	}

#if NETCOREAPP
	[Kept]
	[KeptMember (".ctor()")]
	class Foo : IDynamicInterfaceCastable
	{
		[Kept]
		public RuntimeTypeHandle GetInterfaceImplementation (RuntimeTypeHandle interfaceType)
		{
			if (interfaceType.Equals (typeof (IReferencedInIDynamicInterfaceCastableType).TypeHandle)) {
				return typeof (IReferencedInIDynamicInterfaceCastableTypeImpl).TypeHandle;
			}
			return default;
		}

		[Kept]
		public bool IsInterfaceImplemented (RuntimeTypeHandle interfaceType, bool throwIfNotImplemented)
		{
			return interfaceType.Equals (typeof (IReferencedInIDynamicInterfaceCastableType).TypeHandle);
		}
	}

	[Kept]
	interface IReferencedAndCalled
	{
		[Kept]
		void Bar ();
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IReferencedAndCalled))]
	[DynamicInterfaceCastableImplementation]
	interface IReferencedAndCalledImpl : IReferencedAndCalled
	{
		[Kept]
		void IReferencedAndCalled.Bar () { }
	}

	[Kept]
	interface IReferenced
	{
		void Baz ();
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IReferenced))]
	[DynamicInterfaceCastableImplementation]
	interface IReferencedImpl : IReferenced
	{
		void IReferenced.Baz () { }
	}

	interface IUnreferenced
	{
		void Frob () { }
	}

	[DynamicInterfaceCastableImplementation]
	interface IUnreferencedImpl : IUnreferenced
	{
		void IUnreferenced.Frob () { }
	}

	[Kept]
	interface IReferencedInIDynamicInterfaceCastableType
	{
		void Foo () { }
	}

	[Kept]
	[KeptAttributeAttribute (typeof (DynamicInterfaceCastableImplementationAttribute))]
	[KeptInterface (typeof (IReferencedInIDynamicInterfaceCastableType))]
	[DynamicInterfaceCastableImplementation]
	interface IReferencedInIDynamicInterfaceCastableTypeImpl : IReferencedInIDynamicInterfaceCastableType
	{
		void IReferencedInIDynamicInterfaceCastableType.Foo () { }
	}
#endif
}
