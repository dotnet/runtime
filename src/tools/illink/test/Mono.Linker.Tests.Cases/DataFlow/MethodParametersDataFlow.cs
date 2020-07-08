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
	public class MethodParametersDataFlow
	{
		public static void Main ()
		{
			var instance = new MethodParametersDataFlow ();

			PublicParameterlessConstructorParameter (typeof (TestType));
			PublicConstructorsParameter (typeof (TestType));
			NonPublicConstructorsParameter (typeof (TestType));
			WriteToParameterOnStaticMethod (null);
			LongWriteToParameterOnStaticMethod (0, 0, 0, 0, null);
			instance.InstanceMethod (typeof (TestType));
			instance.TwoAnnotatedParameters (typeof (TestType), typeof (TestType));
			instance.TwoAnnotatedParametersIntoOneValue (typeof (TestType), typeof (TestType));
			instance.NoAnnotation (typeof (TestType));
			instance.UnknownValue ();
			instance.AnnotatedValueToUnAnnotatedParameter (typeof (TestType));
			instance.UnknownValueToUnAnnotatedParameter ();
			instance.UnknownValueToUnAnnotatedParameterOnInterestingMethod ();
			instance.WriteToParameterOnInstanceMethod (null);
			instance.LongWriteToParameterOnInstanceMethod (0, 0, 0, 0, null);
		}

		// Validate the error message when annotated parameter is passed to another annotated parameter
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) },
			"The parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow.PublicParameterlessConstructorParameter(Type)' " +
			"with dynamically accessed member kinds 'PublicParameterlessConstructor' is passed into the parameter 'type' " +
			"of method 'Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow.RequirePublicConstructors(Type)' " +
			"which requires dynamically accessed member kinds 'PublicConstructors'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private static void PublicParameterlessConstructorParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) })]
		private static void PublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		private static void NonPublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		private void InstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), "type")]
		private void WriteToParameterOnInstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), "type")]
		private static void WriteToParameterOnStaticMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), "type")]
		private void LongWriteToParameterOnInstanceMethod (
			int a, int b, int c, int d,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (Type), "type")]
		private static void LongWriteToParameterOnStaticMethod (
			int a, int b, int c, int d,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static private Type ReturnThingsWithPublicParameterlessConstructor ()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		private void TwoAnnotatedParameters (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type2)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicParameterlessConstructor (type2);
			RequirePublicConstructors (type);
			RequirePublicConstructors (type2);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) })]
		private void TwoAnnotatedParametersIntoOneValue (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type2)
		{
			Type t = type == null ? type : type2;
			RequirePublicParameterlessConstructor (t);
			RequirePublicConstructors (t);
		}

		// Validate the error message for the case of unannotated method return value passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) },
			"The parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow.NoAnnotation(Type)' " +
			"with dynamically accessed member kinds 'None' is passed into " +
			"the parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow.RequirePublicParameterlessConstructor(Type)' " +
			"which requires dynamically accessed member kinds 'PublicParameterlessConstructor'. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicParameterlessConstructor'.")]
		private void NoAnnotation (Type type)
		{
			RequirePublicParameterlessConstructor (type);
		}

		// Validate error message when untracable value is passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) },
			"A value from unknown source is passed " +
			"into the parameter 'type' of method 'Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow.RequirePublicParameterlessConstructor(Type)' " +
			"which requires dynamically accessed member kinds 'PublicParameterlessConstructor'. " +
			"It's not possible to guarantee that these requirements are met by the application.")]
		private void UnknownValue ()
		{
			var array = new object[1];
			array[0] = this.GetType ();
			RequirePublicParameterlessConstructor ((Type) array[0]);
		}

		[RecognizedReflectionAccessPattern]
		private void AnnotatedValueToUnAnnotatedParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
			RequireNothing (type);
		}

		[RecognizedReflectionAccessPattern]
		private void UnknownValueToUnAnnotatedParameter ()
		{
			RequireNothing (this.GetType ());
		}

		[RecognizedReflectionAccessPattern]
		private void UnknownValueToUnAnnotatedParameterOnInterestingMethod ()
		{
			RequirePublicParameterlessConstructorAndNothing (typeof (TestType), this.GetType ());
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

		private static void RequirePublicParameterlessConstructorAndNothing (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type,
			Type type2)
		{
		}

		class TestType
		{
			public TestType () { }
			public TestType (int arg) { }
			private TestType (int arg1, int arg2) { }
		}
	}
}
