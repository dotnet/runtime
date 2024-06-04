// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class ArrayDataFlow
	{
		public static void Main ()
		{
			TestArrayWithInitializerOneElementStaticType ();
			TestArrayWithInitializerOneElementParameter (typeof (TestType));
			TestArrayWithInitializerMultipleElementsStaticType ();
			TestArrayWithInitializerMultipleElementsMix<TestType> (typeof (TestType));

			TestArraySetElementOneElementStaticType ();
			TestArraySetElementOneElementMix ();
			TestArraySetElementOneElementMerged ();
			TestArraySetElementOneElementParameter (typeof (TestType));
			TestArraySetElementMultipleElementsStaticType ();
			TestMergedArrayElement (1);
			TestArraySetElementMultipleElementsMix<TestType> (typeof (TestType));

			TestArraySetElementAndInitializerMultipleElementsMix<TestType> (typeof (TestType));

			TestGetElementAtUnknownIndex ();
			TestGetMergedArrayElement ();
			TestMergedArrayElementWithUnknownIndex (0);

			// Array reset - certain operations on array are not tracked fully (or impossible due to unknown inputs)
			// and sometimes the only valid thing to do is to reset the array to all unknowns as it's impossible
			// to determine what the operation did to the array. So after the reset, everything in the array
			// should be treated as unknown value.
			TestArrayResetStoreUnknownIndex ();
			TestArrayResetGetElementOnByRefArray ();
			TestArrayResetAfterCall ();
			TestArrayResetAfterAssignment ();

			TestArrayRecursion ();

			TestMultiDimensionalArray.Test ();

			WriteCapturedArrayElement.Test ();

			WriteElementOfCapturedArray.Test ();

			ConstantFieldValuesAsIndex.Test ();

			HoistedArrayMutation.Test ();
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerOneElementStaticType ()
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerOneElementParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
			Type[] arr = new Type[] { type };
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerMultipleElementsStaticType ()
		{
			Type[] arr = new Type[] { typeof (TestType), typeof (TestType), typeof (TestType) };
			arr[0].RequiresAll ();
			arr[1].RequiresAll ();
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArrayWithInitializerMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
		{
			Type[] arr = new Type[] { typeof (TestType), typeof (TProperties), typeAll };
			arr[0].RequiresAll ();
			arr[1].RequiresPublicProperties ();
			arr[1].RequiresPublicFields (); // Should warn - member types mismatch
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementOneElementStaticType ()
		{
			Type[] arr = new Type[1];
			arr[0] = typeof (TestType);
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll))]
		[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestArraySetElementOneElementMix ()
		{
			Type[] arr = new Type[1];
			if (string.Empty.Length == 0)
				arr[0] = GetUnknownType ();
			else
				arr[0] = GetTypeWithPublicConstructors ();
			arr[0].RequiresAll ();
		}

		[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2737")]
		[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2737")]
		[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2737")]
		static void TestArraySetElementOneElementMerged ()
		{
			Type[] arr = new Type[1];
			arr[0] = string.Empty.Length == 0 ? GetUnknownType () : GetTypeWithPublicConstructors ();
			arr[0].RequiresAll ();
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementOneElementParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
		{
			Type[] arr = new Type[1];
			arr[0] = type;
			arr[0].RequiresAll ();
			arr[1].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementMultipleElementsStaticType ()
		{
			Type[] arr = new Type[3];
			arr[0] = typeof (TestType);
			arr[1] = typeof (TestType);
			arr[2] = typeof (TestType);
			arr[0].RequiresAll ();
			arr[1].RequiresAll ();
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2072", nameof (ArrayDataFlow.GetMethods))]
		[ExpectedWarning ("IL2072", nameof (ArrayDataFlow.GetFields))]
		static void TestMergedArrayElement (int i)
		{
			Type[] arr = new Type[] { null };
			if (i == 1)
				arr[0] = GetMethods ();
			else
				arr[0] = GetFields ();
			arr[0].RequiresAll (); // Should warn - Methods/Fields does not have match annotations with All.
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetMethods () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetFields () => null;

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
		{
			Type[] arr = new Type[3];
			arr[0] = typeof (TestType);
			arr[1] = typeof (TProperties);
			arr[2] = typeAll;
			arr[0].RequiresAll ();

			arr[1].RequiresPublicProperties ();
			arr[1].RequiresPublicFields (); // Should warn
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestArraySetElementAndInitializerMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
		{
			Type[] arr = new Type[] { typeof (TestType), null, null };
			arr[1] = typeof (TProperties);
			arr[2] = typeAll;
			arr[0].RequiresAll ();
			arr[1].RequiresPublicProperties ();
			arr[1].RequiresPublicFields (); // Should warn
			arr[2].RequiresAll ();
			arr[3].RequiresPublicMethods (); // Should warn - unknown value at this index
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestGetElementAtUnknownIndex (int i = 0)
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[i].RequiresPublicFields ();
		}

		[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/93416")]
		[ExpectedWarning ("IL2072", [nameof (GetMethods), nameof (DataFlowTypeExtensions.RequiresAll)], Tool.Analyzer, "https://github.com/dotnet/runtime/issues/93416")]
		[ExpectedWarning ("IL2072", [nameof (GetFields), nameof (DataFlowTypeExtensions.RequiresAll)], Tool.Analyzer, "https://github.com/dotnet/runtime/issues/93416")]
		static void TestGetMergedArrayElement (bool b = true)
		{
			Type[] arr = new Type[] { GetMethods () };
			Type[] arr2 = new Type[] { GetFields () };
			if (b)
				arr = arr2;
			arr[0].RequiresAll ();
		}

		// Trimmer code doesn't handle locals from different branches separately, therefore merges incorrectly GetMethods with Unknown producing both warnings
		[UnexpectedWarning ("IL2072", nameof (ArrayDataFlow.GetMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/93416")]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll))]
		static void TestMergedArrayElementWithUnknownIndex (int i)
		{
			Type[] arr = new Type[] { null };
			if (i == 1)
				arr[0] = GetMethods ();
			else
				arr[i] = GetFields ();
			arr[0].RequiresAll (); // Should warn - there is an unknown value on fields therefore the merged value should be unknown
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestArrayResetStoreUnknownIndex (int i = 0)
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			arr[i] = typeof (TestType); // Unknown index - we reset array to all unknowns

			arr[0].RequiresPublicFields (); // Warns
		}

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2680")]
		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2680")]
		static void TestArrayResetGetElementOnByRefArray (int i = 0)
		{
			Type[] arr = new Type[] { typeof (TestType), typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			TakesTypeByRef (ref arr[0]); // Should reset index 0 - analyzer doesn't
			arr[0].RequiresPublicMethods (); // Should warn - analyzer doesn't
			arr[1].RequiresPublicMethods (); // Shouldn't warn

			TakesTypeByRef (ref arr[i]); // Reset - unknown index
			arr[1].RequiresPublicFields (); // Warns
		}

		static void TakesTypeByRef (ref Type type) { }

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void TestArrayResetAfterCall ()
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			// Calling a method and passing the array will reset the array after the call
			// This is necessary since the array is passed by referenced and its unknown
			// what the method will do to the array
			TakesTypesArray (arr);
			arr[0].RequiresPublicFields (); // Warns
		}

		static void TakesTypesArray (Type[] types) { }

		[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.None, "https://github.com/dotnet/linker/issues/2680")]
		static void TestArrayResetAfterAssignment ()
		{
			Type[] arr = new Type[] { typeof (TestType) };
			arr[0].RequiresPublicProperties ();

			// Assigning the array out of the method means that others can modify it - for non-method-calls it's not very likely to cause problems
			// because the only meaningful way this could work in the program is if some other thread accessed and modified the array
			// but it's still better to be safe in this case.
			_externalArray = arr;

			arr[0].RequiresPublicFields (); // Should warn
		}

		static void TestArrayRecursion ()
		{
			typeof (TestType).RequiresAll (); // Force data flow on this method

			object[] arr = new object[3];
			arr[0] = arr; // Recursive reference

			ConsumeArray (arr);

			static void ConsumeArray (object[] a) { }
		}

		static Type[] _externalArray;

		/// <summary>
		/// These tests are for tracking dataflow values through multi-dimensional arrays. We likely won't support this anytime soon.
		/// UnexpectedWarnings here are for the ideal behavior if tracking through multidimensional arrays was supported. They can be treated more as ExpectedWarnings.
		/// </summary>
		static class TestMultiDimensionalArray
		{
			public static void Test ()
			{
				TestArrayWithInitializerOneElementStaticType ();
				TestArrayWithInitializerOneElementParameter (typeof (TestType));
				TestArrayWithInitializerMultipleElementsStaticType ();
				TestArrayWithInitializerMultipleElementsMix<TestType> (typeof (TestType));

				TestArraySetElementOneElementStaticType ();
				TestArraySetElementOneElementParameter (typeof (TestType));
				TestArraySetElementMultipleElementsStaticType ();
				TestArraySetElementMultipleElementsMix<TestType> (typeof (TestType));

				TestArraySetElementAndInitializerMultipleElementsMix<TestType> (typeof (TestType));

				TestGetElementAtUnknownIndex ();

				// Array reset - certain operations on array are not tracked fully (or impossible due to unknown inputs)
				// and sometimes the only valid thing to do is to reset the array to all unknowns as it's impossible
				// to determine what the operation did to the array. So after the reset, everything in the array
				// should be treated as unknown value.
				TestArrayResetStoreUnknownIndex ();
				TestArrayResetGetElementOnByRefArray ();
				TestArrayResetAfterCall ();
				TestArrayResetAfterAssignment ();

				TestAddressOfElement ();
			}

			// Multidimensional Arrays not handled -- assumed to be UnknownValue
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayWithInitializerOneElementStaticType ()
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			// Multidimensional Arrays not handled -- assumed to be UnknownValue
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayWithInitializerOneElementParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{
				Type[,] arr = new Type[,] { { type } };
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			// Below are because we do not handle Multi dimensional arrays
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayWithInitializerMultipleElementsStaticType ()
			{
				Type[,] arr = new Type[,] { { typeof (TestType), typeof (TestType), typeof (TestType) } };
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresAll ();
				arr[0, 2].RequiresAll ();
				arr[0, 3].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			// Bug
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			// Below are because we do not handle Multi dimensional arrays
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayWithInitializerMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
			{
				Type[,] arr = new Type[,] { { typeof (TestType), typeof (TProperties), typeAll } };
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicProperties ();
				arr[0, 1].RequiresPublicFields (); // Should warn
				arr[0, 2].RequiresAll ();
				arr[0, 3].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			// Multidimensional Arrays not handled -- assumed to be UnknownValue
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArraySetElementOneElementStaticType ()
			{
				Type[,] arr = new Type[1, 1];
				arr[0, 0] = typeof (TestType);
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArraySetElementOneElementParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{
				Type[,] arr = new Type[1, 1];
				arr[0, 0] = type;
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArraySetElementMultipleElementsStaticType ()
			{
				Type[,] arr = new Type[1, 3];
				arr[0, 0] = typeof (TestType);
				arr[0, 1] = typeof (TestType);
				arr[0, 2] = typeof (TestType);
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresAll ();
				arr[0, 2].RequiresAll ();
				arr[0, 3].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			// Bug
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			// Below are because we do not handle Multi dimensional arrays
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArraySetElementMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
			{
				Type[,] arr = new Type[1, 3];
				arr[0, 0] = typeof (TestType);
				arr[0, 1] = typeof (TProperties);
				arr[0, 2] = typeAll;
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicProperties ();
				arr[0, 1].RequiresPublicFields (); // Should warn
				arr[0, 2].RequiresAll ();
				arr[0, 3].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			// Bug
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArraySetElementAndInitializerMultipleElementsMix<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TProperties> (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type typeAll)
			{
				Type[,] arr = new Type[,] { { typeof (TestType), null, null } };
				arr[0, 1] = typeof (TProperties);
				arr[0, 2] = typeAll;
				arr[0, 0].RequiresAll ();
				arr[0, 1].RequiresPublicProperties ();
				arr[0, 1].RequiresPublicFields (); // Should warn
				arr[0, 2].RequiresAll ();
				arr[0, 3].RequiresPublicMethods (); // Should warn - unknown value at this index
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestGetElementAtUnknownIndex (int i = 0)
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				arr[0, i].RequiresPublicFields ();
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayResetStoreUnknownIndex (int i = 0)
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				arr[0, 0].RequiresPublicProperties ();

				arr[0, i] = typeof (TestType); // Unknown index - we reset array to all unknowns

				arr[0, 0].RequiresPublicFields (); // Warns
			}

			// Analyzer doesn't reset array in this case
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2680")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayResetGetElementOnByRefArray (int i = 0)
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				arr[0, 0].RequiresPublicProperties ();

				TakesTypeByRef (ref arr[0, 0]); // No reset - known index
				arr[0, 0].RequiresPublicMethods (); // Doesn't warn

				TakesTypeByRef (ref arr[0, i]); // Reset - unknown index
				arr[0, 0].RequiresPublicFields (); // Warns
			}

			static void TakesTypeByRef (ref Type type) { }

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			// Multidimensional Arrays not handled -- assumed to be UnknownValue
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayResetAfterCall ()
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				arr[0, 0].RequiresPublicProperties ();

				// Calling a method and passing the array will reset the array after the call
				// This is necessary since the array is passed by referenced and its unknown
				// what the method will do to the array
				TakesTypesArray (arr);
				arr[0, 0].RequiresPublicFields (); // Warns
			}

			static void TakesTypesArray (Type[,] types) { }

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2680")]
			// Multidimensional Arrays not handled -- assumed to be UnknownValue
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicProperties), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestArrayResetAfterAssignment ()
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				arr[0, 0].RequiresPublicProperties ();

				// Assigning the array out of the method means that others can modify it - for non-method-calls it's not very likely to cause problems
				// because the only meaningful way this could work in the program is if some other thread accessed and modified the array
				// but it's still better to be safe in this case.
				_externalArray = arr;

				arr[0, 0].RequiresPublicFields (); // Should warn
			}

			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresPublicMethods), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/101951")]
			static void TestAddressOfElement ()
			{
				Type[,] arr = new Type[,] { { typeof (TestType) } };
				ref Type t = ref arr[0, 0];
				t.RequiresPublicMethods ();
			}

			static Type[,] _externalArray;
		}

		class WriteCapturedArrayElement
		{
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2737")]
			[UnexpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2737")]
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2737")]
			static void TestNullCoalesce ()
			{
				Type[] arr = new Type[1];
				arr[0] = GetUnknownType () ?? GetTypeWithPublicConstructors ();
				arr[0].RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll))]
			static void TestNullCoalescingAssignment ()
			{
				Type[] arr = new Type[1];
				arr[0] = GetTypeWithPublicConstructors ();
				arr[0] ??= GetUnknownType ();
				arr[0].RequiresAll ();
			}

			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2746")]
			[UnexpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/linker/issues/2746")]
			static void TestNullCoalescingAssignmentToEmpty ()
			{
				Type[] arr = new Type[1];
				arr[0] ??= GetUnknownType ();
				arr[0].RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll))]
			// (ILLink produces incomplete set of IL2072 warnings)
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2746")]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/linker/issues/2746")]
			static void TestNullCoalescingAssignmentComplex ()
			{
				Type[] arr = new Type[1];
				arr[0] = GetWithPublicMethods ();
				arr[0] ??= (GetUnknownType () ?? GetTypeWithPublicConstructors ());
				arr[0].RequiresAll ();
			}

			// ILLink only incidentally matches the analyzer behavior here.
			[UnexpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.TrimmerAnalyzerAndNativeAot, "https://github.com/dotnet/linker/issues/2737")]
			[ExpectedWarning("IL2072", nameof(GetUnknownType), nameof(DataFlowTypeExtensions.RequiresAll), Tool.None, "https://github.com/dotnet/linker/issues/2737")]
			[ExpectedWarning("IL2072", nameof(GetTypeWithPublicConstructors), nameof(DataFlowTypeExtensions.RequiresAll), Tool.None, "https://github.com/dotnet/linker/issues/2737")]
			static void TestNullCoalescingAssignmentToEmptyComplex ()
			{
				Type[] arr = new Type[1];
				arr[0] ??= (GetUnknownType () ?? GetTypeWithPublicConstructors ());
				arr[0].RequiresAll ();
			}

			public static void Test ()
			{
				TestNullCoalesce ();
				TestNullCoalescingAssignment ();
				TestNullCoalescingAssignmentToEmpty ();
				TestNullCoalescingAssignmentComplex ();
				TestNullCoalescingAssignmentToEmptyComplex ();
			}
		}

		class WriteElementOfCapturedArray
		{
			[Kept]
			[ExpectedWarning ("IL2072", nameof (GetUnknownType), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetTypeWithPublicConstructors), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), Tool.None, "https://github.com/dotnet/runtime/issues/90335")]
			// Analysis hole:
			// The array element assignment assigns to a temp array created as a copy of
			// arr1 or arr2, and writes to it aren't reflected back in arr1/arr2.
			static void TestArrayElementAssignment (bool b = true)
			{
				var arr1 = new Type[] { GetUnknownType () };
				var arr2 = new Type[] { GetTypeWithPublicConstructors () };
				(b ? arr1 : arr2)[0] = GetWithPublicMethods ();
				arr1[0].RequiresAll ();
				arr2[0].RequiresAll ();
			}

			[Kept]
			[KeptAttributeAttribute (typeof (InlineArrayAttribute))]
			[InlineArray (8)]
			public struct InlineTypeArray
			{
				[Kept]
				public Type t;
			}

			[Kept]
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll))]
			static void TestInlineArrayElementReferenceAssignment (bool b = true)
			{
				var arr1 = new InlineTypeArray ();
				arr1[0] = GetUnknownType ();
				var arr2 = new InlineTypeArray ();
				arr2[0] = GetTypeWithPublicConstructors ();
				(b ? ref arr1[0] : ref arr2[0]) = GetTypeWithPublicConstructors ();
				arr1[0].RequiresAll ();
				arr2[0].RequiresAll ();
			}

			// Inline array references are not allowed in conditionals, unlike array references.
			// static void TestInlineArrayElementAssignment (bool b = true)
			// {
			// 	var arr1 = new InlineTypeArray ();
			// 	arr1[0] = GetUnknownType ();
			// 	var arr2 = new InlineTypeArray ();
			// 	arr2[0] = GetTypeWithPublicConstructors ();
			// 	(b ? arr1 : arr2)[0] = GetWithPublicMethods ();
			// 	arr1[0].RequiresAll ();
			// 	arr2[0].RequiresAll ();
			// }

			[ExpectedWarning ("IL2087", nameof (T), nameof (DataFlowTypeExtensions.RequiresAll))]
			[ExpectedWarning ("IL2087", nameof (U), nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			[ExpectedWarning ("IL2087", nameof (V), nameof (DataFlowTypeExtensions.RequiresAll), Tool.None, "https://github.com/dotnet/linker/issues/2158")]
			[ExpectedWarning ("IL2087", nameof (V), nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.None, "https://github.com/dotnet/linker/issues/2158")]
			// Missing warnings for 'V' possibly assigned to arr or arr2 because write to temp
			// array isn't reflected back in the local variables.
			static void TestNullCoalesce<T, U, V> (bool b = false)
			{
				Type[]? arr = new Type[1] { typeof (T) };
				Type[] arr2 = new Type[1] { typeof (U) };

				(arr ?? arr2)[0] = typeof (V);
				arr[0].RequiresAll ();
				arr2[0].RequiresPublicFields ();
			}

			[ExpectedWarning ("IL2087", nameof (T), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/runtime/issues/93416")]
			[ExpectedWarning ("IL2087", nameof (U), nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			// Missing warnings for 'V' possibly assigned to arr or arr2 because write to temp
			// array isn't reflected back in the local variables. https://github.com/dotnet/linker/issues/2158
			[ExpectedWarning ("IL2087", nameof (V), nameof (DataFlowTypeExtensions.RequiresAll), Tool.None, "https://github.com/dotnet/linker/issues/2158")]
			[ExpectedWarning ("IL2087", nameof (V), nameof (DataFlowTypeExtensions.RequiresPublicFields), Tool.None, "https://github.com/dotnet/linker/issues/2158")]
			// This also causes an extra analyzer warning for 'U' in 'arr', because the analyzer models the
			// possible assignment of arr2 to arr, without overwriting index '0'. And it produces a warning
			// for each possible value, unlike ILLink/ILCompiler, which produce an unknown value for a merged
			// array value: https://github.com/dotnet/runtime/issues/93416
			[ExpectedWarning ("IL2087", nameof (U), nameof (DataFlowTypeExtensions.RequiresAll), Tool.Analyzer, "https://github.com/dotnet/runtime/issues/93416")]
			[ExpectedWarning ("IL2062", nameof (DataFlowTypeExtensions.RequiresAll), Tool.Trimmer | Tool.NativeAot, "https://github.com/dotnet/runtime/issues/93416")]
			static void TestNullCoalescingAssignment<T, U, V> (bool b = true)
			{
				Type[]? arr = new Type[1] { typeof (T) };
				Type[] arr2 = new Type[1] { typeof (U) };

				(arr ??= arr2)[0] = typeof (V);
				arr[0].RequiresAll ();
				arr2[0].RequiresPublicFields ();
			}

			public static void Test ()
			{
				TestArrayElementAssignment ();
				TestInlineArrayElementReferenceAssignment ();
				// TestInlineArrayElementAssignment ();
				TestNullCoalesce<int, int, int> ();
				TestNullCoalescingAssignment<int, int, int> ();
			}
		}

		class ConstantFieldValuesAsIndex
		{
			private const sbyte ConstSByte = 1;
			private const byte ConstByte = 1;
			private const short ConstShort = 1;
			private const ushort ConstUShort = 1;
			private const int ConstInt = 1;
			private const uint ConstUInt = 1;
			// Longs and ULongs would need support for conversion logic, which is not implement yet

			public static void Test ()
			{
				var types = new Type[2];
				types[0] = GetUnknownType ();
				types[1] = typeof (TestType);

				// All the consts are 1, so there should be no warnings
				types[ConstSByte].RequiresPublicMethods ();
				types[ConstByte].RequiresPublicMethods ();
				types[ConstShort].RequiresPublicMethods ();
				types[ConstUShort].RequiresPublicMethods ();
				types[ConstInt].RequiresPublicMethods ();
				types[ConstUInt].RequiresPublicMethods ();
			}
		}

		class HoistedArrayMutation
		{
			static void LoopAssignmentWithInitAfter ()
			{
				// This is a repro for https://github.com/dotnet/runtime/issues/86379
				// The array value is a hoisted local
				// It's first used in the main method (in the for loop)
				// this doesn't get a deep clone of the value, it takes the value from
				// the hoisted locals dictionary - and modifies it.
				// The local function Initialize then creates a deep copy and uses that.
				// Because of a bug, the changes done in the main body method are also
				// visible in the "old" interprocedural state. So the state never settles
				// and this causes an endless loop in the analyzer.
				int[] arr;

				Initialize ();
				for (int i = 0; i < arr.Length; i++) {
					arr[i] = 0;
				}

				void Initialize ()
				{
					arr = new int[10];
				}
			}

			public static void Test ()
			{
				LoopAssignmentWithInitAfter ();
			}
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)]
		private static Type GetTypeWithPublicConstructors ()
		{
			return null;
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		private static Type GetWithPublicMethods ()
		{
			return null;
		}

		private static Type GetUnknownType ()
		{
			return null;
		}

		public class TestType { }
	}
}
