// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

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

			TestStringEmpty ();

			// TODO:
			// Test multi-value returns
			//    Type.GetType over a constant and a param
			//    Type.GetType over two params
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestPublicParameterlessConstructor ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicParameterlessConstructor ());
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestPublicConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicConstructors ());
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithNonPublicConstructors ());
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (GetType), new Type[] { typeof (string) }, messageCode: "IL2057")]
		static void TestUnknownType ()
		{
			Type type = Type.GetType (GetStringUnkownType ());
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		static void TestTypeNameFromParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			string typeName)
		{
			RequirePublicConstructors (Type.GetType (typeName));
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static string _typeNameWithPublicParameterlessConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
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

		[RecognizedReflectionAccessPattern]
		static void TestStringEmpty ()
		{
			Type.GetType (string.Empty);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) },
			messageCode: "IL2072", message: new string[] { "GetType" })]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetType), new Type[] { typeof (string) },
			messageCode: "IL2057", message: new string[] { "System.Type.GetType(String)" })]
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
