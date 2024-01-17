// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// NativeAOT/analyzer differences in behavior compared to ILLink:
	//
	// See the description on top of GenericParameterWarningLocation for the expected differences in behavior
	// for NativeAOT and the analyzer.
	// The tests affected by this are marked with "NativeAOT_StorageSpaceType"
	//

	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class GenericParameterDataFlow
	{
		public static void Main ()
		{
			TestSingleGenericParameterOnType ();
			TestMultipleGenericParametersOnType ();
			TestBaseTypeGenericRequirements ();
			TestDeepNestedTypesWithGenerics ();
			TestInterfaceTypeGenericRequirements ();
			TestTypeGenericRequirementsOnMembers ();
			TestPartialInstantiationTypes ();

			TestSingleGenericParameterOnMethod ();
			TestMultipleGenericParametersOnMethod ();
			TestMethodGenericParametersViaInheritance ();

			TestNewConstraintSatisfiesParameterlessConstructor<object> ();
			TestStructConstraintSatisfiesParameterlessConstructor<TestStruct> ();
			TestUnmanagedConstraintSatisfiesParameterlessConstructor<byte> ();

			TestGenericParameterFlowsToField ();
			TestGenericParameterFlowsToReturnValue ();

			TestGenericParameterFlowsToDelegateMethod<TestType> ();
			TestGenericParameterFlowsToDelegateMethodDeclaringType<TestType> ();
			TestGenericParameterFlowsToDelegateMethodDeclaringTypeInstance<TestType> ();

			TestNoWarningsInRUCMethod<TestType> ();
			TestNoWarningsInRUCType<TestType, TestType> ();
		}

		static void TestSingleGenericParameterOnType ()
		{
			TypeRequiresNothing<TestType>.Test ();
			TypeRequiresPublicFields<TestType>.Test ();
			TypeRequiresPublicMethods<TestType>.Test ();
			TypeRequiresPublicFieldsPassThrough<TestType>.Test ();
			TypeRequiresNothingPassThrough<TestType>.Test ();
		}

		static void TestGenericParameterFlowsToReturnValue ()
		{
			_ = TypeRequiresPublicFields<TestType>.ReturnRequiresPublicFields ();
			_ = TypeRequiresPublicFields<TestType>.ReturnRequiresPublicMethods ();
			_ = TypeRequiresPublicFields<TestType>.ReturnRequiresNothing ();
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type FieldRequiresPublicFields;

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type FieldRequiresPublicMethods;

		static Type FieldRequiresNothing;


		class TypeRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			[ExpectedWarning ("IL2087", "'" + nameof (T) + "'", nameof (TypeRequiresPublicFields<T>), nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}

			[RequiresUnreferencedCode ("message")]
			public static void RUCTest ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2089", "'" + nameof (T) + "'", nameof (TypeRequiresPublicFields<T>), nameof (FieldRequiresPublicMethods))]
			public static void TestFields ()
			{
				FieldRequiresPublicFields = typeof (T);
				FieldRequiresPublicMethods = typeof (T);
				FieldRequiresNothing = typeof (T);
			}

			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public static Type ReturnRequiresPublicFields ()
			{
				return typeof (T);
			}

			[ExpectedWarning ("IL2088", "'" + nameof (T) + "'", nameof (TypeRequiresPublicFields<T>), nameof (ReturnRequiresPublicMethods))]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
			public static Type ReturnRequiresPublicMethods ()
			{
				return typeof (T);
			}
			public static Type ReturnRequiresNothing ()
			{
				return typeof (T);
			}
		}

		class TypeRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T>
		{
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}
		}

		class TypeRequiresNothing<T>
		{
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}
		}

		class TypeRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TSource>
		{
			[ExpectedWarning ("IL2091", nameof (TSource),
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeRequiresPublicFieldsPassThrough<TSource>",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeRequiresPublicMethods<T>")]
			public static void Test ()
			{
				TypeRequiresPublicFields<TSource>.Test ();
				TypeRequiresPublicMethods<TSource>.Test ();
				TypeRequiresNothing<TSource>.Test ();
			}
		}

		class TypeRequiresNothingPassThrough<T>
		{
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicFields<T>))]
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<T>))]
			public static void Test ()
			{
				TypeRequiresPublicFields<T>.Test ();
				TypeRequiresPublicMethods<T>.Test ();
				TypeRequiresNothing<T>.Test ();
			}
		}

		static void TestBaseTypeGenericRequirements ()
		{
			new DerivedTypeWithInstantiatedGenericOnBase ();
			new DerivedTypeWithInstantiationOverSelfOnBase ();
			new DerivedTypeWithOpenGenericOnBase<TestType> ();
			TestDerivedTypeWithOpenGenericOnBaseWithRUCOnBase ();
			TestDerivedTypeWithOpenGenericOnBaseWithRUCOnDerived ();
			new DerivedTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		/// <summary>
		/// Adding a comment to verify that analyzer doesn't null ref when trying to analyze
		/// generic parameter in cref comments on a class
		/// <see cref="GenericBaseTypeWithRequirements{T}"/>
		/// </summary>
		class GenericBaseTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public GenericBaseTypeWithRequirements ()
			{
				typeof (T).RequiresPublicFields ();
			}
		}

		class DerivedTypeWithInstantiatedGenericOnBase : GenericBaseTypeWithRequirements<TestType>
		{
		}

		class DerivedTypeWithInstantiationOverSelfOnBase : GenericBaseTypeWithRequirements<DerivedTypeWithInstantiationOverSelfOnBase>
		{
		}

		[ExpectedWarning ("IL2091", nameof (GenericBaseTypeWithRequirements<T>))]
		class DerivedTypeWithOpenGenericOnBase<T> : GenericBaseTypeWithRequirements<T>
		{
			// Analyzer does not see the base class constructor
			[ExpectedWarning ("IL2091", nameof (GenericBaseTypeWithRequirements<T>))]
			public DerivedTypeWithOpenGenericOnBase () { }
		}

		static void TestDerivedTypeWithOpenGenericOnBaseWithRUCOnBase ()
		{
			new DerivedTypeWithOpenGenericOnBaseWithRUCOnBase<TestType> ();
		}

		[ExpectedWarning ("IL2109", nameof (BaseTypeWithOpenGenericDAMTAndRUC<T>))]
		[ExpectedWarning ("IL2091", nameof (BaseTypeWithOpenGenericDAMTAndRUC<T>))]
		[ExpectedWarning ("IL2091", nameof (IGenericInterfaceTypeWithRequirements<T>))]
		class DerivedTypeWithOpenGenericOnBaseWithRUCOnBase<T> : BaseTypeWithOpenGenericDAMTAndRUC<T>, IGenericInterfaceTypeWithRequirements<T>
		{
			[ExpectedWarning ("IL2091", nameof (DerivedTypeWithOpenGenericOnBaseWithRUCOnBase<T>))]
			[ExpectedWarning ("IL2026", nameof (BaseTypeWithOpenGenericDAMTAndRUC<T>))]
			public DerivedTypeWithOpenGenericOnBaseWithRUCOnBase () { }
		}

		[RequiresUnreferencedCode ("RUC")]
		class BaseTypeWithOpenGenericDAMTAndRUC<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> { }


		[ExpectedWarning ("IL2026", nameof (DerivedTypeWithOpenGenericOnBaseWithRUCOnDerived<TestType>))]
		static void TestDerivedTypeWithOpenGenericOnBaseWithRUCOnDerived ()
		{
			new DerivedTypeWithOpenGenericOnBaseWithRUCOnDerived<TestType> ();
		}
		[ExpectedWarning ("IL2091", nameof (BaseTypeWithOpenGenericDAMT<T>))]
		[ExpectedWarning ("IL2091", nameof (IGenericInterfaceTypeWithRequirements<T>))]
		[RequiresUnreferencedCode ("RUC")]
		class DerivedTypeWithOpenGenericOnBaseWithRUCOnDerived<T> : BaseTypeWithOpenGenericDAMT<T>, IGenericInterfaceTypeWithRequirements<T>
		{
		}

		class BaseTypeWithOpenGenericDAMT<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> { }


		class DerivedTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: GenericBaseTypeWithRequirements<T>
		{
		}

		static void TestMultipleGenericParametersOnType ()
		{
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestMultiple ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestFields ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestMethods ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestBoth ();
			MultipleTypesWithDifferentRequirements<TestType, TestType, TestType, TestType>.TestNothing ();
		}

		class MultipleTypesWithDifferentRequirements<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing>
		{
			public static void TestMultiple ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
				typeof (TBoth).RequiresPublicFields ();
				typeof (TBoth).RequiresPublicMethods ();
				typeof (TFields).RequiresNone ();
				typeof (TMethods).RequiresNone ();
				typeof (TBoth).RequiresNone ();
				typeof (TNothing).RequiresNone ();
			}

			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void TestFields ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TFields).RequiresPublicMethods ();
				typeof (TFields).RequiresNone ();
			}

			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			public static void TestMethods ()
			{
				typeof (TMethods).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
				typeof (TMethods).RequiresNone ();
			}

			public static void TestBoth ()
			{
				typeof (TBoth).RequiresPublicFields ();
				typeof (TBoth).RequiresPublicMethods ();
				typeof (TBoth).RequiresNone ();
			}

			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
			[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
			public static void TestNothing ()
			{
				typeof (TNothing).RequiresPublicFields ();
				typeof (TNothing).RequiresPublicMethods ();
				typeof (TNothing).RequiresNone ();
			}
		}

		static void TestDeepNestedTypesWithGenerics ()
		{
			RootTypeWithRequirements<TestType>.InnerTypeWithNoAddedGenerics.TestAccess ();
		}

		class RootTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TRoot>
		{
			public class InnerTypeWithNoAddedGenerics
			{
				[ExpectedWarning ("IL2087", nameof (TRoot),
						"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.RootTypeWithRequirements<TRoot>",
						"type",
						"DataFlowTypeExtensions.RequiresPublicMethods(Type)")]
				public static void TestAccess ()
				{
					typeof (TRoot).RequiresPublicFields ();
					typeof (TRoot).RequiresPublicMethods ();
				}
			}
		}

		static void TestInterfaceTypeGenericRequirements ()
		{
			IGenericInterfaceTypeWithRequirements<TestType> instance = new InterfaceImplementationTypeWithInstantiatedGenericOnBase ();
			new InterfaceImplementationTypeWithInstantiationOverSelfOnBase ();
			new InterfaceImplementationTypeWithOpenGenericOnBase<TestType> ();
			new InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<TestType> ();

			RecursiveGenericWithInterfacesRequirement.Test ();
		}

		interface IGenericInterfaceTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
		}

		class InterfaceImplementationTypeWithInstantiatedGenericOnBase : IGenericInterfaceTypeWithRequirements<TestType>
		{
		}

		interface IGenericInterfaceTypeWithRequiresAll<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T>
		{
		}

		class InterfaceImplementationTypeWithInstantiationOverSelfOnBase : IGenericInterfaceTypeWithRequiresAll<InterfaceImplementationTypeWithInstantiationOverSelfOnBase>
		{
		}

		[ExpectedWarning ("IL2091", nameof (IGenericInterfaceTypeWithRequirements<T>))]
		class InterfaceImplementationTypeWithOpenGenericOnBase<T> : IGenericInterfaceTypeWithRequirements<T>
		{
		}
		class InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: IGenericInterfaceTypeWithRequirements<T>
		{
		}

		class RecursiveGenericWithInterfacesRequirement
		{
			interface IFace<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.Interfaces)] T>
			{
			}

			class TestType : IFace<TestType>
			{
			}

			public static void Test ()
			{
				var a = typeof (IFace<string>);
				var t = new TestType ();
			}
		}

		static void TestTypeGenericRequirementsOnMembers ()
		{
			// Basically just root everything we need to test
			var instance = new TypeGenericRequirementsOnMembers<TestType> ();

			_ = instance.PublicFieldsField;
			_ = instance.PublicMethodsField;

			_ = instance.PublicFieldsProperty;
			instance.PublicFieldsProperty = null;
			_ = instance.PublicMethodsProperty;
			instance.PublicMethodsProperty = null;
			_ = instance.PublicMethodsImplicitGetter;

			instance.PublicFieldsMethodParameter (null);
			instance.PublicMethodsMethodParameter (null);

			instance.PublicFieldsMethodReturnValue ();
			instance.PublicMethodsMethodReturnValue ();

			instance.PublicFieldsMethodLocalVariable ();
			instance.PublicMethodsMethodLocalVariable ();
		}

		class TypeGenericRequirementsOnMembers<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
		{
			public TypeRequiresPublicFields<TOuter> PublicFieldsField;

			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			public TypeRequiresPublicMethods<TOuter> PublicMethodsField;

			public TypeRequiresPublicFields<TOuter> PublicFieldsProperty {
				get;
				set;
			}

			public TypeRequiresPublicMethods<TOuter> PublicMethodsProperty {
				[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				get => null;
				[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
				set { }
			}

			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer, CompilerGeneratedCode = true)] // NativeAOT_StorageSpaceType
			public TypeRequiresPublicMethods<TOuter> PublicMethodsImplicitGetter => null;

			public void PublicFieldsMethodParameter (TypeRequiresPublicFields<TOuter> param) { }
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			public void PublicMethodsMethodParameter (TypeRequiresPublicMethods<TOuter> param) { }

			public TypeRequiresPublicFields<TOuter> PublicFieldsMethodReturnValue () { return null; }

			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			public TypeRequiresPublicMethods<TOuter> PublicMethodsMethodReturnValue () { return null; }

			public void PublicFieldsMethodLocalVariable ()
			{
				TypeRequiresPublicFields<TOuter> t = null;
			}

			// The analyzer matches NativeAot behavior for local variables - it doesn't warn on generic types of local variables.
			[ExpectedWarning ("IL2091", nameof (TypeRequiresPublicMethods<TOuter>), ProducedBy = Tool.Trimmer)] // NativeAOT_StorageSpaceType
			public void PublicMethodsMethodLocalVariable ()
			{
				TypeRequiresPublicMethods<TOuter> t = null;
			}
		}

		static void TestPartialInstantiationTypes ()
		{
			_ = new PartialyInstantiatedFields<TestType> ();
			_ = new FullyInstantiatedOverPartiallyInstantiatedFields ();
			_ = new PartialyInstantiatedMethods<TestType> ();
			_ = new FullyInstantiatedOverPartiallyInstantiatedMethods ();
		}

		class BaseForPartialInstantiation<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods>
		{
		}

		class PartialyInstantiatedFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TOuter, TestType>
		{
		}

		class FullyInstantiatedOverPartiallyInstantiatedFields
			: PartialyInstantiatedFields<TestType>
		{
		}

		[ExpectedWarning ("IL2091", nameof (BaseForPartialInstantiation<TestType, TOuter>), "'TMethods'")]
		class PartialyInstantiatedMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TestType, TOuter>
		{
			[ExpectedWarning ("IL2091", nameof (BaseForPartialInstantiation<TestType, TOuter>), "'TMethods'")]
			public PartialyInstantiatedMethods () { }
		}

		class FullyInstantiatedOverPartiallyInstantiatedMethods
			: PartialyInstantiatedMethods<TestType>
		{
		}

		static void TestSingleGenericParameterOnMethod ()
		{
			MethodRequiresPublicFields<TestType> ();
			MethodRequiresPublicMethods<TestType> ();
			MethodRequiresNothing<TestType> ();
			MethodRequiresPublicFieldsPassThrough<TestType> ();
			MethodRequiresNothingPassThrough<TestType> ();
		}

		/// <summary>
		/// Adding a comment to verify that analyzer doesn't null ref when trying to analyze
		/// generic parameter comments on a method
		/// <see cref="MethodRequiresPublicFields{T}"/>
		/// </summary>
		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void MethodRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void MethodRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[RequiresUnreferencedCode ("message")]
		static void RUCMethodRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).RequiresPublicFields ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void MethodRequiresNothing<T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicMethods), "'T'")]
		static void MethodRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicFields), "'T'")]
		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicMethods), "'T'")]
		static void MethodRequiresNothingPassThrough<T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		static void TestMultipleGenericParametersOnMethod ()
		{
			MethodMultipleWithDifferentRequirements_TestMultiple<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestFields<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestMethods<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestBoth<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestNothing<TestType, TestType, TestType, TestType> ();
		}

		static void MethodMultipleWithDifferentRequirements_TestMultiple<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			typeof (TFields).RequiresPublicFields (); ;
			typeof (TMethods).RequiresPublicMethods ();
			typeof (TBoth).RequiresPublicFields (); ;
			typeof (TBoth).RequiresPublicMethods ();
			typeof (TFields).RequiresNone ();
			typeof (TMethods).RequiresNone ();
			typeof (TBoth).RequiresNone ();
			typeof (TNothing).RequiresNone ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void MethodMultipleWithDifferentRequirements_TestFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			typeof (TFields).RequiresPublicFields (); ;
			typeof (TFields).RequiresPublicMethods ();
			typeof (TFields).RequiresNone ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		static void MethodMultipleWithDifferentRequirements_TestMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			typeof (TMethods).RequiresPublicFields ();
			typeof (TMethods).RequiresPublicMethods ();
			typeof (TMethods).RequiresNone ();
		}

		static void MethodMultipleWithDifferentRequirements_TestBoth<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			typeof (TBoth).RequiresPublicFields ();
			typeof (TBoth).RequiresPublicMethods ();
			typeof (TBoth).RequiresNone ();
		}

		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicFields))]
		[ExpectedWarning ("IL2087", nameof (DataFlowTypeExtensions.RequiresPublicMethods))]
		static void MethodMultipleWithDifferentRequirements_TestNothing<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods,
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicMethods)] TBoth,
			TNothing> ()
		{
			typeof (TNothing).RequiresPublicFields ();
			typeof (TNothing).RequiresPublicMethods ();
			typeof (TNothing).RequiresNone ();
		}

		static void TestMethodGenericParametersViaInheritance ()
		{
			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticRequiresPublicFields<TestType> ();
			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticRequiresPublicFieldsNonGeneric ();

			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticPartialInstantiation ();
			TypeWithInstantiatedGenericMethodViaGenericParameter<TestType>.StaticPartialInstantiationUnrecognized ();

			var instance = new TypeWithInstantiatedGenericMethodViaGenericParameter<TestType> ();

			instance.InstanceRequiresPublicFields<TestType> ();
			instance.InstanceRequiresPublicFieldsNonGeneric ();

			instance.VirtualRequiresPublicFields<TestType> ();
			instance.VirtualRequiresPublicMethods<TestType> ();

			instance.CallInterface ();

			IInterfaceWithGenericMethod interfaceInstance = (IInterfaceWithGenericMethod) instance;
			interfaceInstance.InterfaceRequiresPublicFields<TestType> ();
			interfaceInstance.InterfaceRequiresPublicMethods<TestType> ();
		}

		class BaseTypeWithGenericMethod
		{
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();
			public virtual void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();

			public static void StaticRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				=> typeof (T).RequiresPublicMethods ();
			public void InstanceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				=> typeof (T).RequiresPublicMethods ();
			public virtual void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				=> typeof (T).RequiresPublicMethods ();

			public static void StaticRequiresMultipleGenericParams<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TFields,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TMethods> ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
			}
		}

		interface IInterfaceWithGenericMethod
		{
			void InterfaceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ();
			void InterfaceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ();
		}


		class TypeWithInstantiatedGenericMethodViaGenericParameter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseTypeWithGenericMethod, IInterfaceWithGenericMethod
		{
			[ExpectedWarning ("IL2091",
				"'TInner'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>.StaticRequiresPublicFields<TInner>()",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresPublicMethods<T>()")]
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				StaticRequiresPublicFields<TInner> ();
				StaticRequiresPublicMethods<TInner> ();
			}

			[ExpectedWarning ("IL2091",
				"'TOuter'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresPublicMethods<T>()")]
			public static void StaticRequiresPublicFieldsNonGeneric ()
			{
				StaticRequiresPublicFields<TOuter> ();
				StaticRequiresPublicMethods<TOuter> ();
			}

			public static void StaticPartialInstantiation ()
			{
				StaticRequiresMultipleGenericParams<TOuter, TestType> ();
			}

			[ExpectedWarning ("IL2091",
				nameof (TOuter),
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
				"TMethods",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams<TFields, TMethods>()",
				ProducedBy = Tool.Analyzer)]
			[ExpectedWarning ("IL2091",
				"'TOuter'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter",
				"'TMethods'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams",
				ProducedBy = Tool.Trimmer | Tool.NativeAot)]
			public static void StaticPartialInstantiationUnrecognized ()
			{
				StaticRequiresMultipleGenericParams<TestType, TOuter> ();
			}

			[ExpectedWarning ("IL2091",
				"'TInner'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter", "InstanceRequiresPublicFields",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.InstanceRequiresPublicMethods")]
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				InstanceRequiresPublicFields<TInner> ();
				InstanceRequiresPublicMethods<TInner> ();
			}

			[ExpectedWarning ("IL2091",
				"'TOuter'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.InstanceRequiresPublicMethods")]
			public void InstanceRequiresPublicFieldsNonGeneric ()
			{
				InstanceRequiresPublicFields<TOuter> ();
				InstanceRequiresPublicMethods<TOuter> ();
			}

			public override void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (T).RequiresPublicFields ();
			}

			public override void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			public void InterfaceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (T).RequiresPublicFields (); ;
			}

			public void InterfaceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			[ExpectedWarning ("IL2091",
				"'TOuter'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter",
				"'T'",
				"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.IInterfaceWithGenericMethod.InterfaceRequiresPublicMethods")]
			public void CallInterface ()
			{
				IInterfaceWithGenericMethod interfaceInstance = (IInterfaceWithGenericMethod) this;
				interfaceInstance.InterfaceRequiresPublicFields<TOuter> ();
				interfaceInstance.InterfaceRequiresPublicMethods<TOuter> ();
			}
		}

		static void TestNewConstraintSatisfiesParameterlessConstructor<T> () where T : new()
		{
			RequiresParameterlessConstructor<T> ();
		}

		static void TestStructConstraintSatisfiesParameterlessConstructor<T> () where T : struct
		{
			RequiresParameterlessConstructor<T> ();
		}
		static void TestUnmanagedConstraintSatisfiesParameterlessConstructor<T> () where T : unmanaged
		{
			RequiresParameterlessConstructor<T> ();
		}

		static void RequiresParameterlessConstructor<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> ()
		{
		}

		// Warn about calls to static methods:
		[ExpectedWarning ("IL2026", "TypeRequiresPublicFields", "RUCTest()", "message")]
		[ExpectedWarning ("IL2026", "RUCMethodRequiresPublicMethods", "message")]
		// And about type/method generic parameters on the RUC methods:
		[ExpectedWarning ("IL2091", "TypeRequiresPublicFields")]
		[ExpectedWarning ("IL2091", "RUCMethodRequiresPublicMethods")]
		static void TestNoWarningsInRUCMethod<T> ()
		{
			TypeRequiresPublicFields<T>.RUCTest ();
			RUCMethodRequiresPublicMethods<T> ();
		}

		// Warn about calls to the static methods and the ctor on the RUC type:
		[ExpectedWarning ("IL2026", "StaticMethod", "message")]
		[ExpectedWarning ("IL2026", "StaticMethodRequiresPublicMethods", "message")]
		[ExpectedWarning ("IL2026", "StaticMethodRequiresPublicMethods", "message")]
		[ExpectedWarning ("IL2026", "StaticMethodRequiresPublicMethods", "message")]
		[ExpectedWarning ("IL2026", "RUCTypeRequiresPublicFields", "message")]
		// And about method generic parameters:
		[ExpectedWarning ("IL2091", "InstanceMethodRequiresPublicMethods")]
		[ExpectedWarning ("IL2091", "StaticMethodRequiresPublicMethods")]
		[ExpectedWarning ("IL2091", "StaticMethodRequiresPublicMethods")]
		[ExpectedWarning ("IL2091", "VirtualMethodRequiresPublicMethods")]
		// And about type generic parameters: (one for each reference to the type):
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields")] // StaticMethod
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields")] // StaticMethodRequiresPublicMethods<T>
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields")] // StaticMethodRequiresPublicMethods<U>
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields")] // RUCTypeRequiresPublicFields<T> ctor
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields", ProducedBy = Tool.Trimmer)] // RUCTypeRequiresPublicFields<T> local, // NativeAOT_StorageSpaceType
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields", ProducedBy = Tool.Trimmer)] // InstanceMethod, // NativeAOT_StorageSpaceType
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields")] // InstanceMethodRequiresPublicMethods<T>
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields", ProducedBy = Tool.Trimmer)] // VirtualMethod, // NativeAOT_StorageSpaceType
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields")] // VirtualMethodRequiresPublicMethods<T>
		static void TestNoWarningsInRUCType<T, U> ()
		{
			RUCTypeRequiresPublicFields<T>.StaticMethod ();
			RUCTypeRequiresPublicFields<T>.StaticMethodRequiresPublicMethods<T> ();
			RUCTypeRequiresPublicFields<U>.StaticMethodRequiresPublicMethods<T> ();
			RUCTypeRequiresPublicFields<int>.StaticMethodRequiresPublicMethods<string> ();
			var rucType = new RUCTypeRequiresPublicFields<T> ();
			rucType.InstanceMethod ();
			rucType.InstanceMethodRequiresPublicMethods<T> ();
			rucType.VirtualMethod ();
			rucType.VirtualMethodRequiresPublicMethods<T> ();
		}

		[RequiresUnreferencedCode ("message")]
		public class RUCTypeRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public static void StaticMethod ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			public static void StaticMethodRequiresPublicMethods<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] U> ()
			{
				typeof (U).RequiresPublicFields ();
			}

			public void InstanceMethod ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			public void InstanceMethodRequiresPublicMethods<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] U> ()
			{
				typeof (U).RequiresPublicFields ();
			}

			public virtual void VirtualMethod ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			public virtual void VirtualMethodRequiresPublicMethods<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] U> ()
			{
				typeof (U).RequiresPublicFields ();
			}
		}

		static void TestGenericParameterFlowsToField ()
		{
			TypeRequiresPublicFields<TestType>.TestFields ();
		}

		[ExpectedWarning ("IL2091", nameof (MethodRequiresPublicFields))]
		static void TestGenericParameterFlowsToDelegateMethod<T> ()
		{
			Action a = MethodRequiresPublicFields<T>;
		}

		[ExpectedWarning ("IL2091", nameof (DelegateMethodTypeRequiresFields<T>), nameof (DelegateMethodTypeRequiresFields<T>.Method))]
		static void TestGenericParameterFlowsToDelegateMethodDeclaringType<T> ()
		{
			Action a = DelegateMethodTypeRequiresFields<T>.Method;
		}

		[ExpectedWarning ("IL2091", nameof (DelegateMethodTypeRequiresFields<T>))]
		// NativeAOT_StorageSpaceType: illink warns about the type of 'instance' local variable
		[ExpectedWarning ("IL2091", nameof (DelegateMethodTypeRequiresFields<T>), ProducedBy = Tool.Trimmer)]
		// NativeAOT_StorageSpaceType: illink warns about the declaring type of 'InstanceMethod' on ldftn
		[ExpectedWarning ("IL2091", nameof (DelegateMethodTypeRequiresFields<T>), ProducedBy = Tool.Trimmer)]
		static void TestGenericParameterFlowsToDelegateMethodDeclaringTypeInstance<T> ()
		{
			var instance = new DelegateMethodTypeRequiresFields<T> ();
			Action a = instance.InstanceMethod;
		}

		class DelegateMethodTypeRequiresFields<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			public static void Method ()
			{
			}

			public void InstanceMethod ()
			{
			}
		}

		public class TestType
		{
		}
		public struct TestStruct
		{
		}

	}
}
