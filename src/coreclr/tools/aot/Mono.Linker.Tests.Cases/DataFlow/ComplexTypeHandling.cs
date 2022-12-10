// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
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
			TestArrayInAttributeParameter_ViaReflection ();
		}

		// NativeAOT: No need to preserve the element type if it's never instantiated
		// There will be a reflection record about it, but we don't validate that yet
		[Kept (By = ProducedBy.Trimmer)]
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

		// NativeAOT: No need to preserve the element type if it's never instantiated
		// There will be a reflection record about it, but we don't validate that yet
		[Kept (By = ProducedBy.Trimmer)]
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

		// NativeAOT: No need to preserve the element type if it's never instantiated
		// There will be a reflection record about it, but we don't validate that yet
		[Kept (By = ProducedBy.Trimmer)]
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

		// NativeAOT: No need to preserve the element type if it's never instantiated
		// There will be a reflection record about it, but we don't validate that yet
		[Kept (By = ProducedBy.Trimmer)]
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


		// https://github.com/dotnet/runtime/issues/72833
		// NativeAOT doesn't implement full type name parser yet - it ignores the [] and thus sees this as a direct type reference
		[Kept]
		sealed class ArrayTypeGetTypeElement
		{
			// https://github.com/dotnet/runtime/issues/72833
			// NativeAOT doesn't implement full type name parser yet - it ignores the [] and thus sees this as a direct type reference
			[Kept (By = ProducedBy.NativeAot)]
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		static void TestArrayTypeGetType ()
		{
			RequirePublicMethods (Type.GetType ("Mono.Linker.Tests.Cases.DataFlow.ComplexTypeHandling+ArrayTypeGetTypeElement[]"));
		}

		// ILLink: Technically there's no reason to mark this type since it's only used as an array element type and CreateInstance
		// doesn't work on arrays, but the currently implementation will preserve it anyway due to how it processes
		// string -> Type resolution. This will only impact code which would have failed at runtime, so very unlikely to
		// actually occur in real apps (and even if it does happen, it just increases size, doesn't break behavior).
		// NativeAOT: https://github.com/dotnet/runtime/issues/72833
		// NativeAOT doesn't implement full type name parser yet - it ignores the [] and thus sees this as a direct type reference
		[Kept (By = ProducedBy.Trimmer)]
		class ArrayCreateInstanceByNameElement
		{
			public ArrayCreateInstanceByNameElement ()
			{
			}
		}

		[Kept]
		static void TestArrayCreateInstanceByName ()
		{
			Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.DataFlow.ComplexTypeHandling+ArrayCreateInstanceByNameElement[]");
		}

		// NativeAOT doesn't keep attributes on non-reflectable methods
		[Kept (By = ProducedBy.Trimmer)]
		class ArrayInAttributeParamElement
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		// NativeAOT doesn't keep attributes on non-reflectable methods
		[KeptAttributeAttribute (typeof (RequiresPublicMethodAttribute), By = ProducedBy.Trimmer)]
		[RequiresPublicMethod (typeof (ArrayInAttributeParamElement[]))]
		static void TestArrayInAttributeParameter ()
		{
		}

		// The usage of a type in attribute parameter is not enough to create NativeAOT EEType
		// which is what the test infra looks for right now, so the type is not kept.
		// There should be a reflection record of the type though (we just don't validate that yet).
		[Kept (By = ProducedBy.Trimmer)]
		class ArrayInAttributeParamElement_ViaReflection
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresPublicMethodAttribute))]
		[RequiresPublicMethod (typeof (ArrayInAttributeParamElement_ViaReflection[]))]
		static void TestArrayInAttributeParameter_ViaReflection_Inner ()
		{
		}

		[Kept]
		static void TestArrayInAttributeParameter_ViaReflection ()
		{
			typeof (ComplexTypeHandling)
				.GetMethod (nameof (TestArrayInAttributeParameter_ViaReflection_Inner), BindingFlags.NonPublic)
				.Invoke (null, new object[] { });
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
