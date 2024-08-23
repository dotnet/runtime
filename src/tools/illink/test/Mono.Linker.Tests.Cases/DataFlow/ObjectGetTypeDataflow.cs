// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class ObjectGetTypeDataflow
	{
		public static void Main ()
		{
			SealedConstructorAsSource.Test ();
			InstantiatedGenericAsSource.Test ();
			EnumTypeSatisfiesPublicFields.Test ();
			EnumConstraintSatisfiesPublicFields.Test ();
			InstantiatedTypeParameterAsSource.Test ();
			EnumerationOverInstances.Test ();
			NullValue.Test ();
			NoValue.Test ();
			UnknownValue.Test ();
		}

		class SealedConstructorAsSource
		{
			public class Base
			{
			}

			public sealed class Derived : Base
			{
				[RequiresUnreferencedCode (nameof (Method))]
				public void Method () { }
			}

			[ExpectedWarning ("IL2026", nameof (Derived.Method))]
			public static void Test ()
			{
				new Derived ().GetType ().GetMethod ("Method");
			}
		}

		class InstantiatedGenericAsSource
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class Generic<T> {
				[ExpectedWarning ("IL2112")]
				[RequiresUnreferencedCode (nameof (KeptForMethodParameter))]
				public void KeptForMethodParameter () {}

				[ExpectedWarning ("IL2112")]
				[RequiresUnreferencedCode (nameof (KeptForField))]
				public void KeptForField () {}

				[ExpectedWarning ("IL2112")]
				[RequiresUnreferencedCode (nameof (KeptJustBecause))]
				public void KeptJustBecause () {}
			}

			static void TestMethodParameter (Generic<int> instance)
			{
				instance.GetType ().GetMethod ("KeptForMethodParameter");
			}

			static Generic<int> field = null;

			static void TestField ()
			{
				field.GetType ().GetMethod ("KeptForField");
			}

			public static void Test ()
			{
				TestMethodParameter (null);
				TestField ();
			}
		}

		class EnumTypeSatisfiesPublicFields
		{
			static void ParameterType (Enum instance)
			{
				instance.GetType ().RequiresPublicFields ();
			}

			class FieldType
			{
				Enum field;

				public FieldType (Enum instance) => field = instance;

				public void Test ()
				{
					field.GetType ().RequiresPublicFields ();
				}
			}

			enum EnumType
			{
				Value
			}

			static Enum ReturnType ()
			{
				return EnumType.Value;
			}

			static void TestReturnType ()
			{
				ReturnType ().GetType ().RequiresPublicFields ();
			}

			static void OutParameter (out Enum value)
			{
				value = EnumType.Value;
			}

			// Analyzer doesn't assign a value to the out parameter after calling the OutParameter method,
			// so when it looks up the value of the local 'value', it returns an empty value, and the
			// GetType intrinsic handling can't see that the out param satisfies the public fields requirement.
			// Similar for the other cases below.
			[ExpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101734")]
			static void TestOutParameter ()
			{
				OutParameter (out var value);
				value.GetType ().RequiresPublicFields ();
			}

			public static void Test ()
			{
				ParameterType (EnumType.Value);
				new FieldType (EnumType.Value).Test ();
				TestReturnType ();
				TestOutParameter ();
			}
		}

		class EnumConstraintSatisfiesPublicFields
		{
			static void MethodGenericParameterAsParameter<T> (T instance) where T : Enum
			{
				instance.GetType ().RequiresPublicFields ();
			}

			class TypeGenericParameterAsField<T> where T : Enum
			{
				public static T field;

				public static void TestAccessFromType ()
				{
					field.GetType ().RequiresPublicFields ();
				}
			}

			static void TestAccessTypeGenericParameterAsField ()
			{
				TypeGenericParameterAsField<Enum>.field.GetType ().RequiresPublicFields ();
			}

			class TypeGenericParameterAsParameter<T> where T : Enum
			{
				public static void Method (T instance)
				{
					instance.GetType ().RequiresPublicFields ();
				}

				public static void Test ()
				{
					Method (default);
				}
			}

			class TypeGenericParameterAsReturnType<T> where T : Enum
			{
				public static T Method ()
				{
					return default;
				}

				public static void TestAccessFromType ()
				{
					Method ().GetType ().RequiresPublicFields ();
				}
			}

			static void TestAccessTypeGenericParameterAsReturnType ()
			{
				TypeGenericParameterAsReturnType<Enum>.Method ().GetType ().RequiresPublicFields ();
			}

			class TypeGenericParameterAsOutParam<T> where T : Enum
			{
				public static void Method (out T instance)
				{
					instance = default;
				}

				[ExpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101734")]
				public static void TestAccessFromType ()
				{
					Method (out var instance);
					instance.GetType ().RequiresPublicFields ();
				}
			}

			[ExpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101734")]
			static void TestAccessTypeGenericParameterAsOutParam ()
			{
				TypeGenericParameterAsOutParam<Enum>.Method (out var instance);
				instance.GetType ().RequiresPublicFields ();
			}

			enum EnumType
			{
				Value
			}

			static T MethodGenericParameterAsReturnType<T> () where T : Enum
			{
				return default;
			}

			static void TestMethodGenericParameterAsReturnType ()
			{
				MethodGenericParameterAsReturnType<Enum> ().GetType ().RequiresPublicFields ();
			}

			public static void Test ()
			{
				TypeGenericParameterAsParameter<Enum>.Test ();
				TypeGenericParameterAsReturnType<Enum>.TestAccessFromType ();
				TestAccessTypeGenericParameterAsReturnType ();
				TypeGenericParameterAsOutParam<Enum>.TestAccessFromType ();
				TestAccessTypeGenericParameterAsOutParam ();
				TypeGenericParameterAsField<Enum>.TestAccessFromType ();
				TestAccessTypeGenericParameterAsField ();
				MethodGenericParameterAsParameter<Enum> (EnumType.Value);
				TestMethodGenericParameterAsReturnType ();
			}
		}

		class InstantiatedTypeParameterAsSource
		{
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
			class Annotated {}

			static T GenericReturnType<T> () => default;

			static void TestGenericMethodReturnType ()
			{
				GenericReturnType<Annotated> ().GetType ().RequiresPublicFields ();
			}

			static void GenericOutParameter<T> (out T value) => value = default;

			[UnexpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101734")]
			static void TestGenericMethodOutParameter ()
			{
				GenericOutParameter (out Annotated value);
				value.GetType ().RequiresPublicFields ();
			}

			class Generic<T>
			{
				public static T ReturnType () => default;

				public static void OutParameter (out T value) => value = default;

				public static T field;

				public static T Property { get; }

				public class Nested {
					public static T ReturnType () => default;

					public static void OutParameter (out T value) => value = default;

					public static T field;

					public static T Property { get; }
				}
			}

			static void TestGenericClassReturnType ()
			{
				Generic<Annotated>.ReturnType ().GetType ().RequiresPublicFields ();
			}

			[UnexpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101734")]
			static void TestGenericClassOutParameter ()
			{
				Generic<Annotated>.OutParameter (out var value);
				value.GetType ().RequiresPublicFields ();
			}

			static void TestGenericClassField ()
			{
				Generic<Annotated>.field.GetType ().RequiresPublicFields ();
			}

			static void TestGenericClassProperty ()
			{
				Generic<Annotated>.Property.GetType ().RequiresPublicFields ();
			}

			static void TestNestedGenericClassReturnType ()
			{
				Generic<Annotated>.Nested.ReturnType ().GetType ().RequiresPublicFields ();
			}

			[UnexpectedWarning ("IL2072", Tool.Analyzer, "https://github.com/dotnet/runtime/issues/101734")]
			static void TestNestedGenericClassOutParameter ()
			{
				Generic<Annotated>.Nested.OutParameter (out var value);
				value.GetType ().RequiresPublicFields ();
			}

			static void TestNestedGenericClassField ()
			{
				Generic<Annotated>.Nested.field.GetType ().RequiresPublicFields ();
			}

			static void TestNestedGenericClassProperty ()
			{
				Generic<Annotated>.Nested.Property.GetType ().RequiresPublicFields ();
			}

			public static void Test ()
			{
				TestGenericMethodReturnType ();
				TestGenericMethodOutParameter ();
				TestGenericClassReturnType ();
				TestGenericClassOutParameter ();
				TestGenericClassField ();
				TestGenericClassProperty ();
				TestNestedGenericClassReturnType ();
				TestNestedGenericClassOutParameter ();
				TestNestedGenericClassField ();
				TestNestedGenericClassProperty ();
			}
		}

		class EnumerationOverInstances
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class AnnotatedBase
			{
			}

			class Derived : AnnotatedBase
			{
				public void Method () { }
			}

			static IEnumerable<AnnotatedBase> GetInstances () => new AnnotatedBase[] { new Derived () };

			public static void Test ()
			{
				foreach (var instance in GetInstances ()) {
					instance.GetType ().GetMethod ("Method");
				}
			}
		}

		class NullValue
		{
			class TestType
			{
			}

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Object.GetType) + "()")]
			public static void Test ()
			{
				TestType nullInstance = null;
				// Even though this throws at runtime, we warn about the return value of GetType
				nullInstance.GetType ().RequiresAll ();
			}
		}

		class NoValue
		{
			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Object.GetType) + "()")]
			static void TestGetTypeOfType ()
			{
				Type t = null;
				Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
				// Even though the above throws at runtime, we warn about the return value of GetType.
				// This doesn't go through the intrinsic handling because this goes to the newslot method Type.GetType,
				// instead of the intrinsically handled object.GetType.
				noValue.GetType ().RequiresAll ();
			}

			static void TestGetTypeOfMethod ()
			{
				Type t = null;
				MethodInfo noValue = t.GetMethod ("Method");
				noValue.GetType ().RequiresAll ();
			}

			public static void Test ()
			{
				TestGetTypeOfType ();
				TestGetTypeOfMethod ();
			}
		}

		class UnknownValue
		{
			class TestType
			{
			}

			static TestType GetInstance () => new TestType ();

			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (Object.GetType) + "()")]
			public static void Test ()
			{
				TestType unknownValue = GetInstance ();
				// Should warn about the return value of GetType
				unknownValue.GetType ().RequiresAll ();
			}
		}
	}
}
