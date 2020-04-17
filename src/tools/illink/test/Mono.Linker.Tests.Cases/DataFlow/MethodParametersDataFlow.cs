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
	public class MethodParametersDataFlow
	{
		public static void Main ()
		{
			var instance = new MethodParametersDataFlow ();

			DefaultConstructorParameter (typeof (TestType));
			PublicConstructorsParameter (typeof (TestType));
			ConstructorsParameter (typeof (TestType));
			WriteToParameterOnStaticMethod (null);
			LongWriteToParameterOnStaticMethod (0, 0, 0, 0, null);
			instance.InstanceMethod (typeof (TestType));
			instance.TwoAnnotatedParameters (typeof (TestType), typeof (TestType));
			instance.TwoAnnotatedParametersIntoOneValue(typeof (TestType), typeof (TestType));
			instance.NoAnnotation (typeof (TestType));
			instance.UnknownValue ();
			instance.AnnotatedValueToUnAnnotatedParameter (typeof (TestType));
			instance.UnknownValueToUnAnnotatedParameter ();
			instance.UnknownValueToUnAnnotatedParameterOnInterestingMethod ();
			instance.WriteToParameterOnInstanceMethod (null);
			instance.LongWriteToParameterOnInstanceMethod (0, 0, 0, 0, null);
		}

		// Validate the error message when annotated parameter is passed to another annotated parameter
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) },
			"The parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::DefaultConstructorParameter(System.Type)' " +
			"with dynamically accessed member kinds 'DefaultConstructor' is passed into the parameter 'type' " +
			"of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::RequirePublicConstructors(System.Type)' " +
			"which requires dynamically accessed member kinds `PublicConstructors`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'PublicConstructors'.")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private static void DefaultConstructorParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireConstructors), new Type [] { typeof (Type) })]
		private static void PublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
		}

		[RecognizedReflectionAccessPattern]
		private static void ConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
			RequireConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		private void InstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type)
		{
			RequireDefaultConstructor (type);
			RequirePublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (WriteToParameterOnInstanceMethod), new Type [] { typeof (Type) })]
		private void WriteToParameterOnInstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
			type = ReturnThingsWithDefaultConstructor ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (WriteToParameterOnStaticMethod), new Type [] { typeof (Type) })]
		private static void WriteToParameterOnStaticMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
			type = ReturnThingsWithDefaultConstructor ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (LongWriteToParameterOnInstanceMethod), new Type [] { typeof(int), typeof (int), typeof (int), typeof (int), typeof (Type) })]
		private void LongWriteToParameterOnInstanceMethod (
			int a, int b, int c, int d,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
			type = ReturnThingsWithDefaultConstructor ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (LongWriteToParameterOnStaticMethod), new Type [] { typeof (int), typeof (int), typeof (int), typeof (int), typeof (Type) })]
		private static void LongWriteToParameterOnStaticMethod (
			int a, int b, int c, int d,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.Constructors)]
			Type type)
		{
			type = ReturnThingsWithDefaultConstructor ();
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberKinds.DefaultConstructor)]
		static private Type ReturnThingsWithDefaultConstructor()
		{
			return null;
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		private void TwoAnnotatedParameters (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type type2)
		{
			RequireDefaultConstructor (type);
			RequireDefaultConstructor (type2);
			RequirePublicConstructors (type);
			RequirePublicConstructors (type2);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type [] { typeof (Type) })]
		private void TwoAnnotatedParametersIntoOneValue (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.PublicConstructors)]
			Type type2)
		{
			Type t = type == null ? type : type2;
			RequireDefaultConstructor (t);
			RequirePublicConstructors (t);
		}

		// Validate the error message for the case of unannotated method return value passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireDefaultConstructor), new Type [] { typeof (Type) },
			"The parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::NoAnnotation(System.Type)' " +
			"with dynamically accessed member kinds 'None' is passed into " +
			"the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::RequireDefaultConstructor(System.Type)' " +
			"which requires dynamically accessed member kinds `DefaultConstructor`. " +
			"To fix this add DynamicallyAccessedMembersAttribute to it and specify at least these member kinds 'DefaultConstructor'.")]
		private void NoAnnotation (Type type)
		{
			RequireDefaultConstructor (type);
		}

		// Validate error message when untracable value is passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireDefaultConstructor), new Type [] { typeof (Type) },
			"A value from unknown source is passed " +
			"into the parameter 'type' of method 'System.Void Mono.Linker.Tests.Cases.DataFlow.MethodParametersDataFlow::RequireDefaultConstructor(System.Type)' " +
			"which requires dynamically accessed member kinds `DefaultConstructor`. " +
			"It's not possible to guarantee that these requirements are met by the application.")]
		private void UnknownValue ()
		{
			var array = new object [1];
			array [0] = this.GetType ();
			RequireDefaultConstructor ((Type)array [0]);
		}

		[RecognizedReflectionAccessPattern]
		private void AnnotatedValueToUnAnnotatedParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type)
		{
			RequireNothing (type);
		}

		[RecognizedReflectionAccessPattern]
		private void UnknownValueToUnAnnotatedParameter ()
		{
			RequireNothing (this.GetType());
		}

		[RecognizedReflectionAccessPattern]
		private void UnknownValueToUnAnnotatedParameterOnInterestingMethod ()
		{
			RequireDefaultConstructorAndNothing (typeof (TestType), this.GetType ());
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

		private static void RequireDefaultConstructorAndNothing (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberKinds.DefaultConstructor)]
			Type type,
			Type type2)
		{
		}

		class TestType
		{
			public TestType() { }
			public TestType (int arg) { }
			private TestType (int arg1, int arg2) { }
		}
	}
}
