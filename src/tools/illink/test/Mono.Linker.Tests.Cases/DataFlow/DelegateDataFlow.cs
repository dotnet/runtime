// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class DelegateDataFlow
	{
		public static void Main ()
		{
			AnnotatedDelegateParameter.Test ();
			AnnotatedDelegateFuncLikeParameter.Test ();
			AnnotatedDelegateMultipleParameters.Test ();
			AnnotatedDelegateWithAnnotatedParamAndUnannotatedReturn.Test ();
			GenericDelegateWithAnnotatedTypeParameter.Test ();
			ActionAndFuncWithAnnotatedLambda.Test ();
		}

		// ===================================================
		// Tests for custom delegate type with DAM-annotated parameter
		// as field, property, and local variable — then invoked
		// ===================================================
		class AnnotatedDelegateParameter
		{
			delegate void DelegateWithAnnotatedParam ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type);

			// --- Field scenarios ---

			static DelegateWithAnnotatedParam _fieldDelegate;

			static void TestFieldInvokeWithMatchingAnnotation (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				_fieldDelegate (type);
			}

			[ExpectedWarning ("IL2067", nameof (type), nameof (_fieldDelegate))]
			static void TestFieldInvokeWithoutAnnotation (Type type)
			{
				_fieldDelegate (type);
			}

			[ExpectedWarning ("IL2067", nameof (type), nameof (_fieldDelegate))]
			static void TestFieldInvokeWithMismatchedAnnotation (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				_fieldDelegate (type);
			}

			static void TestFieldInvokeWithTypeOf ()
			{
				_fieldDelegate (typeof (TestType));
			}

			// --- Property scenarios ---

			static DelegateWithAnnotatedParam DelegateProperty { get; set; }

			static void TestPropertyInvokeWithMatchingAnnotation (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				DelegateProperty (type);
			}

			[ExpectedWarning ("IL2067", nameof (type), nameof (DelegateProperty))]
			static void TestPropertyInvokeWithoutAnnotation (Type type)
			{
				DelegateProperty (type);
			}

			// --- Local variable scenarios ---

			[ExpectedWarning ("IL2067", nameof (type), "Invoke")]
			static void TestLocalInvokeWithoutAnnotation (Type type)
			{
				DelegateWithAnnotatedParam local = _fieldDelegate;
				local (type);
			}

			public static void Test ()
			{
				TestFieldInvokeWithMatchingAnnotation (typeof (TestType));
				TestFieldInvokeWithoutAnnotation (typeof (TestType));
				TestFieldInvokeWithMismatchedAnnotation (typeof (TestType));
				TestFieldInvokeWithTypeOf ();
				TestPropertyInvokeWithMatchingAnnotation (typeof (TestType));
				TestPropertyInvokeWithoutAnnotation (typeof (TestType));
				TestLocalInvokeWithoutAnnotation (typeof (TestType));
			}
		}

		// ===================================================
		// Tests for Func-like delegate (parameter + return) with DAM on parameter
		// ===================================================
		class AnnotatedDelegateFuncLikeParameter
		{
			delegate string DelegateWithAnnotatedTypeParam ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type);

			static DelegateWithAnnotatedTypeParam _field;

			[ExpectedWarning ("IL2067", nameof (type), nameof (_field))]
			static void TestFieldInvokeWithoutAnnotation (Type type)
			{
				_field (type);
			}

			static void TestFieldInvokeWithMatchingAnnotation (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				_field (type);
			}

			public static void Test ()
			{
				TestFieldInvokeWithoutAnnotation (typeof (TestType));
				TestFieldInvokeWithMatchingAnnotation (typeof (TestType));
			}
		}

		// ===================================================
		// Tests for custom delegate types with multiple annotated parameters
		// ===================================================
		class AnnotatedDelegateMultipleParameters
		{
			delegate void DelegateWithTwoAnnotatedParams (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type typeMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type typeFields);

			static DelegateWithTwoAnnotatedParams _field;

			static void TestMatchingAnnotations (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type typeMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type typeFields)
			{
				_field (typeMethods, typeFields);
			}

			[ExpectedWarning ("IL2067", nameof (type), nameof (_field))]
			static void TestFirstParameterMismatched (
				Type type,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type typeFields)
			{
				_field (type, typeFields);
			}

			[ExpectedWarning ("IL2067", nameof (type), nameof (_field))]
			static void TestSecondParameterMismatched (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type typeMethods,
				Type type)
			{
				_field (typeMethods, type);
			}

			[ExpectedWarning ("IL2067", nameof (type1), nameof (_field))]
			[ExpectedWarning ("IL2067", nameof (type2), nameof (_field))]
			static void TestBothParametersMismatched (Type type1, Type type2)
			{
				_field (type1, type2);
			}

			public static void Test ()
			{
				TestMatchingAnnotations (typeof (TestType), typeof (TestType));
				TestFirstParameterMismatched (typeof (TestType), typeof (TestType));
				TestSecondParameterMismatched (typeof (TestType), typeof (TestType));
				TestBothParametersMismatched (typeof (TestType), typeof (TestType));
			}
		}

		// ===================================================
		// Tests for custom delegate with annotated parameter and unannotated return
		// verifying both parameter and return value flow
		// ===================================================
		class AnnotatedDelegateWithAnnotatedParamAndUnannotatedReturn
		{
			delegate Type DelegateWithAnnotatedParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type);

			static DelegateWithAnnotatedParam _field;

			// Return is unannotated, so using it for PublicMethods should warn
			[ExpectedWarning ("IL2072", nameof (_field), nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			static void TestReturnValueUsedWithRequirement (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				_field (type).RequiresPublicMethods ();
			}

			// Parameter is unannotated - should warn for parameter; return used without requirement - no extra warning
			[ExpectedWarning ("IL2067", nameof (type), nameof (_field))]
			static void TestParameterMismatch (Type type)
			{
				_ = _field (type);
			}

			// Both: parameter mismatch + return value used with requirement
			[ExpectedWarning ("IL2067", nameof (type), nameof (_field))]
			[ExpectedWarning ("IL2072", nameof (_field), nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			static void TestBothMismatched (Type type)
			{
				_field (type).RequiresPublicMethods ();
			}

			public static void Test ()
			{
				TestReturnValueUsedWithRequirement (typeof (TestType));
				TestParameterMismatch (typeof (TestType));
				TestBothMismatched (typeof (TestType));
			}
		}

		// ===================================================
		// Tests for generic delegate with DAM on the type parameter itself
		// ===================================================
		class GenericDelegateWithAnnotatedTypeParameter
		{
			delegate void DelegateWithGenericParam<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> (T value);

			static DelegateWithGenericParam<Type> _fieldInstantiatedWithType;

			[ExpectedWarning ("IL2067", nameof (type), nameof (_fieldInstantiatedWithType))]
			static void TestFieldInvokeWithoutAnnotation (Type type)
			{
				_fieldInstantiatedWithType (type);
			}

			static void TestFieldInvokeWithMatchingAnnotation (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				_fieldInstantiatedWithType (type);
			}

			public static void Test ()
			{
				TestFieldInvokeWithoutAnnotation (typeof (TestType));
				TestFieldInvokeWithMatchingAnnotation (typeof (TestType));
			}
		}

		// ===================================================
		// Tests for Action<> and Func<> with annotated lambda parameters
		// This tests that the annotations on lambda parameters are tracked
		// when creating delegates from lambdas with DAM annotations.
		// ===================================================
		class ActionAndFuncWithAnnotatedLambda
		{
			// Assigning a lambda with DAM-annotated parameter to Action<Type>
			// should warn because Action<Type>'s Invoke parameter has no DAM.
			[ExpectedWarning ("IL2111")]
			static void TestActionWithAnnotatedLambda ()
			{
				Action<Type> a = ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) => { };
				a (typeof (TestType));
			}

			// Same for Func<Type, string>
			[ExpectedWarning ("IL2111")]
			static void TestFuncWithAnnotatedLambda ()
			{
				Func<Type, string> f = ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type t) => t.ToString ();
				f (typeof (TestType));
			}

			// Annotated local function assigned to Action<Type>
			[ExpectedWarning ("IL2111")]
			static void TestActionWithAnnotatedLocalFunction ()
			{
				Action<Type> a = LocalFunction;
				a (typeof (TestType));

				void LocalFunction (
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
				{
				}
			}

			public static void Test ()
			{
				TestActionWithAnnotatedLambda ();
				TestFuncWithAnnotatedLambda ();
				TestActionWithAnnotatedLocalFunction ();
			}
		}

		class TestType
		{
		}
	}
}
