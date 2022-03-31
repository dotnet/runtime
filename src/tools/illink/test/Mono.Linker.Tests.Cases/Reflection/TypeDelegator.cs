// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection
{
	public class TypeDelegator
	{
		public static void Main ()
		{
			TestTypeUsedWithDelegator ();
			TestNullValue ();
			TestNoValue ();
		}

		[Kept]
		static class TypeUsedWithDelegator
		{
			[Kept]
			public static void Method () { }

			public static void UnrelatedMethod () { }
		}

		[Kept]
		static void TestTypeUsedWithDelegator ()
		{
			_ = new System.Reflection.TypeDelegator (typeof (TypeUsedWithDelegator)).GetMethod ("Method");
		}

		[Kept]
		static void TestNullValue ()
		{
			var typeDelegator = new System.Reflection.TypeDelegator (null);
			RequireAll (typeDelegator);
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var typeDelegator = new System.Reflection.TypeDelegator (noValue);
			RequireAll (typeDelegator);
		}

		[Kept]
		public static void RequireAll (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			System.Reflection.TypeDelegator t
		)
		{ }
	}
}
