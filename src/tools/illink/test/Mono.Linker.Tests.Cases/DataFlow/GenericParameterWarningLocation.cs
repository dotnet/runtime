// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// NativeAOT/analyzer differences in behavior compared to ILLink:
	//
	// Validation of generic parameters only matters if the instantiation can be used to run code with the substituted type.
	// So for generic methods the validation has to happen basically always (since any access to the method can lead to the code
	// of the method executing eventually).
	// For generic types though the situation is different. Code on the type can only run if the type is instantiated (new)
	// or if static members are accessed on it (method calls, or fields accesses both can lead to static .cctor execution).
	// Others usages of the type cannot themselves lead to code execution in the type, and thus don't need to be validated.
	// Currently linker validates every time there's a type occurrence in the code.
	// NativeAOT and the analyzer on the other hand only validate the cases which can lead to code execution (this is partially
	// because the compiler doesn't care about the type in other situations really).
	// So for example local variables of a given type, or method parameters of that type alone will not cause code execution
	// inside that type and thus won't be validated by NativeAOT compiler or the analyzer.
	//
	// Below this explanation/fact is referred to as "NativeAOT_StorageSpaceType"
	//   Storage space - declaring a storage space as having a specific type doesn't in itself do anything with that type as per
	//                   the above description.

	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class GenericParameterWarningLocation
	{
		public static void Main ()
		{
			TypeInheritance.Test ();
			TypeImplementingInterface.Test ();
			MethodParametersAndReturn.Test ();
			MethodParametersAndReturnAccessedViaReflection.Test ();
			FieldDefinition.Test ();
			FieldDefinitionViaReflection.Test ();
			PropertyDefinition.Test ();
			MethodBody.Test ();
			GenericAttributes.Test ();
			NestedGenerics.Test ();
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
			{
				[ExpectedWarning ("IL2091")] // Compiler generates an implicit call to BaseWithPublicMethods<TUnknown>..ctor. Also visible in analyzer CFG.
				public DerivedWithNoAnnotations () { }
			}

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

			[ExpectedWarning ("IL2091")]
			static void TestWithUnannotatedTypeArgument<TUnknown> ()
			{
				object a;
				a = new DerivedWithMatchingAnnotation<TUnknown> (); // IL2091 due to the instantiation
				a = new DerivedWithNoAnnotations<TUnknown> ();
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

				// Also try exact instantiations
				object a;
				a = new DerivedWithMatchingAnnotation<TestType> ();
				a = new DerivedWithMatchingAnnotation<string> ();

				// Also try with unannotated type parameter
				TestWithUnannotatedTypeArgument<TestType> ();

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

		//.NativeAOT: Method parameter types are not interesting until something creates an instance of them
		// so there's no need to validate generic arguments. See comment at the top of the file for more details.
		class MethodParametersAndReturn
		{
			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{ }

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{ }

			static void MethodWithSpecificType (TypeWithPublicMethods<TestType> one, IWithTwo<TestType, TestType> two) { }

			[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			static void MethodWithOneMismatch<TUnknown> (TypeWithPublicMethods<TUnknown> one) { }

			[ExpectedWarning ("IL2091", nameof (IWithTwo<TestType, TestType>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			[ExpectedWarning ("IL2091", nameof (TypeWithPublicMethods<TestType>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			static void MethodWithTwoMismatches<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				(IWithTwo<TPublicMethods, TPublicMethods> two, TypeWithPublicMethods<TPublicFields> one)
			{ }

			static TypeWithPublicMethods<TestType> MethodWithSpecificReturnType () => null;

			static TypeWithPublicMethods<TPublicMethods> MethodWithMatchingReturn<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> () => null;

			[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			static TypeWithPublicMethods<TUnknown> MethodWithOneMismatchReturn<TUnknown> () => null;

			[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			static IWithTwo<TPublicFields, TPublicMethods> MethodWithTwoMismatchesInReturn<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				() => null;

			class ConstructorWithOneMatchAndOneMismatch<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods>
			{
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				public ConstructorWithOneMatchAndOneMismatch (IWithTwo<TMethods, TMethods> two) { }
			}

			public static void Test ()
			{
				MethodWithSpecificType (null, null);
				MethodWithOneMismatch<TestType> (null);
				MethodWithTwoMismatches<TestType, TestType> (null, null);

				MethodWithSpecificReturnType ();
				MethodWithMatchingReturn<TestType> ();
				MethodWithOneMismatchReturn<TestType> ();
				MethodWithTwoMismatchesInReturn<TestType, TestType> ();

				_ = new ConstructorWithOneMatchAndOneMismatch<TestType> (null);
			}
		}

		// NativeAot warns for members accessed by reflection as a workaround for an incorrect suppression
		// in DI: https://github.com/dotnet/runtime/issues/81358
		// ILLink doesn't differentiate between reflection and non-reflection access, so it warns in both cases.
		// Analyzer doesn't implement the workaround, so it warns in neither case.
		class MethodParametersAndReturnAccessedViaReflection
		{
			class TypeWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{ }

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{ }

			static void MethodWithSpecificType (TypeWithPublicMethods<TestType> one, IWithTwo<TestType, TestType> two) { }

			[ExpectedWarning ("IL2091", ProducedBy = Tool.NativeAot | Tool.Trimmer)]
			static void MethodWithOneMismatch<TUnknown> (TypeWithPublicMethods<TUnknown> one) { }

			[ExpectedWarning ("IL2091", nameof (IWithTwo<TestType, TestType>), ProducedBy = Tool.NativeAot | Tool.Trimmer)]
			[ExpectedWarning ("IL2091", nameof (TypeWithPublicMethods<TestType>), ProducedBy = Tool.NativeAot | Tool.Trimmer)]
			static void MethodWithTwoMismatches<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				(IWithTwo<TPublicMethods, TPublicMethods> two, TypeWithPublicMethods<TPublicFields> one)
			{ }

			static TypeWithPublicMethods<TestType> MethodWithSpecificReturnType () => null;

			static TypeWithPublicMethods<TPublicMethods> MethodWithMatchingReturn<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> () => null;

			[ExpectedWarning ("IL2091", ProducedBy = Tool.NativeAot | Tool.Trimmer)]
			static TypeWithPublicMethods<TUnknown> MethodWithOneMismatchReturn<TUnknown> () => null;

			[ExpectedWarning ("IL2091", ProducedBy = Tool.NativeAot | Tool.Trimmer)]
			[ExpectedWarning ("IL2091", ProducedBy = Tool.NativeAot | Tool.Trimmer)]
			static IWithTwo<TPublicFields, TPublicMethods> MethodWithTwoMismatchesInReturn<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				() => null;

			class ConstructorWithOneMatchAndOneMismatch<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods>
			{
				[ExpectedWarning ("IL2091", ProducedBy = Tool.NativeAot | Tool.Trimmer)]
				public ConstructorWithOneMatchAndOneMismatch (IWithTwo<TMethods, TMethods> two) { }
			}

			public static void Test ()
			{
				// Access all of the methods via reflection
				typeof (MethodParametersAndReturnAccessedViaReflection).RequiresNonPublicMethods ();
				typeof (ConstructorWithOneMatchAndOneMismatch<>).RequiresPublicConstructors ();
			}
		}

		//.NativeAOT: Field types are not interesting until something creates an instance of them
		// so there's no need to validate generic arguments. See comment at the top of the file for more details.
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
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static TypeWithPublicMethods<TUnknown> _field1;
				static TypeWithPublicMethods<TPublicMethods> _field2;
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

		// NativeAot warns for members accessed by reflection as a workaround for an incorrect suppression
		// in DI: https://github.com/dotnet/runtime/issues/81358
		// ILLink doesn't differentiate between reflection and non-reflection access, so it warns in both cases.
		// Analyzer doesn't implement the workaround, so it warns in neither case.
		class FieldDefinitionViaReflection
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
			}

			class OneMatchingAnnotation<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
			{
				static TypeWithPublicMethods<TPublicMethods> _field;
			}

			class MultipleReferencesToTheSameType<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods, TUnknown>
			{
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				static TypeWithPublicMethods<TUnknown> _field1;
				static TypeWithPublicMethods<TPublicMethods> _field2;
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				static TypeWithPublicMethods<TUnknown> _field3;
			}

			class TwoMismatchesInOne<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				static IWithTwo<TPublicFields, TPublicMethods> _field;
			}

			public static void Test ()
			{
				typeof (SpecificType).RequiresNonPublicFields ();
				typeof (OneMatchingAnnotation<>).RequiresNonPublicFields ();
				typeof (MultipleReferencesToTheSameType<,>).RequiresNonPublicFields ();
				typeof (TwoMismatchesInOne<,>).RequiresNonPublicFields ();
			}
		}

		//.NativeAOT: Property types are not interesting until something creates an instance of them
		// so there's no need to validate generic arguments. See comment at the top of the file for more details.
		// In case of trimmer/aot it's even less important because properties don't exist in IL really
		// and thus no code can manipulate them directly - only through reflection.
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
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static TypeWithPublicMethods<TUnknown> Property1 {
					[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
					get;

					[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
					set;
				}

				static TypeWithPublicMethods<TPublicMethods> Property2 {
					get;
					set;
				}

				// The warning is generated on the backing field
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static TypeWithPublicMethods<TUnknown> Property3 {
					[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
					get;

					[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", CompilerGeneratedCode = true, ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static IWithTwo<TPublicFields, TPublicMethods> Property {
					// Getter is trimmed and doesn't produce any warning
					get;

					[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
					[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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
				public void InstanceMethod () { }

				public static string Field;
				public string InstanceField;

				public static string Property { get; set; }
			}

			interface IWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
			{
				public static void Method () { }
				public void InstanceMethod ();

				public static string Field;

				public static string Property { get; set; }
			}

			class TypeWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields> : Exception
			{ }

			static void MethodWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> () { }

			void InstanceMethodWithPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> () { }

			static void MethodWithTwo<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields> ()
			{ }

			static MethodBody GetInstance () => null;

			[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // return type // NativeAOT_StorageSpaceType
			static TypeWithPublicMethods<T> GetInstanceForTypeWithPublicMethods<T> () => null;

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

				// Analyzer doesn't warn on typeof
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				static void MultipleReferencesToTheSameType<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Type t = typeof (TypeWithPublicMethods<TUnknown>); // Warn
					t = typeof (TypeWithPublicMethods<TPublicMethods>); // No warn
					t = typeof (TypeWithPublicMethods<TUnknown>); // Warn
				}

				// Analyzer doesn't warn on typeof
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

				[ExpectedWarning ("IL2091")]
				static void InstanceMethodMismatch<TUnknown> ()
				{
					GetInstance ().InstanceMethodWithPublicMethods<TUnknown> ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
					InstanceMethodMismatch<TestType> ();
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // local variable // NativeAOT_StorageSpaceType
				static void InstanceMethodMismatch<TUnknown> ()
				{
					TypeWithPublicMethods<TUnknown> instance = GetInstanceForTypeWithPublicMethods<TUnknown> ();
					instance.InstanceMethod ();
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameMethod<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
					InstanceMethodMismatch<TestType> ();
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // access to the field // NativeAOT_StorageSpaceType
				static void InstanceFieldMismatch<TUnknown> ()
				{
					TypeWithPublicMethods<TUnknown> instance = GetInstanceForTypeWithPublicMethods<TUnknown> ();
					_ = instance.InstanceField;
				}

				public static void Test ()
				{
					SpecificType ();
					OneMatchingAnnotation<TestType> ();
					MultipleReferencesToTheSameField<TestType, TestType> ();
					TwoMismatchesInOneStatement<TestType, TestType> ();
					InstanceFieldMismatch<TestType> ();
				}
			}

			//.NativeAOT: Local variable types are not interesting until something creates an instance of them
			// so there's no need to validate generic arguments. See comment at the top of the file for more details.
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static void MultipleReferencesToTheSameType<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					TypeWithPublicMethods<TUnknown> t1 = null; // Warn
					TypeWithPublicMethods<TPublicMethods> t2 = null; // No warn
					TypeWithPublicMethods<TUnknown> t3 = null; // Warn
				}

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken field
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				static void MultipleReferencesToTheSameField<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TUnknown>.Field; // Warn
					a = () => TypeWithPublicMethods<TPublicMethods>.Field; // No warn
					a = () => TypeWithPublicMethods<TUnknown>.Field; // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken field
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken method (getter)
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				static void MultipleReferencesToTheSameProperty<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					Expression<Func<string>> a = () => TypeWithPublicMethods<TUnknown>.Property; // Warn
					a = () => TypeWithPublicMethods<TPublicMethods>.Property; // No warn
					a = () => TypeWithPublicMethods<TUnknown>.Property; // Warn
				}

				[ExpectedWarning ("IL2091")]
				[ExpectedWarning ("IL2091")]
				// There are two warnings per "callsite" in this case because the generated IL does
				//    ldtoken method (getter)
				//    ldtoken owningtype
				// In order to call the right Expression APIs.
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer | Tool.NativeAot)]
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

			//.NativeAOT: Checking an instance for its type is not interesting until something creates an instance of that type
			// so there's no need to validate generic arguments. See comment at the top of the file for more details.
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					bool a1 = _value is TypeWithPublicMethods<TUnknown>; // Warn
					bool a2 = _value is TypeWithPublicMethods<TPublicMethods>; // No warn
					bool a3 = _value is TypeWithPublicMethods<TUnknown>; // Warn
				}

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

			// This is basically the same operation as IsInstance
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				static void MultipleReferencesToTheSameMethod<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
					TUnknown> ()
				{
					object a1 = _value as TypeWithPublicMethods<TUnknown>; // Warn
					object a2 = _value as TypeWithPublicMethods<TPublicMethods>; // No warn
					object a3 = _value as TypeWithPublicMethods<TUnknown>; // Warn
				}

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

			//.NativeAOT: Exception types are effectively very similar to local variable or method parameters.
			// and are not interesting until something creates an instance of them
			// so there's no need to validate generic arguments. See comment at the top of the file for more details.
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

			// This is basically the same as IsInstance and thus not dangerous
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				[ExpectedWarning ("IL2091", ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
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

			class TypeWithPrivateMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T>
			{
			}

			class TypeWithRUCMethod
			{
				[RequiresUnreferencedCode ("TypeWithRUCMethod.PrivateRUCMethod")]
				private static void PrivateRUCMethod () { }
			}

			class AnnotatedString
			{
				static void MethodWithAnnotatedParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] string typeName) { }

				// Analyzer: https://github.com/dotnet/runtime/issues/95118
				// NativeAOT: https://github.com/dotnet/runtime/issues/95140
				[ExpectedWarning ("IL2026", "TypeWithRUCMethod.PrivateRUCMethod", ProducedBy = Tool.Trimmer)]
				static void AnnotatedParameter ()
				{
					MethodWithAnnotatedParameter ("Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithPrivateMethods`1[[Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithRUCMethod]]");
				}

				// Analyzer: https://github.com/dotnet/runtime/issues/95118
				// NativeAOT: https://github.com/dotnet/runtime/issues/95140
				[ExpectedWarning ("IL2026", "TypeWithRUCMethod.PrivateRUCMethod", ProducedBy = Tool.Trimmer)]
				[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				static string AnnotatedReturnValue ()
				{
					return "Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithPrivateMethods`1[[Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithRUCMethod]]";
				}

				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
				static string _annotatedField;

				// Analyzer: https://github.com/dotnet/runtime/issues/95118
				// NativeAOT: https://github.com/dotnet/runtime/issues/95140
				[ExpectedWarning ("IL2026", "TypeWithRUCMethod.PrivateRUCMethod", ProducedBy = Tool.Trimmer)]
				static void AnnotatedField ()
				{
					_annotatedField = "Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithPrivateMethods`1[[Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithRUCMethod]]";
				}

				public static void Test ()
				{
					AnnotatedParameter ();
					AnnotatedReturnValue ();
					AnnotatedField ();
				}
			}

			class TypeGetType
			{
				// Analyzer: https://github.com/dotnet/runtime/issues/95118
				// NativeAOT: https://github.com/dotnet/runtime/issues/95140
				[ExpectedWarning ("IL2026", "TypeWithRUCMethod.PrivateRUCMethod", ProducedBy = Tool.Trimmer)]
				static void SpecificType ()
				{
					Type.GetType ("Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithPrivateMethods`1[[Mono.Linker.Tests.Cases.DataFlow.GenericParameterWarningLocation+MethodBody+TypeWithRUCMethod]]");
				}

				public static void Test ()
				{

					SpecificType ();
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
				AnnotatedString.Test ();
				TypeGetType.Test ();
			}
		}

		// There are no warnings due to data flow itself
		// since the generic attributes must be fully instantiated always.
		class GenericAttributes
		{
			class TypeWithPublicMethodsAttribute<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods>
				: Attribute
			{ }

			class TypeWithTwoAttribute<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TPublicFields>
				: Attribute
			{ }

			[TypeWithPublicMethods<TestType>]
			static void OneSpecificType () { }

			[TypeWithTwo<TestType, TestType>]
			static void TwoSpecificTypes () { }

			public static void Test ()
			{
				OneSpecificType ();
				TwoSpecificTypes ();
			}
		}

		class NestedGenerics
		{
			class RequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
			{
			}

			class RequiresNothing<T>
			{
			}

			class RequiresFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			{
			}

			static void GenericMethodNoRequirements<T> () { }

			static void GenericMethodNoRequirementsAccessToT<T> ()
			{
				_ = typeof (T).Name;
			}

			class GenericTypeWithMethodAndField<T>
			{
				public void GenericInstanceMethod<T> () { }
				public static void GenericMethod<T> () { }

				public static int Field;
			}

			class TypeWithRUCMethod
			{
				[RequiresUnreferencedCode ("--RUCMethod--")]
				public static void RUCMethod () { }
			}

			[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
			[ExpectedWarning ("IL2026", "--RUCMethod--")]
			static void GenericMethodNesting<TUnknown> ()
			{
				GenericMethodNoRequirements<RequiresMethods<RequiresNothing<TUnknown>>> (); // No warning
				GenericMethodNoRequirements<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>> (); // IL2091
				GenericMethodNoRequirements<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>> (); // IL2026
			}

			[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
			[ExpectedWarning ("IL2026", "--RUCMethod--")]
			static void GenericMethodNestingAccessToT<TUnknown> ()
			{
				GenericMethodNoRequirementsAccessToT<RequiresMethods<RequiresNothing<TUnknown>>> (); // No warning
				GenericMethodNoRequirementsAccessToT<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>> (); // IL2091
				GenericMethodNoRequirementsAccessToT<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>> (); // IL2026
			}

			[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
			[ExpectedWarning ("IL2026", "--RUCMethod--")]
			static void GenericInstanceMethodNesting<TUnknown> ()
			{
				GenericTypeWithMethodAndField<TestType> instance = new ();
				instance.GenericInstanceMethod<RequiresMethods<RequiresNothing<TUnknown>>> (); // No warning
				instance.GenericInstanceMethod<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>> (); // IL2091
				instance.GenericInstanceMethod<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>> (); // IL2026
			}

			[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields")]
			[ExpectedWarning ("IL2091", "TUnknown", "RequiresMethods")]
			[ExpectedWarning ("IL2026", "--RUCMethod--")]
			[ExpectedWarning ("IL2026", "--RUCMethod--")]
			static void MethodOnGenericTypeNesting<TUnknown> ()
			{
				GenericTypeWithMethodAndField<RequiresMethods<RequiresNothing<TUnknown>>>.GenericMethod<string> (); // No warning
				GenericTypeWithMethodAndField<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>>          // IL2091
					.GenericMethod<RequiresNothing<RequiresFields<RequiresFields<RequiresMethods<TUnknown>>>>> (); // IL2091
				GenericTypeWithMethodAndField<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>> // IL2026
					.GenericMethod<RequiresNothing<RequiresFields<RequiresMethods<TypeWithRUCMethod>>>> (); // IL2026
			}

			[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
			[ExpectedWarning ("IL2026", "--RUCMethod--")]
			static void FieldOnGenericTypeNesting<TUnknown> ()
			{
				GenericTypeWithMethodAndField<RequiresMethods<RequiresNothing<TUnknown>>>.Field = 0; // No warning
				_ = GenericTypeWithMethodAndField<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>>.Field; // IL2091
				_ = GenericTypeWithMethodAndField<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>>.Field; // IL2026
			}

			class BaseTypeGenericNesting
			{
				class Base<T>
				{
					static Base () { _ = typeof (T).Name; }
				}

				class DerivedWithNothing<TUnknown>
					: Base<RequiresMethods<RequiresNothing<TUnknown>>>
				{ }

				[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
				class DerivedWithFields<TUnknown>
					: Base<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>>
				{
					static DerivedWithFields ()
					{
					}
				}

				[ExpectedWarning ("IL2026", "--RUCMethod--")]
				class DerivedWithRUC
					: Base<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>>
				{ }

				public static void Test ()
				{
					Type a;
					a = typeof (DerivedWithNothing<TestType>);
					a = typeof (DerivedWithFields<TestType>);
					a = typeof (DerivedWithRUC);
				}
			}

			class InterfaceGenericNesting
			{
				interface IBase<T>
				{
				}

				class DerivedWithNothing<TUnknown>
					: IBase<RequiresMethods<RequiresNothing<TUnknown>>>
				{ }

				[ExpectedWarning ("IL2091", "TUnknown", "RequiresFields", nameof (DynamicallyAccessedMemberTypes.PublicFields))]
				class DerivedWithFields<TUnknown>
					: IBase<RequiresMethods<RequiresNothing<RequiresFields<TUnknown>>>>
				{
					static DerivedWithFields ()
					{
					}
				}

				[ExpectedWarning ("IL2026", "--RUCMethod--")]
				class DerivedWithRUC
					: IBase<RequiresMethods<RequiresNothing<RequiresMethods<TypeWithRUCMethod>>>>
				{ }

				public static void Test ()
				{
					// We have to instantiate the types otherwise trimmer will remove interfaces
					// since they're not needed.
					object a = new DerivedWithNothing<TestType> ();
					a = new DerivedWithFields<TestType> ();
					a = new DerivedWithRUC ();

					// We also have to reference the interface type to "keep" it
					var t = typeof (IBase<TestType>);
				}
			}

			public static void Test ()
			{
				GenericMethodNesting<TestType> ();
				GenericMethodNestingAccessToT<TestType> ();
				GenericInstanceMethodNesting<TestType> ();
				MethodOnGenericTypeNesting<TestType> ();
				FieldOnGenericTypeNesting<TestType> ();
				BaseTypeGenericNesting.Test ();
				InterfaceGenericNesting.Test ();
			}
		}

		class TestType { }

		static void DoNothing () { }
	}
}
