// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Security.Policy;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class GenericParameterWarningLocation
	{
		public static void Main ()
		{
			TypeInheritance.Test ();
			TypeImplementingInterface.Test ();
			MethodParametersAndReturn.Test ();
			FieldDefinition.Test ();
			PropertyDefinition.Test ();
			MethodBody.Test ();
		}

		class TypeInheritance
		{
			class BaseWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{
				public static void GetMethods ()
				{
					typeof (TPublicMethods).GetMethods ();
				}
			}

			class BaseWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{ }

			// No warning - annotations applied
			class DerivedWithSpecificType : BaseWithPublicMethods<TestType> { }

			// No warning - annotations match
			class DerivedWithMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] TAllMethods>
				: BaseWithPublicMethods<TAllMethods>
			{ }

			[ExpectedWarning ("IL2091")]
			class DerivedWithNoAnnotations<TUnknown>
				: BaseWithPublicMethods<TUnknown>
			{ }

			[ExpectedWarning ("IL2091")]
			class DerivedWithMismatchAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
				: BaseWithPublicMethods<TPublicFields>
			{ }

			[ExpectedWarning ("IL2091", nameof (DynamicallyAccessedMemberTypes.PublicMethods))]
			class DerivedWithOneMismatch<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
				: BaseWithTwo<TPublicFields, TPublicFields>
			{ }

			class DerivedWithTwoMatching<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				: BaseWithTwo<TPublicMethods, TPublicFields>
			{ }

			[ExpectedWarning ("IL2091")]
			class DerivedWithOnlyStaticMethodReference<TUnknown> : BaseWithPublicMethods<TUnknown>
			{
				// The method body in this case looks like:
				//     BaseWithPublicMethods<TUnknown>.GetMethods ()
				// The type instantiation needs to be validated and in this case it produces a warning.
				// This is no different from the same code being part of a completely unrelated method/class.
				// So the fact that this is in derived class has no impact on the validation in this case.
				[ExpectedWarning ("IL2091")]
				public static void GetDerivedMethods () => GetMethods ();
			}

			public static void Test ()
			{
				Type t;
				t = typeof (DerivedWithSpecificType);
				t = typeof (DerivedWithMatchingAnnotation<>);
				t = typeof (DerivedWithNoAnnotations<>);
				t = typeof (DerivedWithMismatchAnnotation<>);
				t = typeof (DerivedWithOneMismatch<>);
				t = typeof (DerivedWithTwoMatching<,>);

				DerivedWithOnlyStaticMethodReference<TestType>.GetDerivedMethods ();
			}
		}

		class TypeImplementingInterface
		{
			interface IWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> { }

			interface IWithPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields> { }

			// No warning - annotations applied
			class ImplementsWithSpecificType : IWithPublicMethods<TestType>, IWithPublicFields<TestType> { }

			// No warning - matching annotations
			class ImplementsWithMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] TAll>
				: IWithPublicMethods<TAll>, IWithPublicFields<TAll>
			{ }

			[ExpectedWarning ("IL2091", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
			class ImplementsWithOneMismatch<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				: IWithPublicMethods<TPublicMethods>, IWithPublicFields<TPublicMethods>
			{ }

			[ExpectedWarning ("IL2091", nameof (DynamicallyAccessedMemberTypes.PublicMethods))]
			[ExpectedWarning ("IL2091", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
			class ImplementsWithTwoMismatches<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
				: IWithPublicMethods<TPublicFields>, IWithPublicFields<TPublicMethods>
			{ }

			public static void Test ()
			{
				// Instantiate the types
				new ImplementsWithSpecificType ();
				new ImplementsWithMatchingAnnotation<TestType> ();
				new ImplementsWithOneMismatch<TestType> ();
				new ImplementsWithTwoMismatches<TestType, TestType> ();

				// Also reference the interfaces, otherwise they could be trimmed
				Type t;
				t = typeof (IWithPublicMethods<>);
				t = typeof (IWithPublicFields<>);
			}
		}

		// Method parameter instantiation doesn't technically need to be validated on its own
		// Especially in AOT compiler the type of the method parameter won't even exist in the compilation
		// unless something passes a real value to it, in which case that value creation will do all the necessary
		// validations. So the declaration of the parameter type alone doesn't create an analysis hole
		// it only becomes one if something tries to create an instance of such type.
		// Similarly for statics - since we do validate accesses to static members, the static .ctor is covered through
		// those as well.
		class MethodParametersAndReturn
		{
			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{ }

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{ }

			static void MethodWithSpecificType (TypeWithPublicMethods<TestType> one, IWithTwo<TestType, TestType> two) { }

			[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
			static void MethodWithOneMismatch<TUnknown> (TypeWithPublicMethods<TUnknown> one) { }

			[ExpectedWarning ("IL2091", nameof (IWithTwo<TestType, TestType>), ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2091", nameof (TypeWithPublicMethods<TestType>), ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
			static void MethodWithTwoMismatches<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				(IWithTwo<TPublicMethods, TPublicMethods> two, TypeWithPublicMethods<TPublicFields> one)
			{ }

			static TypeWithPublicMethods<TestType> MethodWithSpecificReturnType () => null;

			static TypeWithPublicMethods<TPublicMethods> MethodWithMatchingReturn<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> () => null;

			[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
			static TypeWithPublicMethods<TUnknown> MethodWithOneMismatchReturn<TUnknown> () => null;

			[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
			static IWithTwo<TPublicFields, TPublicMethods> MethodWithTwoMismatchesInReturn<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				() => null;

			public static void Test ()
			{
				MethodWithSpecificType (null, null);
				MethodWithOneMismatch<TestType> (null);
				MethodWithTwoMismatches<TestType, TestType> (null, null);

				MethodWithSpecificReturnType ();
				MethodWithMatchingReturn<TestType> ();
				MethodWithOneMismatchReturn<TestType> ();
				MethodWithTwoMismatchesInReturn<TestType, TestType> ();
			}
		}

		// Same as for method parameters - declaring a field with a specific type in itself is not problematic
		// for the field to be "dangerous" it would need to have a non-null value and thus the instance has to be created somewhere
		// and that point of creation is the place where the validation happens.
		class FieldDefinition
		{
			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{ }

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{ }

			class SpecificType
			{
				static TypeWithPublicMethods<TestType> _field;

				public static void Test ()
				{
					_field = null;
				}
			}

			class OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{
				static TypeWithPublicMethods<TPublicMethods> _field;

				public static void Test ()
				{
					_field = null;
				}
			}

			class MultipleReferencesToTheSameType<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods, TUnknown>
			{
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				static TypeWithPublicMethods<TUnknown> _field1;
				static TypeWithPublicMethods<TPublicMethods> _field2;
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				static TypeWithPublicMethods<TUnknown> _field3;

				public static void Test ()
				{
					_field1 = null;
					_field2 = null;
					_field3 = null;
				}
			}

			class TwoMismatchesInOne<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				static IWithTwo<TPublicFields, TPublicMethods> _field;

				public static void Test ()
				{
					_field = null;
				}
			}

			public static void Test ()
			{
				SpecificType.Test ();
				OneMatchingAnnotation<TestType>.Test ();
				MultipleReferencesToTheSameType<TestType, TestType>.Test ();
				TwoMismatchesInOne<TestType, TestType>.Test ();
			}
		}

		class PropertyDefinition
		{
			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{ }

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{ }

			class SpecificType
			{
				static TypeWithPublicMethods<TestType> Property { get; set; }

				public static void Test ()
				{
					Property = null;
				}
			}

			class OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{
				static TypeWithPublicMethods<TPublicMethods> Property { get; set; }

				public static void Test ()
				{
					Property = null;
				}
			}

			class MultipleReferencesToTheSameType<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods, TUnknown>
			{
				// The warning is generated on the backing field
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				static TypeWithPublicMethods<TUnknown> Property1 {
					[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
					get;

					[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
					set;
				}

				static TypeWithPublicMethods<TPublicMethods> Property2 {
					get;
					set;
				}

				// The warning is generated on the backing field
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				static TypeWithPublicMethods<TUnknown> Property3 {
					[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
					get;

					[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
					set;
				}

				public static void Test ()
				{
					Property1 = Property1;
					Property2 = Property2;
					Property3 = Property3;
				}
			}

			class TwoMismatchesInOne<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{
				// The warnings are generated on the backing field
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
				static IWithTwo<TPublicFields, TPublicMethods> Property {
					// Getter is trimmed and doesn't produce any warning
					get;

					[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
					[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.Analyzer)]
					set;
				}

				public static void Test ()
				{
					Property = null;
				}
			}

			public static void Test ()
			{
				SpecificType.Test ();
				OneMatchingAnnotation<TestType>.Test ();
				MultipleReferencesToTheSameType<TestType, TestType>.Test ();
				TwoMismatchesInOne<TestType, TestType>.Test ();
			}
		}

		class MethodBody
		{
			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> : Exception
			{
				public static void Method () { }

				public static string Field;

				public static string Property { get; set; }
			}

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{
				public static void Method () { }

				public static string Field;

				public static string Property { get; set; }
			}

			class TypeWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields> : Exception
			{ }

			static void MethodWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> () { }

			static void MethodWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields> ()
			{ }

			class TypeOf
			{
				static void AccessOpenTypeDefinition ()
				{
					// Accessing the open type definition should not do anything on its own - just validating that it doesn't break anything
					Type t = typeof (TypeWithPublicMethods<>);
					t = typeof (IWithTwo<,>);
				}

				static void SpecificType ()
				{
					Type t = typeof (TypeWithPublicMethods<TestType>);
					t = typeof (IWithTwo<TestType, TestType>);
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					Type t = typeof (TypeWithPublicMethods<TPublicMethods>);
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameType<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Type t = typeof (TypeWithPublicMethods<TUnknown>); // Warn
					t = typeof (TypeWithPublicMethods<TPublicMethods>); // No warn
					t = typeof (TypeWithPublicMethods<TUnknown>); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					Type t = typeof (IWithTwo<TPublicFields, TPublicMethods>);
				}

				public static void Test ()
				{
					AccessOpenTypeDefinition ();
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameType<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class MethodCallOnGenericMethod
			{
				static void SpecificType ()
				{
					MethodWithPublicMethods<TestType> ();
					MethodWithTwo<TestType, TestType> ();
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					MethodWithPublicMethods<TPublicMethods> ();
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					MethodWithPublicMethods<TUnknown> (); // Warn
					MethodWithPublicMethods<TPublicMethods> (); // No warn
					MethodWithPublicMethods<TUnknown> (); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					MethodWithTwo<TPublicFields, TPublicMethods> ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class MethodCallOnGenericType
			{
				static void SpecificType ()
				{
					TypeWithPublicMethods<TestType>.Method ();
					IWithTwo<TestType, TestType>.Method ();
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					TypeWithPublicMethods<TPublicMethods>.Method ();
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					TypeWithPublicMethods<TUnknown>.Method (); // Warn
					TypeWithPublicMethods<TPublicMethods>.Method (); // No warn
					TypeWithPublicMethods<TUnknown>.Method (); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					IWithTwo<TPublicFields, TPublicMethods>.Method ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class FieldAccessOnGenericType
			{
				static void SpecificType ()
				{
					_ = TypeWithPublicMethods<TestType>.Field;
					IWithTwo<TestType, TestType>.Field = "";
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					_ = TypeWithPublicMethods<TPublicMethods>.Field;
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameField<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					_ = TypeWithPublicMethods<TUnknown>.Field; // Warn
					TypeWithPublicMethods<TPublicMethods>.Field = ""; // No warn
					TypeWithPublicMethods<TUnknown>.Field = ""; // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					_ = IWithTwo<TPublicFields, TPublicMethods>.Field;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameField<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class LocalVariable
			{
				static void SpecificType ()
				{
					TypeWithPublicMethods<TestType> t = null;
					IWithTwo<TestType, TestType> i = null;
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					TypeWithPublicMethods<TPublicMethods> t = null;
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameType<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					TypeWithPublicMethods<TUnknown> t1 = null; // Warn
					TypeWithPublicMethods<TPublicMethods> t2 = null; // No warn
					TypeWithPublicMethods<TUnknown> t3 = null; // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					IWithTwo<TPublicFields, TPublicMethods> i = null;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameType<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class DelegateUsageOnGenericMethod
			{
				static void SpecificType ()
				{
					var a = new Action (MethodWithPublicMethods<TestType>);
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					var a = new Action (MethodWithPublicMethods<TPublicMethods>);
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					var a1 = new Action (MethodWithPublicMethods<TUnknown>); // Warn
					var a2 = new Action (MethodWithPublicMethods<TPublicMethods>); // No warn
					var a3 = new Action (MethodWithPublicMethods<TUnknown>); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					var a = new Action (MethodWithTwo<TPublicFields, TPublicMethods>);
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class DelegateUsageOnGenericType
			{
				static void SpecificType ()
				{
					var a = new Action (TypeWithPublicMethods<TestType>.Method);
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					var a = new Action (TypeWithPublicMethods<TPublicMethods>.Method);
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					var a1 = new Action (TypeWithPublicMethods<TUnknown>.Method); // Warn
					var a2 = new Action (TypeWithPublicMethods<TPublicMethods>.Method); // No warn
					var a3 = new Action (TypeWithPublicMethods<TUnknown>.Method); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					var a = new Action (IWithTwo<TPublicFields, TPublicMethods>.Method);
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class LdTokenOnGenericMethod
			{
				static void SpecificType ()
				{
					Expression<Action> a = () => MethodWithPublicMethods<TestType> ();
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					Expression<Action> a = () => MethodWithPublicMethods<TPublicMethods> ();
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Expression<Action> a = () => MethodWithPublicMethods<TUnknown> (); // Warn
					a = () => MethodWithPublicMethods<TPublicMethods> (); // No warn
					a = () => MethodWithPublicMethods<TUnknown> (); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					Expression<Action> a = () => MethodWithTwo<TPublicFields, TPublicMethods> ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class LdTokenOfMethodOnGenericType
			{
				static void SpecificType ()
				{
					Expression<Action> a = () => TypeWithPublicMethods<TestType>.Method ();
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					Expression<Action> a = () => TypeWithPublicMethods<TPublicMethods>.Method ();
				}

				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken method
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Expression<Action> a = () => TypeWithPublicMethods<TUnknown>.Method (); // Warn
					a = () => TypeWithPublicMethods<TPublicMethods>.Method (); // No warn
					a = () => TypeWithPublicMethods<TUnknown>.Method (); // Warn
				}

				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken method
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					Expression<Action> a = () => IWithTwo<TPublicFields, TPublicMethods>.Method ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class LdTokenOfFieldOnGenericType
			{
				static void SpecificType ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TestType>.Field;
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TPublicMethods>.Field;
				}

				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken field
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				static void MultipleReferencesToTheSameField<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TUnknown>.Field; // Warn
					a = () => TypeWithPublicMethods<TPublicMethods>.Field; // No warn
					a = () => TypeWithPublicMethods<TUnknown>.Field; // Warn
				}

				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken field
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					Expression<Func<string>> a = () => IWithTwo<TPublicFields, TPublicMethods>.Field;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameField<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class LdTokenOfPropertyOnGenericType
			{
				static void SpecificType ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TestType>.Property;
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TPublicMethods>.Property;
				}

				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken method (getter)
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				static void MultipleReferencesToTheSameProperty<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TUnknown>.Property; // Warn
					a = () => TypeWithPublicMethods<TPublicMethods>.Property; // No warn
					a = () => TypeWithPublicMethods<TUnknown>.Property; // Warn
				}

				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken method (getter)
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer | ProducedBy.NativeAot)]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					Expression<Func<string>> a = () => IWithTwo<TPublicFields, TPublicMethods>.Property;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameProperty<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class CreateInstance
			{
				static void SpecificType ()
				{
					object a = new TypeWithPublicMethods<TestType> ();
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					object a = new TypeWithPublicMethods<TPublicMethods> ();
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					object a1 = new TypeWithPublicMethods<TUnknown> (); // Warn
					object a2 = new TypeWithPublicMethods<TPublicMethods> (); // No warn
					object a3 = new TypeWithPublicMethods<TUnknown> (); // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					object a = new TypeWithTwo<TPublicFields, TPublicMethods> ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class IsInstance
			{
				static object _value = null;

				static void SpecificType ()
				{
					bool a = _value is TypeWithPublicMethods<TestType>;
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					bool a = _value is TypeWithPublicMethods<TPublicMethods>;
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					bool a1 = _value is TypeWithPublicMethods<TUnknown>; // Warn
					bool a2 = _value is TypeWithPublicMethods<TPublicMethods>; // No warn
					bool a3 = _value is TypeWithPublicMethods<TUnknown>; // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					bool a = _value is TypeWithTwo<TPublicFields, TPublicMethods>;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class AsType
			{
				static object _value = null;

				static void SpecificType ()
				{
					object a = _value as TypeWithPublicMethods<TestType>;
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					object a = _value as TypeWithPublicMethods<TPublicMethods>;
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					object a1 = _value as TypeWithPublicMethods<TUnknown>; // Warn
					object a2 = _value as TypeWithPublicMethods<TPublicMethods>; // No warn
					object a3 = _value as TypeWithPublicMethods<TUnknown>; // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					object a = _value as TypeWithTwo<TPublicFields, TPublicMethods>;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class ExceptionCatch
			{
				static void SpecificType ()
				{
					try {
						DoNothing ();
					} catch (TypeWithPublicMethods<TestType>) {
					}
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					try {
						DoNothing ();
					} catch (TypeWithPublicMethods<TPublicMethods>) {
					}
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameType<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					try {
						DoNothing ();
					} catch (TypeWithPublicMethods<TUnknown>) { // Warn
					}

					try {
						DoNothing ();
					} catch (TypeWithPublicMethods<TPublicMethods>) { // No warn
					}

					try {
						DoNothing ();
					} catch (TypeWithPublicMethods<TUnknown>) { // Warn
					}
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					try {
						DoNothing ();
					} catch (TypeWithTwo<TPublicFields, TPublicMethods>) { // Warn x2
					}
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameType<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			class ExceptionFilter
			{
				static void SpecificType ()
				{
					try {
						DoNothing ();
					} catch (Exception ex) when (ex is TypeWithPublicMethods<TestType>) {
					}
				}

				static void OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
				{
					try {
						DoNothing ();
					} catch (Exception ex) when (ex is TypeWithPublicMethods<TPublicMethods>) {
					}
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void MultipleReferencesToTheSameType<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					try {
						DoNothing ();
					} catch (Exception ex) when (ex is TypeWithPublicMethods<TUnknown>) { // Warn
					}

					try {
						DoNothing ();
					} catch (Exception ex) when (ex is TypeWithPublicMethods<TPublicMethods>) { // No warn
					}

					try {
						DoNothing ();
					} catch (Exception ex) when (ex is TypeWithPublicMethods<TUnknown>) { // Warn
					}
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				static void TwoMismatchesInOneStatement<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
					()
				{
					try {
						DoNothing ();
					} catch (Exception ex) when (ex is TypeWithTwo<TPublicFields, TPublicMethods>) { // Warn x2
					}
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameType<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
				}
			}

			public static void Test ()
			{
				TypeOf.Test ();
				MethodCallOnGenericMethod.Test ();
				MethodCallOnGenericType.Test ();
				FieldAccessOnGenericType.Test ();
				LocalVariable.Test ();
				DelegateUsageOnGenericMethod.Test ();
				DelegateUsageOnGenericType.Test ();
				LdTokenOnGenericMethod.Test ();
				LdTokenOfMethodOnGenericType.Test ();
				LdTokenOfFieldOnGenericType.Test ();
				LdTokenOfPropertyOnGenericType.Test ();
				CreateInstance.Test ();
				IsInstance.Test ();
				AsType.Test ();
				ExceptionCatch.Test ();
				ExceptionFilter.Test ();
			}
		}

		class TestType { }

		static void DoNothing () { }
	}
}
