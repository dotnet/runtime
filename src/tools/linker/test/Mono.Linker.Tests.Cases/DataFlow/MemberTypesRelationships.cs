// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class MemberTypesRelationships
	{
		public static void Main ()
		{
			TestPublicParameterlessConstructor (typeof (TestType));
			TestPublicConstructors (typeof (TestType));
			TestNonPublicConstructors (typeof (TestType));
			TestPublicMethods (typeof (TestType));
			TestNonPublicMethods (typeof (TestType));
			TestPublicFields (typeof (TestType));
			TestNonPublicFields (typeof (TestType));
			TestPublicNestedTypes (typeof (TestType));
			TestNonPublicNestedTypes (typeof (TestType));
			TestPublicProperties (typeof (TestType));
			TestNonPublicProperties (typeof (TestType));
			TestPublicEvents (typeof (TestType));
			TestNonPublicEvents (typeof (TestType));
			TestInterfaces (typeof (TestType));
			TestAll (typeof (TestType));
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicParameterlessConstructor (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors (); // Warns
			type.RequiresNonPublicConstructors (); // Warns
			type.RequiresNone ();
			type.RequiresPublicMethods (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicConstructors (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors (); // Warns
			type.RequiresNone ();
			type.RequiresPublicMethods (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestNonPublicConstructors (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
		{
			type.RequiresPublicParameterlessConstructor (); // Warns
			type.RequiresPublicConstructors (); // Warns
			type.RequiresNonPublicConstructors ();
			type.RequiresNone ();
			type.RequiresPublicMethods (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicMethods (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			type.RequiresPublicMethods ();
			type.RequiresNonPublicMethods (); // Warns
			type.RequiresNone ();
			type.RequiresPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestNonPublicMethods (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
		{
			type.RequiresPublicMethods (); // Warns
			type.RequiresNonPublicMethods ();
			type.RequiresNone ();
			type.RequiresNonPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicFields (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
			type.RequiresPublicFields ();
			type.RequiresNonPublicFields (); // Warns
			type.RequiresNone ();
			type.RequiresPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestNonPublicFields (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
		{
			type.RequiresPublicFields (); // Warns
			type.RequiresNonPublicFields ();
			type.RequiresNone ();
			type.RequiresNonPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresInterfaces))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicNestedTypes (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type type)
		{
			type.RequiresPublicNestedTypes ();
			type.RequiresNonPublicNestedTypes (); // Warns
			type.RequiresNone ();
			type.RequiresInterfaces (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresInterfaces))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestNonPublicNestedTypes (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] Type type)
		{
			type.RequiresPublicNestedTypes (); // Warns
			type.RequiresNonPublicNestedTypes ();
			type.RequiresNone ();
			type.RequiresInterfaces (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicProperties))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicProperties (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		{
			type.RequiresPublicProperties ();
			type.RequiresNonPublicProperties (); // Warns
			type.RequiresNone ();
			type.RequiresPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicProperties))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestNonPublicProperties (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
		{
			type.RequiresPublicProperties (); // Warns
			type.RequiresNonPublicProperties ();
			type.RequiresNone ();
			type.RequiresNonPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicEvents))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestPublicEvents (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] Type type)
		{
			type.RequiresPublicEvents ();
			type.RequiresNonPublicEvents (); // Warns
			type.RequiresNone ();
			type.RequiresPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicEvents))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestNonPublicEvents (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicEvents)] Type type)
		{
			type.RequiresPublicEvents (); // Warns
			type.RequiresNonPublicEvents ();
			type.RequiresNone ();
			type.RequiresNonPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestInterfaces (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] Type type)
		{
			type.RequiresInterfaces ();
			type.RequiresNone ();
			type.RequiresPublicNestedTypes (); // Warns
			type.RequiresNonPublicNestedTypes (); // Warns
			type.RequiresAll (); // Warns
		}

		static void TestAll (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
			type.RequiresAll ();
			type.RequiresNone ();
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors ();
			type.RequiresPublicMethods ();
			type.RequiresNonPublicMethods ();
			type.RequiresPublicFields ();
			type.RequiresNonPublicFields ();
			type.RequiresPublicNestedTypes ();
			type.RequiresNonPublicNestedTypes ();
			type.RequiresPublicProperties ();
			type.RequiresNonPublicProperties ();
			type.RequiresPublicEvents ();
			type.RequiresNonPublicEvents ();
			type.RequiresInterfaces ();
		}

		class TestType { }
	}
}
