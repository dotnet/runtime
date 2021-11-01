// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
