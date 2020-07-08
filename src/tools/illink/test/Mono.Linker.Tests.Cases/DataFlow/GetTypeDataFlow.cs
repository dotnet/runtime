// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	public class GetTypeDataFlow
	{
		public static void Main ()
		{
			TestPublicParameterlessConstructor ();
			TestPublicConstructors ();
			TestConstructors ();
			TestUnknownType ();

			TestTypeNameFromParameter (null);
			TestTypeNameFromField ();

			TestMultipleConstantValues ();
			TestMultipleMixedValues ();

			// TODO:
			// Test multi-value returns
			//    Type.GetType over a constant and a param
			//    Type.GetType over two params
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		static void TestPublicParameterlessConstructor ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicParameterlessConstructor ());
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		static void TestPublicConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicConstructors ());
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		static void TestConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithNonPublicConstructors ());
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (GetType), new Type[] { typeof (string) })]
		static void TestUnknownType ()
		{
			Type type = Type.GetType (GetStringUnkownType ());
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		static void TestTypeNameFromParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			string typeName)
		{
			RequirePublicConstructors (Type.GetType (typeName));
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static string _typeNameWithPublicParameterlessConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		static void TestTypeNameFromField ()
		{
			RequirePublicConstructors (Type.GetType (_typeNameWithPublicParameterlessConstructor));
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

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) },
			"The method return value with dynamically accessed member kinds 'PublicParameterlessConstructor' is passed into " +
			"the parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow.RequireNonPublicConstructors(Type)' " +
			"which requires dynamically accessed member kinds 'NonPublicConstructors'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'NonPublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetType), new Type[] { typeof (string) },
			"Reflection call 'System.Type.GetType(String)' inside 'Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow.TestMultipleMixedValues()' " +
			"was detected with unknown value for the type name.")]
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

			RequireNonPublicConstructors (Type.GetType (typeName));
		}

		private static void RequirePublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
		}

		private static void RequireNonPublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
		}

		private static void RequireNothing (Type type)
		{
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
