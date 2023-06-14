// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	class UnsafeAccessor
	{
		public static void Main ()
		{
			ConstructorAccess.Test ();
		}

		class ConstructorAccess
		{
			[Kept]
			class ConstructorTarget
			{
				[Kept]
				public ConstructorTarget () { }
			}

			[Kept]
			[KeptAttributeAttribute(typeof(UnsafeAccessorAttribute))]
			[UnsafeAccessor(UnsafeAccessorKind.Constructor)]
			extern static ConstructorTarget InvokeDefaultConstructor ();

			[Kept]
			static void CallDefaultConstructor()
			{
				InvokeDefaultConstructor ();
			}

			[Kept]
			public static void Test ()
			{
				CallDefaultConstructor ();
			}
		}
	}
}
