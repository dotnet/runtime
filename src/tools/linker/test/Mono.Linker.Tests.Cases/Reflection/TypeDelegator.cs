// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
