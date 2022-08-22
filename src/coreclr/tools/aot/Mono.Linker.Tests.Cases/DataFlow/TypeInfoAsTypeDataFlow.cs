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
	class TypeInfoAsTypeDataFlow
	{
		public static void Main ()
		{
			TestNoAnnotations (typeof (TestType).GetTypeInfo ());
			TestWithAnnotations (typeof (TestType).GetTypeInfo ());
			TestWithNull ();
			TestWithNoValue ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestNoAnnotations (TypeInfo t)
		{
			t.AsType ().RequiresPublicMethods ();
			t.AsType ().RequiresNone ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestWithAnnotations ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TypeInfo t)
		{
			t.AsType ().RequiresPublicMethods ();
			t.AsType ().RequiresPublicFields ();
			t.AsType ().RequiresNone ();
		}

		static void TestWithNull ()
		{
			TypeInfo t = null;
			t.AsType ().RequiresPublicMethods ();
		}

		static void TestWithNoValue ()
		{
			Type t = null;
			Type noValueType = Type.GetTypeFromHandle (t.TypeHandle);
			TypeInfo noValue = noValueType.GetTypeInfo ();
			noValue.AsType ().RequiresPublicMethods ();
		}

		class TestType { }
	}
}
