// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Applying DAM PublicMethods on an array will mark Array.CreateInstance which has RDC on it")]
	[KeptAttributeAttribute(typeof(UnconditionalSuppressMessageAttribute))]
	public class ComplexTypeHandling
	{
		public static void Main ()
		{
			TestArray ();
			TestArrayOnGeneric ();
			TestGenericArray ();
			TestGenericArrayOnGeneric ();
			TestArrayGetTypeFromMethodParam ();
			TestArrayGetTypeFromField ();
			TestArrayTypeGetType ();
			TestArrayCreateInstanceByName ();
			TestArrayInAttributeParameter ();
		}

		[Kept]
		class ArrayElementType
		{
			public ArrayElementType () { }

			public void PublicMethod () { }

			private int _privateField;
		}

		[Kept]
		static void TestArray ()
		{
			RequirePublicMethods (typeof (ArrayElementType[]));
		}

		[Kept]
		static void TestGenericArray ()
		{
			RequirePublicMethodsOnArrayOfGeneric<ArrayElementType> ();
		}

		[Kept]
		static void RequirePublicMethodsOnArrayOfGeneric<T> ()
		{
			RequirePublicMethods (typeof (T[]));
		}

		[Kept]
		class ArrayElementInGenericType
		{
			public ArrayElementInGenericType () { }

			public void PublicMethod () { }

			private int _privateField;
		}

		[Kept]
		[KeptMember (".ctor()")]
		class RequirePublicMethodsGeneric<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		[KeptAttributeAttribute (typeof (DynamicallyAccessedMembersAttribute))]
		T>
		{
		}

		[Kept]
		static void TestArrayOnGeneric ()
		{
			_ = new RequirePublicMethodsGeneric<ArrayElementInGenericType[]> ();
		}

		[Kept]
		static void TestGenericArrayOnGeneric ()
		{
			RequirePublicMethodsOnArrayOfGenericParameter<ArrayElementInGenericType> ();
		}

		[Kept]
		static void RequirePublicMethodsOnArrayOfGenericParameter<T> ()
		{
			_ = new RequirePublicMethodsGeneric<T[]> ();
		}

		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve array element types just due to the usage of the array type
		sealed class ArrayGetTypeFromMethodParamElement
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		static void TestArrayGetTypeFromMethodParamHelper (ArrayGetTypeFromMethodParamElement[] p)
		{
			RequirePublicMethods (p.GetType ());
		}

		[Kept]
		static void TestArrayGetTypeFromMethodParam ()
		{
			TestArrayGetTypeFromMethodParamHelper (null);
		}

		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve array element types just due to the usage of the array type
		sealed class ArrayGetTypeFromFieldElement
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		static ArrayGetTypeFromFieldElement[] _arrayGetTypeFromField;

		[Kept]
		static void TestArrayGetTypeFromField ()
		{
			RequirePublicMethods (_arrayGetTypeFromField.GetType ());
		}

		[Kept]
		sealed class ArrayTypeGetTypeElement
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		static void TestArrayTypeGetType ()
		{
			RequirePublicMethods (Type.GetType ("Mono.Linker.Tests.Cases.DataFlow.ComplexTypeHandling+ArrayTypeGetTypeElement[]"));
		}

		// Trimmer: Technically there's no reason to mark this type since it's only used as an array element type and CreateInstance
		// doesn't work on arrays, but the current implementation will preserve it anyway due to how it processes
		// string -> Type resolution. This will only impact code which would have failed at runtime, so very unlikely to
		// actually occur in real apps (and even if it does happen, it just increases size, doesn't break behavior).
		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve array element types just due to the usage of the array type
		class ArrayCreateInstanceByNameElement
		{
			public ArrayCreateInstanceByNameElement ()
			{
			}
		}

		[Kept]
		[ExpectedWarning ("IL2032", ProducedBy = Tool.NativeAot)] // https://github.com/dotnet/runtime/issues/82447
		static void TestArrayCreateInstanceByName ()
		{
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.DataFlow.ComplexTypeHandling+ArrayCreateInstanceByNameElement[]");
		}

		[Kept (By = Tool.Trimmer)] // NativeAOT doesn't preserve array element types just due to the usage of the array type
		class ArrayInAttributeParamElement
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresPublicMethodAttribute))]
		[RequiresPublicMethod (typeof (ArrayInAttributeParamElement[]))]
		static void TestArrayInAttributeParameterImpl ()
		{
		}

		[Kept]
		static void TestArrayInAttributeParameter()
		{
			// Have to access the method through reflection, otherwise NativeAOT will remove all attributes on it
			// since they're not accessible.
			typeof (ComplexTypeHandling).GetMethod (nameof (TestArrayInAttributeParameterImpl), System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).Invoke (null, new object[] { });
		}

		[Kept]
		private static void RequirePublicMethods (
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
			Type type)
		{
		}

		[Kept]
		[KeptBaseType (typeof (Attribute))]
		class RequiresPublicMethodAttribute : Attribute
		{
			[Kept]
			public RequiresPublicMethodAttribute (
				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				[KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
				Type t)
			{
			}
		}
	}
}
