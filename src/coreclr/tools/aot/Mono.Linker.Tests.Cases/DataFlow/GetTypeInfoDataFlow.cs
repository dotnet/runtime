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
	class GetTypeInfoDataFlow
	{
		public static void Main ()
		{
			TestNoAnnotations (typeof (TestType));
			TestWithAnnotations (typeof (TestType));
			TestWithNull ();
			TestWithNoValue ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestNoAnnotations (Type t)
		{
			t.GetTypeInfo ().RequiresPublicMethods ();
			t.GetTypeInfo ().RequiresNone ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestWithAnnotations ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
		{
			t.GetTypeInfo ().RequiresPublicMethods ();
			t.GetTypeInfo ().RequiresPublicFields ();
			t.GetTypeInfo ().RequiresNone ();
		}

		static void TestWithNull ()
		{
			Type t = null;
			t.GetTypeInfo ().RequiresPublicMethods ();
		}

		static void TestWithNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			noValue.GetTypeInfo ().RequiresPublicMethods ();
		}

		class TestType { }
	}
}
