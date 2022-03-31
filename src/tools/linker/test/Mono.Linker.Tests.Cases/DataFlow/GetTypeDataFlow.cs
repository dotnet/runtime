// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class GetTypeDataFlow
	{
		public static void Main ()
		{
			TestPublicParameterlessConstructor ();
			TestPublicConstructors ();
			TestConstructors ();
			TestNull ();
			TestNoValue ();
			TestUnknownType ();

			TestTypeNameFromParameter (null);
			TestTypeNameFromField ();

			TestMultipleConstantValues ();
			TestMultipleMixedValues ();

			TestStringEmpty ();

			// TODO:
			// Test multi-value returns
			//    Type.GetType over a constant and a param
			//    Type.GetType over two params
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		static void TestPublicParameterlessConstructor ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicParameterlessConstructor ());
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors ();
			type.RequiresNone ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		static void TestPublicConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicConstructors ());
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors ();
			type.RequiresNone ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		static void TestConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithNonPublicConstructors ());
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors ();
			type.RequiresNone ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Type.GetType) + "(String)")]
		static void TestNull ()
		{
			// Warns about the return value of GetType, even though this throws at runtime.
			Type.GetType (null).RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Type.GetType) + "(String)")]
		static void TestNoValue ()
		{
			Type t = null;
			string noValue = t.AssemblyQualifiedName;
			// Warns about the return value of GetType, even though AssemblyQualifiedName throws at runtime.
			Type.GetType (noValue).RequiresAll ();
		}

		[ExpectedWarning ("IL2057", nameof (GetType))]
		static void TestUnknownType ()
		{
			Type type = Type.GetType (GetStringUnkownType ());
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		static void TestTypeNameFromParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			string typeName)
		{
			Type.GetType (typeName).RequiresPublicConstructors ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static string _typeNameWithPublicParameterlessConstructor;

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		static void TestTypeNameFromField ()
		{
			Type.GetType (_typeNameWithPublicParameterlessConstructor).RequiresPublicConstructors ();
		}

		static int _switchOnField;

		static void TestMultipleConstantValues ()
		{
			string typeName = null;
			switch (_switchOnField) {
			case 0: // valid
				typeName = "Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow";
				break;
			case 1: // null
				typeName = null;
				break;
			case 2: // invalid
				typeName = "UnknownType";
				break;
			case 3: // invalid second
				typeName = "AnotherUnknownType";
				break;
			}

			Type.GetType (typeName);
		}

		static void TestStringEmpty ()
		{
			Type.GetType (string.Empty);
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), nameof (Type.GetType))]
		[ExpectedWarning ("IL2057", "System.Type.GetType(String)")]
		static void TestMultipleMixedValues ()
		{
			string typeName = null;
			switch (_switchOnField) {
			case 0:
				typeName = GetStringTypeWithPublicParameterlessConstructor ();
				break;
			case 1:
				typeName = GetStringTypeWithNonPublicConstructors ();
				break;
			case 2:
				typeName = "Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow";
				break;
			case 3:
				typeName = GetStringUnkownType ();
				break;
			}

			Type.GetType (typeName).RequiresNonPublicConstructors ();
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private static string GetStringTypeWithPublicParameterlessConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static string GetStringTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		private static string GetStringTypeWithNonPublicConstructors ()
		{
			return null;
		}

		private static string GetStringUnkownType ()
		{
			return null;
		}
	}
}
