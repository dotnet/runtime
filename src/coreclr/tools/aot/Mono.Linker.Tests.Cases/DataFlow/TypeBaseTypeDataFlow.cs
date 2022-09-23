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
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SandboxDependency ("Dependencies/TestSystemTypeBase.cs")]
	[ExpectedNoWarnings]
	public class TypeBaseTypeDataFlow
	{
		public static void Main ()
		{
			TestAllPropagated (typeof (TestType));
			AllPropagatedWithDerivedClass.Test ();

			TestPublicConstructorsAreNotPropagated (typeof (TestType));
			TestPublicEventsPropagated (typeof (TestType));
			TestPublicFieldsPropagated (typeof (TestType));
			TestPublicMethodsPropagated (typeof (TestType));
			TestPublicNestedTypesAreNotPropagated (typeof (TestType));
			TestPublicParameterlessConstructorIsNotPropagated (typeof (TestType));
			TestPublicPropertiesPropagated (typeof (TestType));

			TestNonPublicConstructorsAreNotPropagated (typeof (TestType));
			TestNonPublicEventsAreNotPropagated (typeof (TestType));
			TestNonPublicFieldsAreNotPropagated (typeof (TestType));
			TestNonPublicMethodsAreNotPropagated (typeof (TestType));
			TestNonPublicNestedTypesAreNotPropagated (typeof (TestType));
			TestNonPublicPropertiesAreNotPropagated (typeof (TestType));

			TestInterfacesPropagated (typeof (TestType));

			TestCombinationOfPublicsIsPropagated (typeof (TestType));
			TestCombinationOfNonPublicsIsNotPropagated (typeof (TestType));
			TestCombinationOfPublicAndNonPublicsPropagatesPublicOnly (typeof (TestType));

			TestNoAnnotation (typeof (TestType));
			TestAnnotatedAndUnannotated (typeof (TestType), typeof (TestType), 0);
			TestNull ();
			TestNoValue ();

			Mixed_Derived.Test (typeof (TestType), 0);

			LoopPatterns.Test ();
		}

		static void TestAllPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type derivedType)
		{
			derivedType.BaseType.RequiresAll ();
		}

		class AllPropagatedWithDerivedClass
		{
			// https://github.com/dotnet/linker/issues/2673
			[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll) + "(Type)", nameof (TestSystemTypeBase.BaseType) + ".get",
				ProducedBy = ProducedBy.Analyzer)]
			static void TestAllPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] TestSystemTypeBase derivedType)
			{
				derivedType.BaseType.RequiresAll ();
			}

			// This is a very special case - normally there's basically no way to "new up" a Type instance via the "new" operator.
			// The linker and analyzer see an unknown value and thus warns that it doesn't fulfill the All annotation.
			[ExpectedWarning ("IL2062", nameof (TestAllPropagated))]
			public static void Test ()
			{
				TestAllPropagated (new TestSystemTypeBase ());
			}
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicConstructors))]
		static void TestPublicConstructorsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicConstructors ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicEvents))]
		static void TestPublicEventsPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicEvents ();

			// Should warn
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicEvents ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicFields))]
		static void TestPublicFieldsPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicFields ();

			// Should warn
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicFields ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicProperties))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicMethods))]
		static void TestPublicMethodsPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresPublicProperties ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicMethods ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicNestedTypes))]
		static void TestPublicNestedTypesAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicNestedTypes ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor))]
		static void TestPublicParameterlessConstructorIsNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicParameterlessConstructor ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicProperties))]
		static void TestPublicPropertiesPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicProperties ();

			// Should warn
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicProperties ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors))]
		static void TestNonPublicConstructorsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicConstructors ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicEvents))]
		static void TestNonPublicEventsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicEvents)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicEvents ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicFields))]
		static void TestNonPublicFieldsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicFields ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicMethods))]
		static void TestNonPublicMethodsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicMethods ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicNestedTypes))]
		static void TestNonPublicNestedTypesAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicNestedTypes ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicProperties))]
		static void TestNonPublicPropertiesAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicProperties ();
		}

		static void TestInterfacesPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] Type derivedType)
		{
			derivedType.BaseType.RequiresInterfaces ();
		}

		static void TestCombinationOfPublicsIsPropagated (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicMethods ();
			derivedType.BaseType.RequiresPublicProperties ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicMethods))]
		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicProperties))]
		static void TestCombinationOfNonPublicsIsNotPropagated (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicMethods ();
			derivedType.BaseType.RequiresNonPublicProperties ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresNonPublicMethods))]
		static void TestCombinationOfPublicAndNonPublicsPropagatesPublicOnly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicMethods ();
			derivedType.BaseType.RequiresPublicProperties ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestNoAnnotation (Type derivedType)
		{
			derivedType.BaseType.RequiresPublicMethods ();
		}

		[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions) + "." + nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void TestAnnotatedAndUnannotated (
			Type derivedTypeOne,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type derivedTypeTwo,
			int number)
		{
			Type type = number > 0 ? derivedTypeOne : derivedTypeTwo;
			type.BaseType.RequiresPublicMethods ();
		}

		static void TestNull ()
		{
			Type type = null;
			type.BaseType.RequiresPublicMethods ();
		}

		static void TestNoValue ()
		{
			Type t = null;
			Type noValue = Type.GetTypeFromHandle (t.TypeHandle);
			// No warning because the above throws an exception at runtime.
			noValue.BaseType.RequiresPublicMethods ();
		}

		class Mixed_Base
		{
			public static void PublicMethod () { }
			private static void PrivateMethod () { }
		}

		class Mixed_Derived : Mixed_Base
		{
			public static void Test (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type typeWithPublicMethods,
				int number)
			{
				Type type;
				switch (number) {
				case 0:
					type = typeof (TestType);
					break;
				case 1:
					type = typeof (Mixed_Derived);
					break;
				case 2:
					type = typeWithPublicMethods;
					break;
				default:
					type = null;
					break;
				}

				type.BaseType.RequiresPublicMethods ();
			}
		}

		class LoopPatterns
		{
			static void EnumerateInterfacesOnBaseTypes ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] Type type)
			{
				Type? t = type;
				while (t != null) {
					Type[] interfaces = t.GetInterfaces ();
					t = t.BaseType;
				}
			}

			[ExpectedWarning ("IL2070")]
			[ExpectedWarning ("IL2075", ProducedBy = ProducedBy.Analyzer)] // Linker doesn't implement backward branches data flow yet
			static void EnumerateInterfacesOnBaseTypes_Unannotated (Type type)
			{
				Type? t = type;
				while (t != null) {
					Type[] interfaces = t.GetInterfaces ();
					t = t.BaseType;
				}
			}

			// Can only work with All annotation as NonPublicProperties doesn't propagate to base types
			static void EnumeratePrivatePropertiesOnBaseTypes ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{
				const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
				Type? t = type;
				while (t != null) {
					t.GetProperties (DeclaredOnlyLookup).GetEnumerator ();
					t = t.BaseType;
				}
			}

			// Can only work with All annotation as NonPublicProperties doesn't propagate to base types
			static void EnumeratePrivatePropertiesOnBaseTypesWithForeach ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type type)
			{
				const BindingFlags DeclaredOnlyLookup = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
				Type? t = type;
				while (t != null) {
					foreach (var p in t.GetProperties (DeclaredOnlyLookup)) {
						// Do nothing
					}
					t = t.BaseType;
				}
			}

			public static void Test ()
			{
				EnumerateInterfacesOnBaseTypes (typeof (TestType));
				EnumerateInterfacesOnBaseTypes_Unannotated (typeof (TestType));

				EnumeratePrivatePropertiesOnBaseTypes (typeof (TestType));
				EnumeratePrivatePropertiesOnBaseTypesWithForeach (typeof (TestType));
			}
		}

		class TestType
		{
		}
	}
}
