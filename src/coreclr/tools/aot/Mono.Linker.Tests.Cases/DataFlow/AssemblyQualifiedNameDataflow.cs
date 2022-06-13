// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class AssemblyQualifiedNameDataflow
	{
		static void Main ()
		{
			TestPublicParameterlessConstructor ();
			TestPublicConstructors ();
			TestConstructors ();
			TestUnqualifiedTypeNameWarns ();
			TestNull ();
			TestMultipleValues ();
			TestUnknownValue ();
			TestNoValue ();
			TestObjectGetTypeValue ();
		}

		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (RequireNonPublicConstructors))]
		static void TestPublicParameterlessConstructor ()
		{
			string type = GetTypeWithPublicParameterlessConstructor ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[ExpectedWarning ("IL2072", nameof (RequireNonPublicConstructors))]
		static void TestPublicConstructors ()
		{
			string type = GetTypeWithPublicConstructors ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[ExpectedWarning ("IL2072", nameof (RequirePublicParameterlessConstructor))]
		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors))]
		static void TestConstructors ()
		{
			string type = GetTypeWithNonPublicConstructors ().AssemblyQualifiedName;
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[ExpectedWarning ("IL2105",
			"Type 'System.Invalid.TypeName' was not found in the caller assembly nor in the base library. " +
			"Type name strings used for dynamically accessing a type should be assembly qualified.",
			ProducedBy = ProducedBy.Trimmer)]
		static void TestUnqualifiedTypeNameWarns ()
		{
			RequirePublicConstructors ("System.Invalid.TypeName");
		}

		static void TestNull ()
		{
			Type type = null;
			RequirePublicConstructors (type.AssemblyQualifiedName); // Null should not warn - we know it's going to fail at runtime
		}

		[ExpectedWarning ("IL2072", nameof (RequirePublicConstructors), nameof (GetTypeWithNonPublicConstructors))]
		[ExpectedWarning ("IL2062", nameof (RequirePublicConstructors))]
		static void TestMultipleValues (int p = 0, object[] o = null)
		{
			Type type = p switch {
				0 => GetTypeWithPublicConstructors (),
				1 => GetTypeWithNonPublicConstructors (), // Should produce warning IL2072 due to mismatch annotation
				2 => null, // Should be ignored
				_ => (Type) o[0] // This creates an unknown value - should produce warning IL2062
			};

			RequirePublicConstructors (type.AssemblyQualifiedName);
		}

		[ExpectedWarning ("IL2062", nameof (RequirePublicConstructors))]
		static void TestUnknownValue (object[] o = null)
		{
			string unknown = ((Type) o[0]).AssemblyQualifiedName;
			RequirePublicConstructors (unknown);
			RequireNothing (unknown); // shouldn't warn
		}

		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			// t.TypeHandle throws at runtime so don't warn here.
			RequirePublicConstructors (noValue.AssemblyQualifiedName);
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		class AnnotatedType
		{
		}

		static void TestObjectGetTypeValue (AnnotatedType instance = null)
		{
			string type = instance.GetType ().AssemblyQualifiedName;
			// Currently Object.GetType is unimplemented in the analyzer, but
			// this still shouldn't warn.
			RequirePublicConstructors (type);
			RequireNothing (type);
		}

		private static void RequirePublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			string type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			string type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			string type)
		{
		}

		private static void RequireNothing (string type)
		{
		}


		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private static Type GetTypeWithPublicParameterlessConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		private static Type GetTypeWithNonPublicConstructors ()
		{
			return null;
		}
	}
}
