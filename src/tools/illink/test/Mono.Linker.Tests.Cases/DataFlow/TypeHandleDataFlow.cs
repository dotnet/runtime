// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class TypeHandleDataFlow
	{
		public static void Main ()
		{
			TestTypeOf ();
			TestTypeOfFromGeneric<TestType> ();
			TestGetTypeHandle ();
			TestGetTypeHandleFromGeneric<TestType> ();
			TestUnsupportedPatterns (typeof (TestType));
			TestNull ();
			TestNoValue ();
		}

		[ExpectedWarning ("IL2026", "--" + nameof (TestTypeWithRUCOnMembers.PublicMethodWithRUC) + "--")]
		static void TestTypeOf ()
		{
			// In IL this is a type handle (ldtoken) followed by call to GetTypeFromHandle
			// In analyzer this is a special operation node - so also interesting but doesn't involve type handle

			// Validate that we propagate type - without the propagation this should warn
			typeof (TestType).RequiresNonPublicMethods ();

			// Validate that we correctly propagate the type as a known type, so that we can actually mark
			// specific methods on it.
			typeof (TestTypeWithRUCOnMembers).RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestTypeOfFromGeneric<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresPublicFields ();
		}

		[ExpectedWarning ("IL2026", "--" + nameof (TestTypeWithRUCOnMembers.PublicMethodWithRUC) + "--")]
		static void TestGetTypeHandle ()
		{
			Type.GetTypeFromHandle (typeof (TestType).TypeHandle).RequiresNonPublicMethods ();
			Type.GetTypeFromHandle (typeof (TestTypeWithRUCOnMembers).TypeHandle).RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestGetTypeHandleFromGeneric<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			Type.GetTypeFromHandle (typeof (T).TypeHandle).RequiresPublicMethods ();
			Type.GetTypeFromHandle (typeof (T).TypeHandle).RequiresPublicFields ();
		}

		[ExpectedWarning ("IL2072", nameof (Type.GetTypeFromHandle), nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestUnsupportedPatterns ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type typeWithMethods)
		{
			Type.GetTypeFromHandle (typeWithMethods.TypeHandle).RequiresPublicMethods ();
		}

		static void TestNull ()
		{
			Type type = null;
			// This should not warn - we know the type is null. Null is ignored since we know it will fail at runtime
			// and thus there are no actual requirements on the rest of the app to make it fail that way.
			Type.GetTypeFromHandle (type.TypeHandle).RequiresPublicMethods ();
		}

		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			// t.TypeHandle throws at runtime so don't warn here.
			Type.GetTypeFromHandle (noValue.TypeHandle).RequiresPublicMethods ();
		}

		class TestType { }

		class TestTypeWithRUCOnMembers
		{
			[RequiresUnreferencedCode ("--" + nameof (PublicMethodWithRUC) + "--")]
			public static void PublicMethodWithRUC () { }

			[RequiresUnreferencedCode ("--" + nameof (PrivateMethodWithRUC) + "--")]
			void PrivateMethodWithRUC () { }
		}
	}
}
