// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
			TestMultiple (typeof (TestType));
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
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


		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
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

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
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

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicMethods), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors), "y '" + nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicConstructors) + "' i")]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestPublicMethods (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			type.RequiresPublicMethods ();
			type.RequiresNonPublicMethods (); // Warns
			type.RequiresNone ();
			type.RequiresPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicMethods), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicMethods))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestNonPublicMethods (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] Type type)
		{
			type.RequiresPublicMethods (); // Warns
			type.RequiresNonPublicMethods ();
			type.RequiresNone ();
			type.RequiresNonPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicFields), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestPublicFields (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
			type.RequiresPublicFields ();
			type.RequiresNonPublicFields (); // Warns
			type.RequiresNone ();
			type.RequiresPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicConstructors))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestNonPublicFields (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
		{
			type.RequiresPublicFields (); // Warns
			type.RequiresNonPublicFields ();
			type.RequiresNone ();
			type.RequiresNonPublicConstructors (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicNestedTypes), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresInterfaces), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.Interfaces))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestPublicNestedTypes (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type type)
		{
			type.RequiresPublicNestedTypes ();
			type.RequiresNonPublicNestedTypes (); // Warns
			type.RequiresNone ();
			type.RequiresInterfaces (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicNestedTypes), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresInterfaces), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.Interfaces))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestNonPublicNestedTypes (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] Type type)
		{
			type.RequiresPublicNestedTypes (); // Warns
			type.RequiresNonPublicNestedTypes ();
			type.RequiresNone ();
			type.RequiresInterfaces (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicProperties), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicProperties))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestPublicProperties (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type type)
		{
			type.RequiresPublicProperties ();
			type.RequiresNonPublicProperties (); // Warns
			type.RequiresNone ();
			type.RequiresPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicProperties), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicProperties))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicFields), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestNonPublicProperties (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
		{
			type.RequiresPublicProperties (); // Warns
			type.RequiresNonPublicProperties ();
			type.RequiresNone ();
			type.RequiresNonPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicEvents), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicEvents))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicFields), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestPublicEvents (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] Type type)
		{
			type.RequiresPublicEvents ();
			type.RequiresNonPublicEvents (); // Warns
			type.RequiresNone ();
			type.RequiresPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicEvents), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicEvents))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicFields), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicFields))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
		static void TestNonPublicEvents (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicEvents)] Type type)
		{
			type.RequiresPublicEvents (); // Warns
			type.RequiresNonPublicEvents ();
			type.RequiresNone ();
			type.RequiresNonPublicFields (); // Warns
			type.RequiresAll (); // Warns
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicNestedTypes), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.PublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresNonPublicNestedTypes), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicNestedTypes))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresAll), nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.All))]
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

		static void RequiresMultiplePrivates (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicFields)] Type type)
		{
		}

		static void RequiresSomePublic (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces | DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.NonPublicNestedTypes | DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
		}

		[ExpectedWarning ("IL2067",
			nameof (RequiresMultiplePrivates),
			nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicMethods),
			nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicFields))]
		[ExpectedWarning ("IL2067",
			nameof (RequiresSomePublic),
			nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.Interfaces),
			nameof (DynamicallyAccessedMemberTypes) + "." + nameof (DynamicallyAccessedMemberTypes.NonPublicNestedTypes))]
		static void TestMultiple (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicConstructors)] Type type)
		{
			RequiresMultiplePrivates (type);
			RequiresSomePublic (type);
		}

		class TestType { }
	}
}
