using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using BindingFlags = System.Reflection.BindingFlags;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class MakeGenericDataFlow
	{
		public static void Main ()
		{
			MakeGenericType.Test ();
			MakeGenericMethod.Test ();
		}

		class MakeGenericType
		{
			public static void Test ()
			{
				TestNullType ();
				TestUnknownInput (null);
				TestWithUnknownTypeArray (null);
				TestWithArrayUnknownIndexSet (0);
				TestWithArrayUnknownLengthSet (1);
				TestNoArguments ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromParamWithMismatch (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsFromGenericParamWithMismatch<TestType> ();

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);

				TestWithMultipleArgumentsWithRequirements ();

				TestWithNewConstraint ();
				TestWithStructConstraint ();
				TestWithUnmanagedConstraint ();
				TestWithNullable ();
			}

			// This is OK since we know it's null, so MakeGenericType is effectively a no-op (will throw)
			// so no validation necessary.
			static void TestNullType ()
			{
				Type nullType = null;
				nullType.MakeGenericType (typeof (TestType));
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestUnknownInput (Type inputType)
			{
				inputType.MakeGenericType (typeof (TestType));
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestWithUnknownTypeArray (Type[] types)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestWithArrayUnknownIndexSet (int indexToSet)
			{
				Type[] types = new Type[1];
				types[indexToSet] = typeof (TestType);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestWithArrayUnknownLengthSet (int arrayLen)
			{
				Type[] types = new Type[arrayLen];
				types[0] = typeof (TestType);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			static void TestNoArguments ()
			{
				typeof (TypeMakeGenericNoArguments).MakeGenericType ();
			}

			class TypeMakeGenericNoArguments
			{
			}

			static void TestWithRequirements ()
			{
				// Currently this is not analyzable since we don't track array elements.
				// Would be really nice to support this kind of code in the future though.
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TestType));
			}

			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			// https://github.com/dotnet/linker/issues/2428
			// [ExpectedWarning ("IL2071", "'T'")]
			[ExpectedWarning ("IL2070", "'this'")]
			static void TestWithRequirementsFromParamWithMismatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (T));
			}

			// https://github.com/dotnet/linker/issues/2428
			// [ExpectedWarning ("IL2091", "'T'")]
			[ExpectedWarning ("IL2090", "'this'")] // Note that this actually produces a warning which should not be possible to produce right now
			static void TestWithRequirementsFromGenericParamWithMismatch<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInput> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TInput));
			}

			class GenericWithPublicFieldsArgument<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			{
			}

			static void TestWithNoRequirements ()
			{
				typeof (GenericWithNoRequirements<>).MakeGenericType (typeof (TestType));
			}

			static void TestWithNoRequirementsFromParam (Type type)
			{
				typeof (GenericWithNoRequirements<>).MakeGenericType (type);
			}

			class GenericWithNoRequirements<T>
			{
			}

			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (GenericWithMultipleArgumentsWithRequirements<,>).MakeGenericType (typeof (TestType), typeof (TestType));
			}

			class GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TTwo>
			{
			}

			static void TestWithNewConstraint ()
			{
				typeof (GenericWithNewConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithNewConstraint<T> where T : new()
			{
			}

			static void TestWithStructConstraint ()
			{
				typeof (GenericWithStructConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithStructConstraint<T> where T : struct
			{
			}

			static void TestWithUnmanagedConstraint ()
			{
				typeof (GenericWithUnmanagedConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithUnmanagedConstraint<T> where T : unmanaged
			{
			}

			static void TestWithNullable ()
			{
				typeof (Nullable<>).MakeGenericType (typeof (TestType));
			}
		}

		class MakeGenericMethod
		{
			public static void Test ()
			{
				TestNullMethod ();
				TestUnknownMethod (null);
				TestUnknownMethodButNoTypeArguments (null);
				TestWithUnknownTypeArray (null);
				TestWithArrayUnknownIndexSet (0);
				TestWithArrayUnknownIndexSetByRef (0);
				TestWithArrayUnknownLengthSet (1);
				TestWithArrayPassedToAnotherMethod ();
				TestWithNoArguments ();
				TestWithArgumentsButNonGenericMethod ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsViaRuntimeMethod ();
				TestWithRequirementsButNoTypeArguments ();

				TestWithMultipleKnownGenericParameters ();
				TestWithOneUnknownGenericParameter (null);
				TestWithPartiallyInitializedGenericTypeArray ();
				TestWithConditionalGenericTypeSet (true);

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);
				TestWithNoRequirementsViaRuntimeMethod ();
				TestWithNoRequirementsUnknownType (null);
				TestWithNoRequirementsWrongNumberOfTypeParameters ();
				TestWithNoRequirementsUnknownArrayElement ();

				TestWithMultipleArgumentsWithRequirements ();

				TestWithNewConstraint ();
				TestWithStructConstraint ();
				TestWithUnmanagedConstraint ();
			}

			static void TestNullMethod ()
			{
				MethodInfo mi = null;
				mi.MakeGenericMethod (typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestUnknownMethod (MethodInfo mi)
			{
				mi.MakeGenericMethod (typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestUnknownMethodButNoTypeArguments (MethodInfo mi)
			{
				// Thechnically linker could figure this out, but it's not worth the complexity - such call will always fail at runtime.
				mi.MakeGenericMethod (Type.EmptyTypes);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithUnknownTypeArray (Type[] types)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayUnknownIndexSet (int indexToSet)
			{
				Type[] types = new Type[1];
				types[indexToSet] = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayUnknownIndexSetByRef (int indexToSet)
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				ref Type t = ref types[indexToSet];
				t = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayUnknownLengthSet (int arrayLen)
			{
				Type[] types = new Type[arrayLen];
				types[0] = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void MethodThatTakesArrayParameter (Type[] types)
			{
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithArrayPassedToAnotherMethod ()
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				MethodThatTakesArrayParameter (types);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void TestWithNoArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod ();
			}

			// This should not warn since we can't be always sure about the exact overload as we don't support
			// method parameter signature matching, and thus the GetMethod may return multiple potential methods.
			// It can happen that some are generic and some are not. The analysis should not fail on this.
			static void TestWithArgumentsButNonGenericMethod ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void NonGenericMethod ()
			{
			}

			static void TestWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (type);
			}

			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (T));
			}


			static void TestWithRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithRequirementsButNoTypeArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (Type.EmptyTypes);
			}

			public static void GenericWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
			}

			static void TestWithMultipleKnownGenericParameters ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType), typeof (TestType), typeof (TestType));
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithOneUnknownGenericParameter (Type[] types)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType), typeof (TestStruct), types[0]);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithPartiallyInitializedGenericTypeArray ()
			{
				Type[] types = new Type[3];
				types[0] = typeof (TestType);
				types[1] = typeof (TestStruct);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void TestWithConditionalGenericTypeSet (bool thirdParameterIsStruct)
			{
				Type[] types = new Type[3];
				types[0] = typeof (TestType);
				types[1] = typeof (TestStruct);
				if (thirdParameterIsStruct) {
					types[2] = typeof (TestStruct);
				} else {
					types[2] = typeof (TestType);
				}
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			public static void GenericMultipleParameters<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] U,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] V> ()
			{
			}

			static void TestWithNoRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			static void TestWithNoRequirementsFromParam (Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static)
					.MakeGenericMethod (type);
			}

			static void TestWithNoRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithNoRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			// There are no requirements, so no warnings
			static void TestWithNoRequirementsUnknownType (Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (type);
			}

			// There are no requirements, so no warnings
			static void TestWithNoRequirementsWrongNumberOfTypeParameters ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (typeof (TestType), typeof (TestType));
			}

			// There are no requirements, so no warnings
			static void TestWithNoRequirementsUnknownArrayElement ()
			{
				Type[] types = new Type[1];
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (types);
			}

			public static void GenericWithNoRequirements<T> ()
			{
			}


			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithMultipleArgumentsWithRequirements), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType), typeof (TestType));
			}

			static void GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TTwo> ()
			{
			}

			static void TestWithNewConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithNewConstraint<T> () where T : new()
			{
				var t = new T ();
			}

			static void TestWithStructConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithStructConstraint<T> () where T : struct
			{
				var t = new T ();
			}

			static void TestWithUnmanagedConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithUnmanagedConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithUnmanagedConstraint<T> () where T : unmanaged
			{
				var t = new T ();
			}
		}

		public class TestType
		{
		}

		public struct TestStruct
		{
		}
	}
}
