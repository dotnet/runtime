// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
			TestDefaultConstructor ();
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

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		static void TestDefaultConstructor ()
		{
			Type type = Type.GetType (GetStringTypeWithDefaultConstructor ());
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		static void TestPublicConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithPublicConstructors ());
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
			RequireNothing (type);
		}

		[RecognizedReflectionAccessPattern]
		static void TestConstructors ()
		{
			Type type = Type.GetType (GetStringTypeWithConstructors ());
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
			RequireNothing (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (GetType), new Type [] { typeof (string) })]
		static void TestUnknownType ()
		{
			Type type = Type.GetType (GetStringUnkownType ());
		}

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		static void TestTypeNameFromParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			string typeName)
		{
			RequirePublicConstructors (Type.GetType (typeName));
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
		static string _typeNameWithDefaultConstructor;

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		static void TestTypeNameFromField ()
		{
			RequirePublicConstructors (Type.GetType (_typeNameWithDefaultConstructor));
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

		[UnrecognizedReflectionAccessPattern (typeof (GetTypeDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) },
			"The method return value with dynamically accessed member kinds 'DefaultConstructor' is passed into " +
			"the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow::RequireConstructors(System.Type)' " +
			"which requires dynamically accessed member kinds `Constructors`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'Constructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.GetType), new Type [] { typeof (string) },
			"Reflection call 'System.Type System.Type::GetType(System.String)' inside 'System.Void Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow::TestMultipleMixedValues()' " +
			"was detected with unknown value for the type name.")]
		static void TestMultipleMixedValues ()
		{
			string typeName = null;
			switch (_switchOnField) {
				case 0:
					typeName = GetStringTypeWithDefaultConstructor ();
					break;
				case 1:
					typeName = GetStringTypeWithConstructors ();
					break;
				case 2:
					typeName = "Mono.Linker.Tests.Cases.DataFlow.GetTypeDataFlow";
					break;
				case 3:
					typeName = GetStringUnkownType ();
					break;
			}

			RequireConstructors (Type.GetType (typeName));
		}

		private static void RequireDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type)
		{
		}

		private static void RequirePublicConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type type)
		{
		}

		private static void RequireConstructors (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
		}

		private static void RequireNothing (Type type)
		{
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		private static string GetStringTypeWithDefaultConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		private static string GetStringTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
		private static string GetStringTypeWithConstructors ()
		{
			return null;
		}

		private static string GetStringUnkownType ()
		{
			return null;
		}
	}
}
