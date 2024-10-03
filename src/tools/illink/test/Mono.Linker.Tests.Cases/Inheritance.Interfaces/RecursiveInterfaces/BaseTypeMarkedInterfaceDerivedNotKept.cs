// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.RecursiveInterfaces
{
	class BaseTypeMarkedInterfaceDerivedNotKept
	{
		[Kept]
		public static void Main ()
		{
			IFoo foo = new UsedDerived ();
			foo.Method ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		[KeptMember (".ctor()")]
		abstract class BaseType : IFoo
		{
			[Kept]
			public abstract void Method ();
		}

		[Kept]
		[KeptBaseType (typeof (BaseType))]
		[KeptMember (".ctor()")]
		class UsedDerived : BaseType
		{
			[Kept]
			public override void Method ()
			{
			}
		}

		class UnusedDerived : BaseType
		{
			public override void Method ()
			{
			}
		}

		[Kept]
		interface IFoo
		{
			[Kept]
			void Method ();
		}
	}
}
