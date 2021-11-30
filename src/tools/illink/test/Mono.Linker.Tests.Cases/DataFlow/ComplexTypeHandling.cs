// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
		// Analyzer doesn't support intrinsics: https://github.com/dotnet/linker/issues/2374
		[ExpectedWarning ("IL2072", "'type'", nameof (ComplexTypeHandling) + "." + nameof (RequirePublicMethods) + "(Type)", "System.Object.GetType()",
			ProducedBy = ProducedBy.Analyzer)]
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
		// Analyzer doesn't support intrinsics: https://github.com/dotnet/linker/issues/2374
		[ExpectedWarning ("IL2072", "'type'", nameof (ComplexTypeHandling) + "." + nameof (RequirePublicMethods) + "(Type)", "System.Object.GetType()",
			ProducedBy = ProducedBy.Analyzer)]
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
		// Analyzer doesn't support intrinsics: https://github.com/dotnet/linker/issues/2374
		[ExpectedWarning ("IL2026", "System.Type.GetType(String)",
			ProducedBy = ProducedBy.Analyzer)]
		// Analyzer doesn't track known types: https://github.com/dotnet/linker/issues/2273
		[ExpectedWarning ("IL2072", "'type'", nameof (ComplexTypeHandling) + "." + nameof (RequirePublicMethods) + "(Type)", "System.Type.GetType(String)",
			ProducedBy = ProducedBy.Analyzer)]
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
		// Analyzer doesn't support intrinsics: https://github.com/dotnet/linker/issues/2374
		[ExpectedWarning ("IL2026", "Activator.CreateInstance(String, String)",
			ProducedBy = ProducedBy.Analyzer)]
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
