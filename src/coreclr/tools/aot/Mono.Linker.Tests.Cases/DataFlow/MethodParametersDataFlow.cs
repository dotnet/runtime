// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
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

			ParametersPassedToInstanceCtor (typeof (TestType), typeof (TestType));

			TestParameterOverwrite (typeof (TestType));

			WriteCapturedParameter.Test ();
		}

		// Validate the error message when annotated parameter is passed to another annotated parameter
		[ExpectedWarning ("IL2067", "'sourceType'", "PublicParameterlessConstructorParameter(Type)", "'type'", "RequiresPublicConstructors(Type)")]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private static void PublicParameterlessConstructorParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type sourceType)
		{
			sourceType.RequiresPublicParameterlessConstructor ();
			sourceType.RequiresPublicConstructors ();
			sourceType.RequiresNonPublicConstructors ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		private static void PublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor))]
		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		private static void NonPublicConstructorsParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type.RequiresNonPublicConstructors ();
		}

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		private void InstanceMethod (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
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

		[ExpectedWarning ("IL2072", "'type'", "argument", nameof (LongWriteToParameterOnInstanceMethod) + "(Int32, Int32, Int32, Int32, Type)", nameof (ReturnThingsWithPublicParameterlessConstructor))]
		private void LongWriteToParameterOnInstanceMethod (
			int a, int b, int c, int d,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
			Type type)
		{
			type = ReturnThingsWithPublicParameterlessConstructor ();
		}

		[ExpectedWarning ("IL2072", "'type'", "argument", nameof (LongWriteToParameterOnStaticMethod) + "(Int32, Int32, Int32, Int32, Type)", nameof (ReturnThingsWithPublicParameterlessConstructor))]
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

		[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		private void TwoAnnotatedParameters (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type2)
		{
			type.RequiresPublicParameterlessConstructor ();
			type2.RequiresPublicParameterlessConstructor ();
			type.RequiresPublicConstructors ();
			type2.RequiresPublicConstructors ();
		}

		// TODO: https://github.com/dotnet/linker/issues/2273
		// (Dataflow analysis is not supported by the analyzer yet)
		[ExpectedWarning ("IL2067",
			nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors) + "(Type)",
			"'type'")]
		private void TwoAnnotatedParametersIntoOneValue (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type,
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
			Type type2)
		{
			Type t = type == null ? type : type2;
			t.RequiresPublicParameterlessConstructor ();
			t.RequiresPublicConstructors ();
		}

		// Validate the error message for the case of unannotated method return value passed to an annotated parameter.
		[ExpectedWarning ("IL2067", "'type'", "NoAnnotation(Type)", "'type'", "RequiresPublicParameterlessConstructor(Type)")]
		private void NoAnnotation (Type type)
		{
			type.RequiresPublicParameterlessConstructor ();
		}

		// Validate error message when untracable value is passed to an annotated parameter.
		[ExpectedWarning ("IL2062",
			nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor) + "(Type)",
			"'type'")]
		private void UnknownValue ()
		{
			var array = new object[1];
			array[0] = this.GetType ();
			MakeArrayValuesUnknown (array);
			((Type) array[0]).RequiresPublicParameterlessConstructor ();

			static void MakeArrayValuesUnknown (object[] array)
			{
			}
		}

		private void AnnotatedValueToUnAnnotatedParameter (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			Type type)
		{
			type.RequiresNone ();
		}

		private void UnknownValueToUnAnnotatedParameter ()
		{
			this.GetType ().RequiresNone ();
		}

		private void UnknownValueToUnAnnotatedParameterOnInterestingMethod ()
		{
			RequirePublicParameterlessConstructorAndNothing (typeof (TestType), this.GetType ());
		}

		[ExpectedWarning ("IL2098", "p1", nameof (UnsupportedParameterType))]
		private void UnsupportedParameterType ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] object p1)
		{
		}

		private class InstanceCtor
		{
			[ExpectedWarning ("IL2067", nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
			public InstanceCtor ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type)
			{
				type.RequiresNonPublicConstructors ();
				type.RequiresPublicConstructors ();
			}
		}

		[ExpectedWarning ("IL2067", "'type'")]
		static void ParametersPassedToInstanceCtor ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type typeWithCtors, Type typeWithNothing)
		{
			var a1 = new InstanceCtor (typeWithCtors); // no warn
			var a2 = new InstanceCtor (typeof (TestType)); // no warn
			var a3 = new InstanceCtor (typeWithNothing); // warn
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

		class WriteCapturedParameter
		{
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), "parameter")]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), "parameter")]
			static void TestNullCoalesce ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parameter = null)
			{
				parameter = GetUnknownType () ?? GetTypeWithPublicConstructors ();
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknownType), "parameter")]
			static void TestNullCoalescingAssignment ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parameter = null)
			{
				parameter ??= GetUnknownType ();
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknownType), "parameter")]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), "parameter")]
			static void TestNullCoalescingAssignmentComplex ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type parameter = null)
			{
				parameter ??= (GetUnknownType () ?? GetTypeWithPublicConstructors ());
			}

			public static void Test ()
			{
				TestNullCoalesce ();
				TestNullCoalescingAssignment ();
				TestNullCoalescingAssignmentComplex ();
			}
		}

		class TestType
		{
			public TestType () { }
			public TestType (int arg) { }
			private TestType (int arg1, int arg2) { }
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		private static Type GetUnknownType ()
		{
			return null;
		}
	}
}
