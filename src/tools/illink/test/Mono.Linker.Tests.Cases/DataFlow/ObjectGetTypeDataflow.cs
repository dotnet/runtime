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
			[ExpectedWarning ("IL2072")]
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
				[ExpectedWarning ("IL2072")]
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
			[ExpectedWarning ("IL2072")]
			static void TestReturnType ()
			{
				ReturnType ().GetType ().RequiresPublicFields ();
			}

			[Kept]
			public static void Test ()
			{
				ParameterType (EnumType.Value);
				new FieldType (EnumType.Value).Test ();
				TestReturnType ();
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
				static T field;

				[Kept]
				[ExpectedWarning ("IL2072", Tool.NativeAot, "nativeaot tracks field type as EcmaGenericParameter")]
				// TODO: the field type is EcmaGenericParameter 'T', which is not a signature variable.
				// (It's not a def type, so goes to the first case, but doesn't get the enum special handling.)
				// Why is this EcmaGenericParameter while the method parameter is a signature variable?
				public static void Test ()
				{
					field.GetType ().RequiresPublicFields ();
				}
			}

			[Kept]
			class TypeGenericParameterAsParameter<T> where T : Enum
			{
				[Kept]
				static void Method (T instance)
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
				static T Method ()
				{
					return default;
				}

				[Kept]
				[ExpectedWarning ("IL2072", Tool.NativeAot, "nativeaot tracks return type as EcmaGenericParameter")]
				public static void Test ()
				{
					Method ().GetType ().RequiresPublicFields ();
				}
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
			// ILC tracks the static type of the return value as System.Enum,
			// which doesn't get special handling for GetType.
			[ExpectedWarning ("IL2072", Tool.NativeAot | Tool.Analyzer, "nativeaot tracks instantiated return type. analyzer does same.")]
			// ILLink's handling of this sees a return type of 'T', which
			// gets the new handling because T has an Enum constraint.
			static void TestMethodGenericParameterAsReturnType ()
			{
				MethodGenericParameterAsReturnType<Enum> ().GetType ().RequiresPublicFields ();
			}

			[Kept]
			public static void Test ()
			{
				TypeGenericParameterAsParameter<Enum>.Test ();
				TypeGenericParameterAsReturnType<Enum>.Test ();
				TypeGenericParameterAsField<Enum>.Test ();
				MethodGenericParameterAsParameter<Enum> (EnumType.Value);
				TestMethodGenericParameterAsReturnType ();
			}
		}
	}
}
