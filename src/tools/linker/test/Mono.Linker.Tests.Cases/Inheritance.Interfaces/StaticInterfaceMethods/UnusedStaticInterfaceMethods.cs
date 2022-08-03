// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	public class UnusedStaticInterfaceMethods
	{
		[Kept]
		public static void Main ()
		{
			Foo.KeepFoo ();
			KeepIFooStaticUnused (null);
			((IFooStaticUsed) null).InstanceVirtual ();
			((IFooStaticUsed) null).InstanceAbstract ();
			Type t = typeof (FooVariantCastable);
			CallGetIntStaticUsed<FooVariantCastable> ();
		}

		[Kept]
		static void CallGetIntStaticUsed<T> () where T : IFooStaticUsed
		{
			T.StaticAbstract ();
		}


		[Kept]
		interface IFooStaticUnused
		{
			int InstanceVirtualUnused () => 0;
			int InstanceAbstractUnused ();
			static abstract int StaticAbstractUnused ();
		}

		[Kept]
		interface IFooStaticUsed
		{
			[Kept]
			int InstanceVirtual () => 0;
			[Kept]
			int InstanceAbstract ();
			[Kept]
			static abstract int StaticAbstract ();
		}

		[Kept]
		static void KeepIFooStaticUnused (IFooStaticUnused x) { }

		[Kept]
		class Foo : IFooStaticUnused, IFooStaticUsed
		{
			public int InstanceVirtualUnused () => 1;
			public int InstanceAbstractUnused () => 1;
			public static int StaticAbstractUnused () => 1;
			[Kept]
			public static void KeepFoo () { }
			public int InstanceVirtual () => 1;
			public int InstanceAbstract () => 0;
			public static int StaticAbstract () => 0;
		}

		[Kept]
		[KeptInterface (typeof (IFooStaticUsed))]
		[KeptInterface (typeof (IFooStaticUnused))]
		class FooVariantCastable : IFooStaticUnused, IFooStaticUsed
		{
			public int InstanceVirtualUnused () => 1;
			public int InstanceAbstractUnused () => 1;
			public static int StaticAbstractUnused () => 1;
			public int InstanceVirtual () => 1;
			[Kept]
			public int InstanceAbstract () => 0;
			[Kept]
			public static int StaticAbstract () => 0;
		}
	}
}
