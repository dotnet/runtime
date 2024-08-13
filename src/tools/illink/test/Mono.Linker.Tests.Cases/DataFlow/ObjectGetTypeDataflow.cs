// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
		}

		class SealedConstructorAsSource
		{
			[KeptMember (".ctor()")]
			public class Base
			{
			}

			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (Base))]
			public sealed class Derived : Base
			{
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (Method))]
				public void Method () { }
			}

			[ExpectedWarning ("IL2026", nameof (Derived.Method))]
			public static void Test ()
			{
				new Derived ().GetType ().GetMethod ("Method");
			}
		}

		[Kept]
		class InstantiatedGenericAsSource
		{
			[Kept]
			[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			class Generic<T> {
				[Kept]
				[ExpectedWarning ("IL2112", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/102002")]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (KeptForMethodParameter))]
				public void KeptForMethodParameter () {}

				[Kept]
				[ExpectedWarning ("IL2112", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/102002")]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (KeptForField))]
				public void KeptForField () {}

				[Kept]
				[ExpectedWarning ("IL2112", Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/102002")]
				[KeptAttributeAttribute (typeof (RequiresUnreferencedCodeAttribute))]
				[RequiresUnreferencedCode (nameof (KeptJustBecause))]
				public void KeptJustBecause () {}
			}

			[Kept]
			static void TestMethodParameter (Generic<int> instance)
			{
				instance.GetType ().GetMethod ("KeptForMethodParameter");
			}

			[Kept]
			static Generic<int> field = null;

			[Kept]
			static void TestField ()
			{
				field.GetType ().GetMethod ("KeptForField");
			}

			[Kept]
			public static void Test ()
			{
				TestMethodParameter (null);
				TestField ();
			}
		}

		[Kept]
		class EnumTypeSatisfiesPublicFields
		{
			[Kept]
			static void ParameterType (Enum instance)
			{
				instance.GetType ().RequiresPublicFields ();
			}

			[Kept]
			class FieldType
			{
				[Kept]
				Enum field;

				[Kept]
				public FieldType (Enum instance) => field = instance;

				[Kept]
				public void Test ()
				{
					field.GetType ().RequiresPublicFields ();
				}
			}

			[KeptMember ("value__")]
			enum EnumType
			{
				[Kept]
				Value
			}

			[Kept]
			static Enum ReturnType ()
			{
				return EnumType.Value;
			}

			[Kept]
			static void TestReturnType ()
			{
				ReturnType ().GetType ().RequiresPublicFields ();
			}

			[Kept]
			static void OutParameter (out Enum value)
			{
				value = EnumType.Value;
			}

			[Kept]
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

			[Kept]
			public static void Test ()
			{
				ParameterType (EnumType.Value);
				new FieldType (EnumType.Value).Test ();
				TestReturnType ();
				TestOutParameter ();
			}
		}

		[Kept]
		class EnumConstraintSatisfiesPublicFields
		{
			[Kept]
			static void MethodGenericParameterAsParameter<T> (T instance) where T : Enum
			{
				instance.GetType ().RequiresPublicFields ();
			}

			[Kept]
			class TypeGenericParameterAsField<T> where T : Enum
			{
				[Kept]
				public static T field;

				[Kept]
				public static void TestAccessFromType ()
				{
					field.GetType ().RequiresPublicFields ();
				}
			}

			static void TestAccessTypeGenericParameterAsField ()
			{
				TypeGenericParameterAsField<Enum>.field.GetType ().RequiresPublicFields ();
			}

			[Kept]
			class TypeGenericParameterAsParameter<T> where T : Enum
			{
				[Kept]
				public static void Method (T instance)
				{
					instance.GetType ().RequiresPublicFields ();
				}

				[Kept]
				public static void Test ()
				{
					Method (default);
				}
			}

			[Kept]
			class TypeGenericParameterAsReturnType<T> where T : Enum
			{
				[Kept]
				public static T Method ()
				{
					return default;
				}

				[Kept]
				public static void TestAccessFromType ()
				{
					Method ().GetType ().RequiresPublicFields ();
				}
			}

			static void TestAccessTypeGenericParameterAsReturnType ()
			{
				TypeGenericParameterAsReturnType<Enum>.Method ().GetType ().RequiresPublicFields ();
			}

			[Kept]
			class TypeGenericParameterAsOutParam<T> where T : Enum
			{
				[Kept]
				public static void Method (out T instance)
				{
					instance = default;
				}

				[Kept]
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

			[KeptMember ("value__")]
			enum EnumType
			{
				[Kept]
				Value
			}

			[Kept]
			static T MethodGenericParameterAsReturnType<T> () where T : Enum
			{
				return default;
			}

			[Kept]
			static void TestMethodGenericParameterAsReturnType ()
			{
				MethodGenericParameterAsReturnType<Enum> ().GetType ().RequiresPublicFields ();
			}

			[Kept]
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
			}

			static void TestGenericClassReturnType ()
			{
				Generic<Annotated>.ReturnType ().GetType ().RequiresPublicFields ();
			}

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

			public static void Test ()
			{
				TestGenericMethodReturnType ();
				TestGenericMethodOutParameter ();
				TestGenericClassReturnType ();
				TestGenericClassOutParameter ();
				TestGenericClassField ();
				TestGenericClassProperty ();
			}
		}
	}
}
