// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	class BaseProvidesInterfaceMethod
	{
		[Kept]
		public static void Main ()
		{
			CallMethod<Derived> ();
		}

		[Kept]
		static void CallMethod<T> () where T : IFoo
		{
			T.Method ();
		}
		[Kept]
		interface IFoo
		{
			[Kept]
			static abstract int Method ();
		}

		[Kept]
		class Base
		{
			[Kept]
			public static int Method () => 0;
		}

		[KeptInterface (typeof (IFoo))]
		[KeptBaseType (typeof (Base))]
		[KeptMember ("Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods.BaseProvidesInterfaceMethod.IFoo.Method()")]
		// Compiler generates private explicit implementation that calls Base.Method()
		class Derived : Base, IFoo
		{ }
	}
}
