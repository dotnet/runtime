// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.Reflection
{
	[ExpectedNoWarnings]
	public class TypeDelegator
	{
		public static void Main ()
		{
			TestTypeUsedWithDelegator ();
			TestNullValue ();
			TestNoValue ();
			TestDataFlowPropagation ();
			TestDataFlowOfUnannotated ();
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
			typeDelegator.RequiresAll ();
		}

		[Kept]
		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			var typeDelegator = new System.Reflection.TypeDelegator (noValue);
			typeDelegator.RequiresAll ();
		}

		[Kept]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestDataFlowPropagation (
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type typeWithPublicMethods = null)
		{
			var typeDelegator = new System.Reflection.TypeDelegator (typeWithPublicMethods);
			typeDelegator.RequiresPublicMethods (); // Should not warn
			typeDelegator.RequiresPublicFields (); // Should warn
		}

		[Kept]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestDataFlowOfUnannotated (Type unknownType = null)
		{
			var typeDelegator = new System.Reflection.TypeDelegator (unknownType);
			unknownType.RequiresPublicMethods ();
		}
	}
}
