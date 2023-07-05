// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class TypeBaseTypeUseViaReflection
	{
		public static void Main ()
		{
			KnownType_Derived.Test ();
		}

		[Kept]
		class KnownType_Base
		{
			[Kept]
			public KnownType_Base () { }

			[Kept]
			private static void UsedViaReflection () { }

			private static void Unused () { }
		}

		[Kept]
		[KeptBaseType (typeof (KnownType_Base))]
		class KnownType_Derived : KnownType_Base
		{
			[Kept]
			public static void Test ()
			{
				typeof (KnownType_Derived).BaseType.GetMethod ("UsedViaReflection", BindingFlags.NonPublic | BindingFlags.Static);
				typeof (KnownType_Derived).BaseType.GetConstructor (Type.EmptyTypes);
			}
		}
	}
}
