// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class MethodReturnParameterDataFlow
	{
		public static void Main ()
		{
			var instance = new MethodReturnParameterDataFlow ();

			// Validation that assigning value to the return value is verified
			NoRequirements ();
			instance.ReturnPublicParameterlessConstructor (typeof (TestType), typeof (TestType), typeof (TestType));
			instance.ReturnPublicParameterlessConstructorFromUnknownType (null);
			instance.ReturnPublicParameterlessConstructorFromConstant ();
			instance.ReturnPublicParameterlessConstructorFromNull ();
			instance.ReturnPublicConstructorsFailure (null);
			instance.ReturnNonPublicConstructorsFailure (null);
			instance.ReturnUnknownValue ();

			// Validation that value comming from return value of a method is correctly propagated
			instance.PropagateReturnPublicParameterlessConstructor ();
			instance.PropagateReturnPublicParameterlessConstructorFromConstant ();
			instance.PropagateReturnToReturn (0);

			instance.ReturnWithRequirementsAlwaysThrows ();

			UnsupportedReturnType ();
		}

		static Type NoRequirements ()
		{
			return typeof (TestType);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnPublicParameterlessConstructor),
			new Type[] { typeof (Type), typeof (Type), typeof (Type) }, returnType: typeof (Type), messageCode: "IL2068")]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type ReturnPublicParameterlessConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type publicParameterlessConstructorType,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type publicConstructorsType,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type nonPublicConstructorsType)
		{
			switch (GetHashCode ()) {
			case 1:
				return publicParameterlessConstructorType;
			case 2:
				return publicConstructorsType;
			case 3:
				return nonPublicConstructorsType;
			case 4:
				return typeof (TestType);
			default:
				return null;
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnPublicParameterlessConstructorFromUnknownType),
			new Type[] { typeof (Type) }, returnType: typeof (Type), messageCode: "IL2068")]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type ReturnPublicParameterlessConstructorFromUnknownType (Type unknownType)
		{
			return unknownType;
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type ReturnPublicParameterlessConstructorFromConstant ()
		{
			return typeof (TestType);
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type ReturnPublicParameterlessConstructorFromNull ()
		{
			return null;
		}

		[RecognizedReflectionAccessPattern]
		Type ReturnTypeWithNoRequirements ()
		{
			return null;
		}

		// Validate error message when insufficiently annotated value is returned from a method
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnPublicConstructorsFailure),
			new Type[] { typeof (Type) }, returnType: typeof (Type),
			messageCode: "IL2068", message: new string[] {
				"publicParameterlessConstructorType",
				"MethodReturnParameterDataFlow.ReturnPublicConstructorsFailure",
				"MethodReturnParameterDataFlow.ReturnPublicConstructorsFailure" })]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type ReturnPublicConstructorsFailure (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type publicParameterlessConstructorType)
		{
			return publicParameterlessConstructorType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnNonPublicConstructorsFailure),
			new Type[] { typeof (Type) }, returnType: typeof (Type), messageCode: "IL2068")]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		Type ReturnNonPublicConstructorsFailure (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type publicConstructorsType)
		{
			return publicConstructorsType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnUnknownValue),
			new Type[] { }, returnType: typeof (Type),
			messageCode: "IL2063", message: nameof (ReturnUnknownValue))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type ReturnUnknownValue ()
		{
			var array = new object[1];
			array[0] = this.GetType ();
			MakeArrayValuesUnknown (array);
			return (Type) array[0];

			static void MakeArrayValuesUnknown (object[] array)
			{
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		void PropagateReturnPublicParameterlessConstructor ()
		{
			Type t = ReturnPublicParameterlessConstructor (typeof (TestType), typeof (TestType), typeof (TestType));
			t.RequiresPublicParameterlessConstructor ();
			t.RequiresPublicConstructors ();
			t.RequiresNonPublicConstructors ();
			t.RequiresNone ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2072")]
		void PropagateReturnPublicParameterlessConstructorFromConstant ()
		{
			Type t = ReturnPublicParameterlessConstructorFromConstant ();
			t.RequiresPublicParameterlessConstructor ();
			t.RequiresPublicConstructors ();
			t.RequiresNonPublicConstructors ();
			t.RequiresNone ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (PropagateReturnToReturn), new Type[] { typeof (int) }, returnType: typeof (Type),
			messageCode: "IL2073", message: new string[] {
				nameof (ReturnTypeWithNoRequirements),
				nameof (PropagateReturnToReturn)
			})]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type PropagateReturnToReturn (int n)
		{
			switch (n) {
			case 0:
				return ReturnPublicParameterlessConstructorFromConstant ();
			case 1:
				return ReturnTypeWithNoRequirements ();
			}

			return null;
		}

		// https://github.com/mono/linker/issues/2025
		// Ideally this should not warn
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnWithRequirementsAlwaysThrows), new Type[] { }, returnType: typeof (Type),
			messageCode: "IL2063")]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		Type ReturnWithRequirementsAlwaysThrows ()
		{
			throw new NotImplementedException ();
		}

		[ExpectedWarning ("IL2106", nameof (UnsupportedReturnType))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static object UnsupportedReturnType () => null;

		class TestType
		{
			public TestType () { }
		}
	}
}
