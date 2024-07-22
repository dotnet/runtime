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
			static void ConstrainedParameterType<T> (T instance) where T : Enum
			{
				instance.GetType ().RequiresPublicFields ();
			}

			[Kept]
			class ConstrainedFieldType<T> where T : Enum
			{
				[Kept]
				T field;

				[Kept]
				public ConstrainedFieldType (T instance) => field = instance;

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
			static T ConstrainedReturnType<T> () where T : Enum
			{
				return default;
			}

			[Kept]
			static void TestConstrainedReturnType ()
			{
				ConstrainedReturnType<Enum> ().GetType ().RequiresPublicFields ();
			}

			[Kept]
			public static void Test ()
			{
				ConstrainedParameterType<Enum> (EnumType.Value);
				new ConstrainedFieldType<Enum> (EnumType.Value).Test ();
				TestConstrainedReturnType ();
			}
		}
	}
}
