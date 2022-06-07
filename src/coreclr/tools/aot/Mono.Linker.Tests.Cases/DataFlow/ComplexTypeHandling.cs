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

		[Kept]
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

		[Kept]
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

		// Nothing should be marked as CreateInstance doesn't work on arrays
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

		[Kept]
		class ArrayInAttributeParamElement
		{
			// This method should not be marked, instead Array.* should be marked
			public void PublicMethod () { }
		}

		[Kept]
		[KeptAttributeAttribute (typeof (RequiresPublicMethodAttribute))]
		[RequiresPublicMethod (typeof (ArrayInAttributeParamElement[]))]
		static void TestArrayInAttributeParameter ()
		{
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
