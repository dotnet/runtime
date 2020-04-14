// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	public class MethodReturnParameterDataFlow
	{
		public static void Main()
		{
			var instance = new MethodReturnParameterDataFlow ();

			// Validation that assigning value to the return value is verified
			NoRequirements ();
			instance.ReturnDefaultConstructor (typeof (TestType), typeof (TestType), typeof (TestType));
			instance.ReturnDefaultConstructorFromUnknownType (null);
			instance.ReturnDefaultConstructorFromConstant ();
			instance.ReturnDefaultConstructorFromNull ();
			instance.ReturnPublicConstructorsFailure (null);
			instance.ReturnConstructorsFailure (null);

			// Validation that value comming from return value of a method is correctly propagated
			instance.PropagateReturnDefaultConstructor ();
			instance.PropagateReturnDefaultConstructorFromConstant ();
		}

		private static Type NoRequirements ()
		{
			return typeof (TestType);
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		private Type ReturnDefaultConstructor (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type defaultConstructorType,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type publicConstructorsType,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
			Type constructorsType)
		{
			switch (GetHashCode ()) {
				case 1:
					return defaultConstructorType;
				case 2:
					return publicConstructorsType;
				case 3:
					return constructorsType;
				case 4:
					return typeof (TestType);
				default:
					return null;
			}
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnDefaultConstructorFromUnknownType), new Type [] { typeof (Type) })]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		private Type ReturnDefaultConstructorFromUnknownType (Type unknownType)
		{
			return unknownType;
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		private Type ReturnDefaultConstructorFromConstant ()
		{
			return typeof (TestType);
		}

		[RecognizedReflectionAccessPattern]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		private Type ReturnDefaultConstructorFromNull ()
		{
			return null;
		}

		// Validate error message when insufficiently annotated value is returned from a method
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnPublicConstructorsFailure), new Type [] { typeof (Type) },
			"The parameter 'defaultConstructorType' of method 'System.Type Mono.Linker.Tests.Cases.DataFlow.MethodReturnParameterDataFlow::ReturnPublicConstructorsFailure(System.Type)' " +
			"with dynamically accessed member kinds 'DefaultConstructor' is " +
			"passed into the return value of method 'System.Type Mono.Linker.Tests.Cases.DataFlow.MethodReturnParameterDataFlow::ReturnPublicConstructorsFailure(System.Type)' " +
			"which requires dynamically accessed member kinds `PublicConstructors`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.PublicConstructors)]
		private Type ReturnPublicConstructorsFailure (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type defaultConstructorType)
		{
			return defaultConstructorType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (ReturnConstructorsFailure), new Type [] { typeof (Type) })]
		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.Constructors)]
		private Type ReturnConstructorsFailure (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type publicConstructorsType)
		{
			return publicConstructorsType;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void PropagateReturnDefaultConstructor()
		{
			Type t = ReturnDefaultConstructor (typeof (TestType), typeof (TestType), typeof (TestType));
			RequireDefaultConstructor (t);
			RequirePublicConstructors (t);
			RequireConstructors (t);
			RequireNothing (t);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodReturnParameterDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private void PropagateReturnDefaultConstructorFromConstant ()
		{
			Type t = ReturnDefaultConstructorFromConstant ();
			RequireDefaultConstructor (t);
			RequirePublicConstructors (t);
			RequireConstructors (t);
			RequireNothing (t);
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

		class TestType
		{
			public TestType () { }
		}
	}
}
