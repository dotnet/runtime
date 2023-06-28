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
	[UnconditionalSuppressMessage ("AOT", "IL3050", Justification = "These tests are not targetted at AOT scenarios")]
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
				TestNoValueInput ();
				TestUnknownInput (null);
				TestNullTypeArgument ();
				TestNoValueTypeArgument ();
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

				NewConstraint.Test ();
				StructConstraint.Test ();
				UnmanagedConstraint.Test ();
				TestWithNullable ();
			}

			// This is OK since we know it's null, so MakeGenericType is effectively a no-op (will throw)
			// so no validation necessary.
			static void TestNullType ()
			{
				Type nullType = null;
				nullType.MakeGenericType (typeof (TestType));
			}

			static void TestNoValueInput ()
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
				noValue.MakeGenericType (typeof (TestType));
			}

			static void TestNullTypeArgument ()
			{
				Type t = null;
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (t);
			}

			static void TestNoValueTypeArgument ()
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (noValue);
			}

			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType))]
			static void TestUnknownInput (Type inputType)
			{
				inputType.MakeGenericType (typeof (TestType));
			}

			// https://github.com/dotnet/linker/issues/2755
			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			// https://github.com/dotnet/linker/issues/2755
			[ExpectedWarning ("IL2055", nameof (Type.MakeGenericType), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			class NewConstraint
			{
				class GenericWithNewConstraint<T> where T : new()
				{
				}

				class GenericWithNewConstraintAndAnnotations<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> where T : new()
				{
				}

				static void SpecificType ()
				{
					typeof (GenericWithNewConstraint<>).MakeGenericType (typeof (TestType));
				}

				[ExpectedWarning ("IL2070")]
				static void UnknownType (Type unknownType = null)
				{
					typeof (GenericWithNewConstraint<>).MakeGenericType (unknownType);
				}

				static void AnnotationMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type withCtor = null)
				{
					typeof (GenericWithNewConstraint<>).MakeGenericType (withCtor);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withPublicMethods = null)
				{
					typeof (GenericWithNewConstraint<>).MakeGenericType (withPublicMethods);
				}

				static void AnnotationAndConstraintMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] Type withMethodsAndCtors = null)
				{
					typeof (GenericWithNewConstraintAndAnnotations<>).MakeGenericType (withMethodsAndCtors);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationAndConstraintMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withMethods = null)
				{
					typeof (GenericWithNewConstraintAndAnnotations<>).MakeGenericType (withMethods);
				}

				public static void Test ()
				{
					SpecificType ();
					UnknownType ();
					AnnotationMatch ();
					AnnotationMismatch ();
					AnnotationAndConstraintMatch ();
					AnnotationAndConstraintMismatch ();
				}
			}

			class StructConstraint
			{
				class GenericWithStructConstraint<T> where T : struct
				{
				}

				static void SpecificType ()
				{
					typeof (GenericWithStructConstraint<>).MakeGenericType (typeof (TestType));
				}

				static void AnnotationMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type withCtor = null)
				{
					typeof (GenericWithStructConstraint<>).MakeGenericType (withCtor);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withPublicMethods = null)
				{
					typeof (GenericWithStructConstraint<>).MakeGenericType (withPublicMethods);
				}

				public static void Test ()
				{
					SpecificType ();
					AnnotationMatch ();
					AnnotationMismatch ();
				}
			}

			class UnmanagedConstraint
			{
				class GenericWithUnmanagedConstraint<T> where T : unmanaged
				{
				}

				static void SpecificType ()
				{
					typeof (GenericWithUnmanagedConstraint<>).MakeGenericType (typeof (TestType));
				}

				static void AnnotationMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type withCtor = null)
				{
					typeof (GenericWithUnmanagedConstraint<>).MakeGenericType (withCtor);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withPublicMethods = null)
				{
					typeof (GenericWithUnmanagedConstraint<>).MakeGenericType (withPublicMethods);
				}

				public static void Test ()
				{
					SpecificType ();
					AnnotationMatch ();
					AnnotationMismatch ();
				}
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
				TestNullMethodName ();
				TestNullMethodName_GetRuntimeMethod ();
				TestMethodOnNullType ();
				TestMethodOnNullType_GetRuntimeMethod ();
				TestWithEmptyInputToGetMethod (null);
				TestWithEmptyInputToGetMethod_GetRuntimeMethod (null);
				TestWithEmptyInputNoSuchMethod (null);
				TestWithEmptyInputNoSuchMethod_GetRuntimeMethod (null);
				TestUnknownMethod (null);
				TestUnknownMethodButNoTypeArguments (null);
				TestWithMultipleTypes ();
				TestWithMultipleTypes_GetRuntimeMethod ();
				TestWithMultipleNames ();
				TestWithMultipleNames_GetRuntimeMethod ();
				TestNullTypeArgument ();
				TestNoValueTypeArgument ();
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

				StaticInterfaceMethods.Test ();
				NewConstraint.Test ();
				StructConstraint.Test ();
				UnmanagedConstraint.Test ();

				TestGetMethodFromHandle ();
				TestGetMethodFromHandleWithWarning ();
			}

			static void TestNullMethod ()
			{
				MethodInfo mi = null;
				mi.MakeGenericMethod (typeof (TestType));
			}

			static void TestNullMethodName ()
			{
				// GetMethod(null) throws at runtime.
				MethodInfo noValue = typeof (MakeGenericMethod).GetMethod (null);
				noValue.MakeGenericMethod (typeof (TestType));
			}

			static void TestNullMethodName_GetRuntimeMethod ()
			{
				// GetRuntimeMethod(null, ...) throws at runtime.
				MethodInfo noValue = typeof (MakeGenericMethod).GetRuntimeMethod (null, new Type[] { });
				noValue.MakeGenericMethod (typeof (TestType));
			}

			static void TestMethodOnNullType ()
			{
				Type t = null;
				MethodInfo noValue = t.GetMethod (null);
				noValue.MakeGenericMethod (typeof (TestType));
			}

			static void TestMethodOnNullType_GetRuntimeMethod ()
			{
				Type t = null;
				MethodInfo noValue = t.GetRuntimeMethod (null, new Type[] { });
				noValue.MakeGenericMethod (typeof (TestType));
			}

			static void TestWithEmptyInputToGetMethod (Type unknownType)
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle); // Returns empty value set (throws at runtime)
																	  // No warning - since there's no method on input
				noValue.GetMethod ("NoMethod").MakeGenericMethod (unknownType);
			}

			static void TestWithEmptyInputToGetMethod_GetRuntimeMethod (Type unknownType)
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle); // Returns empty value set (throws at runtime)
																	  // No warning - since there's no method on input
				noValue.GetRuntimeMethod ("NoMethod", new Type[] { }).MakeGenericMethod (unknownType);
			}

			static void TestWithEmptyInputNoSuchMethod (Type unknownType)
			{
				// No warning - the method doesn't exist, so this should throw at runtime anyway
				typeof (TestType).GetMethod ("NoSuchMethod").MakeGenericMethod (unknownType);
			}

			static void TestWithEmptyInputNoSuchMethod_GetRuntimeMethod (Type unknownType)
			{
				// No warning - the method doesn't exist, so this should throw at runtime anyway
				typeof (TestType).GetRuntimeMethod ("NoSuchMethod", new Type[] { }).MakeGenericMethod (unknownType);
			}

			// https://github.com/dotnet/linker/issues/2755
			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void TestUnknownMethod (MethodInfo mi)
			{
				mi.MakeGenericMethod (typeof (TestType));
			}

			// https://github.com/dotnet/linker/issues/2755
			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void TestUnknownMethodButNoTypeArguments (MethodInfo mi)
			{
				// Technically trimming could figure this out, but it's not worth the complexity - such call will always fail at runtime.
				mi.MakeGenericMethod (Type.EmptyTypes);
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithMultipleTypes (
				int p = 0,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type annotatedType = null)
			{
				Type t = null;
				switch (p) {
				case 0:
					t = typeof (MakeGenericMethod);
					break;
				case 1:
					t = null;
					break;
				case 2:
					t = annotatedType;
					break;
				}

				// This should warn just once due to case 2 - annotated type, but unknown method
				t.GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static).MakeGenericMethod (new Type[] { typeof (TestType) });
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithMultipleTypes_GetRuntimeMethod (
				int p = 0,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type annotatedType = null)
			{
				Type t = null;
				switch (p) {
				case 0:
					t = typeof (MakeGenericMethod);
					break;
				case 1:
					t = null;
					break;
				case 2:
					t = annotatedType;
					break;
				}

				// This should warn just once due to case 2 - annotated type, but unknown method
				t.GetRuntimeMethod (nameof (GenericWithNoRequirements), new Type[] { }).MakeGenericMethod (new Type[] { typeof (TestType) });
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithMultipleNames (
				int p = 0,
				string unknownName = null)
			{
				string name = null;
				switch (p) {
				case 0:
					name = nameof (GenericWithNoRequirements);
					break;
				case 1:
					name = null;
					break;
				case 2:
					name = unknownName;
					break;
				}

				// This should warn just once due to case 2 - unknown name
				typeof (MakeGenericMethod).GetMethod (name, BindingFlags.Static).MakeGenericMethod (new Type[] { typeof (TestType) });
			}

			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod))]
			static void TestWithMultipleNames_GetRuntimeMethod (
				int p = 0,
				string unknownName = null)
			{
				string name = null;
				switch (p) {
				case 0:
					name = nameof (GenericWithNoRequirements);
					break;
				case 1:
					name = null;
					break;
				case 2:
					name = unknownName;
					break;
				}

				// This should warn just once due to case 2 - unknown name
				typeof (MakeGenericMethod).GetRuntimeMethod (name, new Type[] { }).MakeGenericMethod (new Type[] { typeof (TestType) });
			}

			static void TestNullTypeArgument ()
			{
				Type t = null;
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (t);
			}

			static void TestNoValueTypeArgument ()
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (noValue);
			}

			// https://github.com/dotnet/linker/issues/2755
			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			// https://github.com/dotnet/linker/issues/2158 - analyzer doesn't work the same as ILLink, it simply doesn't handle refs
			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			static void TestWithArrayUnknownIndexSetByRef (int indexToSet)
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				ref Type t = ref types[indexToSet];
				t = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			// https://github.com/dotnet/linker/issues/2755
			[ExpectedWarning ("IL2060", nameof (MethodInfo.MakeGenericMethod), ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			class StaticInterfaceMethods
			{
				public static void Test ()
				{
					KnownType ();
					UnannotatedGenericParam<int> ();
					AnnotatedGenericParam<int> ();
				}

				static MethodInfo KnownType ()
					=> typeof (IFoo).GetMethod ("Method")
					.MakeGenericMethod (new Type[] { typeof (int) });

				[ExpectedWarning ("IL2090", "T", "PublicMethods")]
				static MethodInfo UnannotatedGenericParam<T> ()
					=> typeof (IFoo).GetMethod ("Method")
					.MakeGenericMethod (new Type[] { typeof (T) });

				static MethodInfo AnnotatedGenericParam<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
					=> typeof (IFoo).GetMethod ("Method")
					.MakeGenericMethod (new Type[] { typeof (T) });

				interface IFoo
				{
					static abstract T Method<
						[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();
				}
			}

			class NewConstraint
			{
				static void GenericWithNewConstraint<T> () where T : new()
				{
					var t = new T ();
				}

				static void GenericWithNewConstraintAndAnnotations<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> () where T : new()
				{
				}

				static void SpecificType ()
				{
					typeof (NewConstraint).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (typeof (TestType));
				}

				[ExpectedWarning ("IL2070")]
				static void UnknownType (Type unknownType = null)
				{
					typeof (NewConstraint).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (unknownType);
				}

				static void AnnotationMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type withCtor = null)
				{
					typeof (NewConstraint).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withCtor);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withPublicMethods = null)
				{
					typeof (NewConstraint).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withPublicMethods);
				}

				static void AnnotationAndConstraintMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicConstructors)] Type withMethodsAndCtors = null)
				{
					typeof (NewConstraint).GetMethod (nameof (GenericWithNewConstraintAndAnnotations), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withMethodsAndCtors);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationAndConstraintMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withMethods = null)
				{
					typeof (NewConstraint).GetMethod (nameof (GenericWithNewConstraintAndAnnotations), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withMethods);
				}

				public static void Test ()
				{
					SpecificType ();
					UnknownType ();
					AnnotationMatch ();
					AnnotationMismatch ();
					AnnotationAndConstraintMatch ();
					AnnotationAndConstraintMismatch ();
				}
			}

			class StructConstraint
			{
				static void GenericWithStructConstraint<T> () where T : struct
				{
					var t = new T ();
				}

				static void SpecificType ()
				{
					typeof (StructConstraint).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (typeof (TestType));
				}

				static void AnnotationMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type withCtor = null)
				{
					typeof (StructConstraint).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withCtor);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withPublicMethods = null)
				{
					typeof (StructConstraint).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withPublicMethods);
				}

				public static void Test ()
				{
					SpecificType ();
					AnnotationMatch ();
					AnnotationMismatch ();
				}
			}

			class UnmanagedConstraint
			{
				static void GenericWithUnmanagedConstraint<T> () where T : unmanaged
				{
					var t = new T ();
				}

				static void SpecificType ()
				{
					typeof (UnmanagedConstraint).GetMethod (nameof (GenericWithUnmanagedConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (typeof (TestType));
				}

				static void AnnotationMatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type withCtor = null)
				{
					typeof (UnmanagedConstraint).GetMethod (nameof (GenericWithUnmanagedConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withCtor);
				}

				[ExpectedWarning ("IL2070")]
				static void AnnotationMismatch ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type withPublicMethods = null)
				{
					typeof (UnmanagedConstraint).GetMethod (nameof (GenericWithUnmanagedConstraint), BindingFlags.Static | BindingFlags.NonPublic)
						.MakeGenericMethod (withPublicMethods);
				}

				public static void Test ()
				{
					SpecificType ();
					AnnotationMatch ();
					AnnotationMismatch ();
				}
			}


			static void TestGetMethodFromHandle (Type unknownType = null)
			{
				MethodInfo m = (MethodInfo) MethodInfo.GetMethodFromHandle (typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements)).MethodHandle);
				m.MakeGenericMethod (unknownType);
			}

			[ExpectedWarning ("IL2070", nameof (MethodInfo.MakeGenericMethod))]
			static void TestGetMethodFromHandleWithWarning ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type publicMethodsType = null)
			{
				MethodInfo m = (MethodInfo) MethodInfo.GetMethodFromHandle (typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements)).MethodHandle);
				m.MakeGenericMethod (publicMethodsType);
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
