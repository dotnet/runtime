// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// Note: this test's goal is to validate that the product correctly reports unrecognized patterns
	//   - so the main validation is done by the ExpectedWarning attributes.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class FieldDataFlow
	{
		public static void Main ()
		{
			var instance = new FieldDataFlow ();

			instance.ReadFromInstanceField ();
			instance.WriteToInstanceField ();

			instance.ReadFromStaticField ();
			instance.WriteToStaticField ();

			instance.ReadFromInstanceFieldOnADifferentClass ();
			instance.WriteToInstanceFieldOnADifferentClass ();

			instance.ReadFromStaticFieldOnADifferentClass ();
			instance.WriteToStaticFieldOnADifferentClass ();

			instance.WriteUnknownValue ();

			WriteCapturedField.Test ();

			_ = _annotationOnWrongType;

			TestStringEmpty ();

			WriteArrayField.Test ();
			AccessReturnedInstanceField.Test ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		Type _typeWithPublicParameterlessConstructor;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		static Type _staticTypeWithPublicParameterlessConstructor;

		static Type _staticTypeWithoutRequirements;

		[ExpectedWarning ("IL2097", nameof (_annotationOnWrongType))]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static object _annotationOnWrongType;

		[ExpectedWarning ("IL2077", nameof (RequirePublicConstructors),
			"_typeWithPublicParameterlessConstructor", "type", "RequirePublicConstructors(Type)")]
		[ExpectedWarning ("IL2077", nameof (RequireNonPublicConstructors))]
		private void ReadFromInstanceField ()
		{
			RequirePublicParameterlessConstructor (_typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_typeWithPublicParameterlessConstructor);
			RequireNothing (_typeWithPublicParameterlessConstructor);
		}

		[ExpectedWarning ("IL2074", nameof (FieldDataFlow) + "." + nameof (_typeWithPublicParameterlessConstructor), nameof (GetUnknownType))]
		[ExpectedWarning ("IL2074", nameof (FieldDataFlow) + "." + nameof (_typeWithPublicParameterlessConstructor), nameof (GetTypeWithNonPublicConstructors))]
		private void WriteToInstanceField ()
		{
			_typeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			_typeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			_typeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			_typeWithPublicParameterlessConstructor = GetUnknownType ();
		}

		[ExpectedWarning ("IL2077", nameof (RequirePublicConstructors))]
		[ExpectedWarning ("IL2077", nameof (RequireNonPublicConstructors))]
		private void ReadFromInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			RequirePublicParameterlessConstructor (store._typeWithPublicParameterlessConstructor);
			RequirePublicConstructors (store._typeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (store._typeWithPublicParameterlessConstructor);
			RequireNothing (store._typeWithPublicParameterlessConstructor);
		}

		[ExpectedWarning ("IL2074", nameof (TypeStore) + "." + nameof (TypeStore._typeWithPublicParameterlessConstructor), nameof (GetUnknownType))]
		[ExpectedWarning ("IL2074", nameof (TypeStore) + "." + nameof (TypeStore._typeWithPublicParameterlessConstructor), nameof (GetTypeWithNonPublicConstructors))]
		private void WriteToInstanceFieldOnADifferentClass ()
		{
			var store = new TypeStore ();

			store._typeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			store._typeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			store._typeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			store._typeWithPublicParameterlessConstructor = GetUnknownType ();
		}

		[ExpectedWarning ("IL2077", nameof (RequirePublicConstructors))]
		[ExpectedWarning ("IL2077", nameof (RequireNonPublicConstructors))]
		private void ReadFromStaticField ()
		{
			RequirePublicParameterlessConstructor (_staticTypeWithPublicParameterlessConstructor);
			RequirePublicConstructors (_staticTypeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (_staticTypeWithPublicParameterlessConstructor);
			RequireNothing (_staticTypeWithPublicParameterlessConstructor);
		}

		[ExpectedWarning ("IL2074", nameof (FieldDataFlow) + "." + nameof (_staticTypeWithPublicParameterlessConstructor), nameof (GetUnknownType))]
		[ExpectedWarning ("IL2074", nameof (FieldDataFlow) + "." + nameof (_staticTypeWithPublicParameterlessConstructor), nameof (GetTypeWithNonPublicConstructors))]
		[ExpectedWarning ("IL2079", nameof (FieldDataFlow) + "." + nameof (_staticTypeWithPublicParameterlessConstructor), nameof (_staticTypeWithoutRequirements))]
		private void WriteToStaticField ()
		{
			_staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			_staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			_staticTypeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			_staticTypeWithPublicParameterlessConstructor = GetUnknownType ();
			_staticTypeWithPublicParameterlessConstructor = _staticTypeWithoutRequirements;
		}

		[ExpectedWarning ("IL2077", nameof (RequirePublicConstructors))]
		[ExpectedWarning ("IL2077", nameof (RequireNonPublicConstructors))]
		private void ReadFromStaticFieldOnADifferentClass ()
		{
			RequirePublicParameterlessConstructor (TypeStore._staticTypeWithPublicParameterlessConstructor);
			RequirePublicConstructors (TypeStore._staticTypeWithPublicParameterlessConstructor);
			RequireNonPublicConstructors (TypeStore._staticTypeWithPublicParameterlessConstructor);
			RequireNothing (TypeStore._staticTypeWithPublicParameterlessConstructor);
		}

		[ExpectedWarning ("IL2074", nameof (TypeStore) + "." + nameof (TypeStore._staticTypeWithPublicParameterlessConstructor), nameof (GetUnknownType))]
		[ExpectedWarning ("IL2074", nameof (TypeStore) + "." + nameof (TypeStore._staticTypeWithPublicParameterlessConstructor), nameof (GetTypeWithNonPublicConstructors))]
		private void WriteToStaticFieldOnADifferentClass ()
		{
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicParameterlessConstructor ();
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetTypeWithPublicConstructors ();
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetTypeWithNonPublicConstructors ();
			TypeStore._staticTypeWithPublicParameterlessConstructor = GetUnknownType ();
		}

		[ExpectedWarning ("IL2064", nameof (TypeStore) + "." + nameof (TypeStore._staticTypeWithPublicParameterlessConstructor))]
		private void WriteUnknownValue ()
		{
			var array = new object[1];
			array[0] = this.GetType ();
			MakeArrayValuesUnknown (array);
			TypeStore._staticTypeWithPublicParameterlessConstructor = (Type) array[0];

			static void MakeArrayValuesUnknown (object[] array)
			{
			}
		}

		private static void TestStringEmpty ()
		{
			RequirePublicMethods (string.Empty);
		}

		class WriteCapturedField
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
			static Type field;

			[ExpectedWarning ("IL2074", nameof (GetUnknownType), nameof (field))]
			[ExpectedWarning ("IL2074", nameof (GetTypeWithPublicConstructors), nameof (field))]
			static void TestNullCoalesce ()
			{
				field = GetUnknownType () ?? GetTypeWithPublicConstructors ();
			}

			[ExpectedWarning ("IL2074", nameof (GetUnknownType), nameof (field))]
			static void TestNullCoalescingAssignment ()
			{
				field ??= GetUnknownType ();
			}

			[ExpectedWarning ("IL2074", nameof (GetUnknownType), nameof (field))]
			[ExpectedWarning ("IL2074", nameof (GetTypeWithPublicConstructors), nameof (field))]
			static void TestNullCoalescingAssignmentComplex ()
			{
				field ??= GetUnknownType () ?? GetTypeWithPublicConstructors ();
			}

			public static void Test ()
			{
				TestNullCoalesce ();
				TestNullCoalescingAssignment ();
				TestNullCoalescingAssignmentComplex ();
			}
		}

		class AccessReturnedInstanceField
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			Type field;

			static AccessReturnedInstanceField GetInstance ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type unused) => null;

			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (GetInstance),
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)] // https://github.com/dotnet/linker/issues/2832
			[ExpectedWarning ("IL2077", nameof (field), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void TestRead ()
			{
				GetInstance (GetUnknownType ()).field.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (GetInstance),
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)] // https://github.com/dotnet/linker/issues/2832
			[ExpectedWarning ("IL2074", nameof (GetUnknownType), nameof (field))]
			static void TestWrite ()
			{
				GetInstance (GetUnknownType ()).field = GetUnknownType ();
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (GetInstance),
				ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)] // https://github.com/dotnet/linker/issues/2832
			[ExpectedWarning ("IL2074", nameof (GetUnknownType), nameof (field))]
			static void TestNullCoalescingAssignment ()
			{
				GetInstance (GetUnknownType ()).field ??= GetUnknownType ();
			}

			public static void Test ()
			{
				TestRead ();
				TestWrite ();
				TestNullCoalescingAssignment ();
			}
		}

		private static void RequirePublicMethods (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			string s)
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

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
		private static Type GetTypeWithPublicParameterlessConstructor ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		private static Type GetTypeWithNonPublicConstructors ()
		{
			return null;
		}

		private static Type GetUnknownType ()
		{
			return null;
		}

		private static void RequireNothing (Type type)
		{
		}

		class TypeStore
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public Type _typeWithPublicParameterlessConstructor;

			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
			public static Type _staticTypeWithPublicParameterlessConstructor;
		}

		class WriteArrayField
		{
			static Type[] ArrayField;

			static void TestAssignment ()
			{
				ArrayField = Array.Empty<Type> ();
			}

			static void TestCoalescingAssignment ()
			{
				ArrayField ??= Array.Empty<Type> ();
			}

			public static void Test ()
			{
				TestAssignment ();
				TestCoalescingAssignment ();
			}
		}
	}
}
