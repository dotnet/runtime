// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
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

			// Validation that value comming from return value of a method is correctly propagated
			instance.PropagateReturnPublicParameterlessConstructor ();
			instance.PropagateReturnPublicParameterlessConstructorFromConstant ();
		}

		private static Type NoRequirements ()
		{
			return typeof (TestType);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnPublicParameterlessConstructor),
			new Type[] { typeof (Type), typeof (Type), typeof (Type) }, returnType: typeof (Type))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private Type ReturnPublicParameterlessConstructor (
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
			new Type[] { typeof (Type) }, returnType: typeof (Type))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private Type ReturnPublicParameterlessConstructorFromUnknownType (Type unknownType)
		{
			return unknownType;
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private Type ReturnPublicParameterlessConstructorFromConstant ()
		{
			return typeof (TestType);
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private Type ReturnPublicParameterlessConstructorFromNull ()
		{
			return null;
		}

		// Validate error message when insufficiently annotated value is returned from a method
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnPublicConstructorsFailure),
			new Type[] { typeof (Type) },
			"The parameter 'publicParameterlessConstructorType' of method 'Mono.Linker.Tests.Cases.DataFlow.MethodReturnParameterDataFlow.ReturnPublicConstructorsFailure(Type)' " +
			"with dynamically accessed member kinds 'PublicParameterlessConstructor' is " +
			"passed into the return value of method 'Mono.Linker.Tests.Cases.DataFlow.MethodReturnParameterDataFlow.ReturnPublicConstructorsFailure(Type)' " +
			"which requires dynamically accessed member kinds 'PublicConstructors'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.", typeof (Type))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private Type ReturnPublicConstructorsFailure (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type publicParameterlessConstructorType)
		{
			return publicParameterlessConstructorType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnNonPublicConstructorsFailure),
			new Type[] { typeof (Type) }, returnType: typeof (Type))]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		private Type ReturnNonPublicConstructorsFailure (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type publicConstructorsType)
		{
			return publicConstructorsType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void PropagateReturnPublicParameterlessConstructor ()
		{
			Type t = ReturnPublicParameterlessConstructor (typeof (TestType), typeof (TestType), typeof (TestType));
			PublicParameterlessConstructorConstructor (t);
			RequirePublicConstructors (t);
			RequireNonPublicConstructors (t);
			RequireNothing (t);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private void PropagateReturnPublicParameterlessConstructorFromConstant ()
		{
			Type t = ReturnPublicParameterlessConstructorFromConstant ();
			PublicParameterlessConstructorConstructor (t);
			RequirePublicConstructors (t);
			RequireNonPublicConstructors (t);
			RequireNothing (t);
		}

		private static void PublicParameterlessConstructorConstructor (
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

		class TestType
		{
			public TestType () { }
		}
	}
}
