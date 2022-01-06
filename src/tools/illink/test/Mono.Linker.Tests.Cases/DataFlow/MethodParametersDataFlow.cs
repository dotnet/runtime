// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the UnrecognizedReflectionAccessPattern attributes.
	[SkipKeptItemsValidation]
	[SetupLinkerArgument ("--keep-metadata", "parametername")]
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
			instance.UnsupportedParameterType (null);

			TestParameterOverwrite (typeof (TestType));
		}

		// Validate the error message when annotated parameter is passed to another annotated parameter
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) },
			messageCode: "IL2067", message: new string[] { "sourceType", "PublicParameterlessConstructorParameter(Type)", "type", "RequirePublicConstructors(Type)" })]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2067")]
		private static void PublicParameterlessConstructorParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type sourceType)
		{
			RequirePublicParameterlessConstructor (sourceType);
			RequirePublicConstructors (sourceType);
			RequireNonPublicConstructors (sourceType);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequireNonPublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2067")]
		private static void PublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) }, messageCode: "IL2067")]
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2067")]
		private static void NonPublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
			RequireNonPublicConstructors (type);
		}

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2067")]
		private void InstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
			RequirePublicParameterlessConstructor (type);
			RequirePublicConstructors (type);
		}

		[ExpectedWarning ("IL2072", "'type'", "argument", nameof (WriteToParameterOnInstanceMethod) + "(Type)", nameof (ReturnThingsWithPublicParameterlessConstructor))]
		private void WriteToParameterOnInstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[ExpectedWarning ("IL2072", "'type'", "argument", nameof (WriteToParameterOnStaticMethod) + "(Type)", nameof (ReturnThingsWithPublicParameterlessConstructor))]
		private static void WriteToParameterOnStaticMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[ExpectedWarning ("IL2072", "'type'", "argument", nameof (LongWriteToParameterOnInstanceMethod) + "(Int32,Int32,Int32,Int32,Type)", nameof (ReturnThingsWithPublicParameterlessConstructor))]
		private void LongWriteToParameterOnInstanceMethod (
			int a, int b, int c, int d,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[ExpectedWarning ("IL2072", "'type'", "argument", nameof (LongWriteToParameterOnStaticMethod) + "(Int32,Int32,Int32,Int32,Type)", nameof (ReturnThingsWithPublicParameterlessConstructor))]
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

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2067")]
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

		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicConstructors), new Type[] { typeof (Type) }, messageCode: "IL2067")]
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
			messageCode: "IL2067", message: new string[] { "type", "NoAnnotation(Type)", "type", "RequirePublicParameterlessConstructor(Type)" })]
		private void NoAnnotation (Type type)
		{
			RequirePublicParameterlessConstructor (type);
		}

		// Validate error message when untracable value is passed to an annotated parameter.
		[UnrecognizedReflectionAccessPattern (typeof (MethodParametersDataFlow), nameof (RequirePublicParameterlessConstructor), new Type[] { typeof (Type) },
			messageCode: "IL2062", message: new string[] { "type", "RequirePublicParameterlessConstructor" })]
		private void UnknownValue ()
		{
			var array = new object[1];
			array[0] = this.GetType ();
			MakeArrayValuesUnknown (array);
			RequirePublicParameterlessConstructor ((Type) array[0]);

			static void MakeArrayValuesUnknown (object[] array)
			{
			}
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

		[ExpectedWarning ("IL2098", "p1", nameof (UnsupportedParameterType))]
		private void UnsupportedParameterType ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] object p1)
		{
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

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type _fieldWithMethods;

		[ExpectedWarning ("IL2077", nameof (_fieldWithMethods))]
		static void TestParameterOverwrite ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
		{
			type = _fieldWithMethods;
			type.GetFields ();
		}

		class TestType
		{
			public TestType () { }
			public TestType (int arg) { }
			private TestType (int arg1, int arg2) { }
		}
	}
}
