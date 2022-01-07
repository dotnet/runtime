// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	public class TypeBaseTypeDataFlow
	{
		public static void Main ()
		{
			TestAllPropagated (typeof (TestType));

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

			Mixed_Derived.Test (typeof (TestType), 0);
		}

		[RecognizedReflectionAccessPattern]
		static void TestAllPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type derivedType)
		{
			derivedType.BaseType.RequiresAll ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicConstructors), new Type[] { typeof (Type) })]
		static void TestPublicConstructorsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicConstructors)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicConstructors ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicEvents), new Type[] { typeof (Type) })]
		static void TestPublicEventsPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicEvents)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicEvents ();

			// Should warn
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicEvents ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicFields), new Type[] { typeof (Type) })]
		static void TestPublicFieldsPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicFields ();

			// Should warn
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicFields ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicProperties), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicMethods), new Type[] { typeof (Type) })]
		static void TestPublicMethodsPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresPublicProperties ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicMethods ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicNestedTypes), new Type[] { typeof (Type) })]
		static void TestPublicNestedTypesAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicNestedTypes)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicNestedTypes ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicParameterlessConstructor), new Type[] { typeof (Type) })]
		static void TestPublicParameterlessConstructorIsNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicParameterlessConstructor ();
		}

		[RecognizedReflectionAccessPattern]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicProperties), new Type[] { typeof (Type) })]
		static void TestPublicPropertiesPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicProperties ();

			// Should warn
			derivedType.BaseType.RequiresPublicMethods ();

			// Should warn
			derivedType.BaseType.RequiresNonPublicProperties ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicConstructors), new Type[] { typeof (Type) })]
		static void TestNonPublicConstructorsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicConstructors ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicEvents), new Type[] { typeof (Type) })]
		static void TestNonPublicEventsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicEvents)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicEvents ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicFields), new Type[] { typeof (Type) })]
		static void TestNonPublicFieldsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicFields ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicMethods), new Type[] { typeof (Type) })]
		static void TestNonPublicMethodsAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicNestedTypes), new Type[] { typeof (Type) })]
		static void TestNonPublicNestedTypesAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicNestedTypes ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicProperties), new Type[] { typeof (Type) })]
		static void TestNonPublicPropertiesAreNotPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicProperties ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestInterfacesPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] Type derivedType)
		{
			derivedType.BaseType.RequiresInterfaces ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestCombinationOfPublicsIsPropagated (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresPublicMethods ();
			derivedType.BaseType.RequiresPublicProperties ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicMethods), new Type[] { typeof (Type) })]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicProperties), new Type[] { typeof (Type) })]
		static void TestCombinationOfNonPublicsIsNotPropagated (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicMethods ();
			derivedType.BaseType.RequiresNonPublicProperties ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresNonPublicMethods), new Type[] { typeof (Type) })]
		static void TestCombinationOfPublicAndNonPublicsPropagatesPublicOnly (
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicProperties)] Type derivedType)
		{
			derivedType.BaseType.RequiresNonPublicMethods ();
			derivedType.BaseType.RequiresPublicProperties ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void TestNoAnnotation (Type derivedType)
		{
			derivedType.BaseType.RequiresPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void TestAnnotatedAndUnannotated (
			Type derivedTypeOne,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type derivedTypeTwo,
			int number)
		{
			Type type = number > 0 ? derivedTypeOne : derivedTypeTwo;
			type.BaseType.RequiresPublicMethods ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) })]
		static void TestNull ()
		{
			Type type = null;
			type.BaseType.RequiresPublicMethods ();
		}

		class Mixed_Base
		{
			public static void PublicMethod () { }
			private static void PrivateMethod () { }
		}

		class Mixed_Derived : Mixed_Base
		{
			[RecognizedReflectionAccessPattern]
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

		class TestType
		{
		}
	}
}
