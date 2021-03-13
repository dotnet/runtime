using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Security.Policy;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using BindingFlags = System.Reflection.BindingFlags;

namespace Mono.Linker.Tests.Cases.DataFlow
{
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

			MakeGenericType.Test ();
			MakeGenericMethod.Test ();

			TestNewConstraintSatisfiesParameterlessConstructor<object> ();
			TestStructConstraintSatisfiesParameterlessConstructor<TestStruct> ();
			TestUnmanagedConstraintSatisfiesParameterlessConstructor<byte> ();

			TestGenericParameterFlowsToField ();
			TestGenericParameterFlowsToReturnValue ();
		}

		static void TestSingleGenericParameterOnType ()
		{
			TypeRequiresNothing<TestType>.Test ();
			TypeRequiresPublicFields<TestType>.Test ();
			TypeRequiresPublicMethods<TestType>.Test ();
			TypeRequiresPublicFieldsPassThrough<TestType>.Test ();
			TypeRequiresNothingPassThrough<TestType>.Test ();
		}

		static void TestGenericParameterFlowsToField ()
		{
			TypeRequiresPublicFields<TestType>.TestFields ();
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
			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) },
				messageCode: "IL2087", message: new string[] {
					nameof (T),
					nameof (TypeRequiresPublicFields <T>),
					nameof (DataFlowTypeExtensions.RequiresPublicMethods)
				})]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (FieldRequiresPublicMethods),
				messageCode: "IL2089", message: new string[] {
					nameof (T),
					nameof (TypeRequiresPublicFields <T>),
					nameof (FieldRequiresPublicMethods)
				})]
			public static void TestFields ()
			{
				FieldRequiresPublicFields = typeof (T);
				FieldRequiresPublicMethods = typeof (T);
				FieldRequiresNothing = typeof (T);
			}


			[RecognizedReflectionAccessPattern]
			[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public static Type ReturnRequiresPublicFields ()
			{
				return typeof (T);
			}


			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicFields<>), nameof (ReturnRequiresPublicMethods), new Type[] { }, returnType: typeof (Type),
				messageCode: "IL2088", message: new string[] {
					nameof (T),
					nameof (TypeRequiresPublicFields<T>),
					nameof (ReturnRequiresPublicMethods)
				})]
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
			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
			public static void Test ()
			{
				typeof (T).RequiresPublicFields ();
				typeof (T).RequiresPublicMethods ();
				typeof (T).RequiresNone ();
			}
		}

		class TypeRequiresNothing<T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
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
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T",
				messageCode: "IL2091", message: new string[] {
					nameof(TSource),
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeRequiresPublicFieldsPassThrough<TSource>",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeRequiresPublicMethods<T>" })]
			public static void Test ()
			{
				TypeRequiresPublicFields<TSource>.Test ();
				TypeRequiresPublicMethods<TSource>.Test ();
				TypeRequiresNothing<TSource>.Test ();
			}
		}

		class TypeRequiresNothingPassThrough<T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicFields<>), "T", messageCode: "IL2091")]
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")]
			public static void Test ()
			{
				TypeRequiresPublicFields<T>.Test ();
				TypeRequiresPublicMethods<T>.Test ();
				TypeRequiresNothing<T>.Test ();
			}
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
			[RecognizedReflectionAccessPattern]
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

			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
			public static void TestFields ()
			{
				typeof (TFields).RequiresPublicFields ();
				typeof (TFields).RequiresPublicMethods ();
				typeof (TFields).RequiresNone ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
			public static void TestMethods ()
			{
				typeof (TMethods).RequiresPublicFields ();
				typeof (TMethods).RequiresPublicMethods ();
				typeof (TMethods).RequiresNone ();
			}

			[RecognizedReflectionAccessPattern]
			public static void TestBoth ()
			{
				typeof (TBoth).RequiresPublicFields ();
				typeof (TBoth).RequiresPublicMethods ();
				typeof (TBoth).RequiresNone ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
			[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
			public static void TestNothing ()
			{
				typeof (TNothing).RequiresPublicFields ();
				typeof (TNothing).RequiresPublicMethods ();
				typeof (TNothing).RequiresNone ();
			}
		}

		[RecognizedReflectionAccessPattern]
		static void TestBaseTypeGenericRequirements ()
		{
			new DerivedTypeWithInstantiatedGenericOnBase ();
			new DerivedTypeWithOpenGenericOnBase<TestType> ();
			new DerivedTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		class GenericBaseTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
			[RecognizedReflectionAccessPattern]
			public GenericBaseTypeWithRequirements ()
			{
				typeof (T).RequiresPublicFields ();
			}
		}

		[RecognizedReflectionAccessPattern]
		class DerivedTypeWithInstantiatedGenericOnBase : GenericBaseTypeWithRequirements<TestType>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericBaseTypeWithRequirements<>), "T", messageCode: "IL2091")]
		class DerivedTypeWithOpenGenericOnBase<T> : GenericBaseTypeWithRequirements<T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class DerivedTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: GenericBaseTypeWithRequirements<T>
		{
		}

		static void TestInterfaceTypeGenericRequirements ()
		{
			IGenericInterfaceTypeWithRequirements<TestType> instance = new InterfaceImplementationTypeWithInstantiatedGenericOnBase ();
			new InterfaceImplementationTypeWithOpenGenericOnBase<TestType> ();
			new InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<TestType> ();
		}

		interface IGenericInterfaceTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class InterfaceImplementationTypeWithInstantiatedGenericOnBase : IGenericInterfaceTypeWithRequirements<TestType>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (IGenericInterfaceTypeWithRequirements<>), "T", messageCode: "IL2091")]
		class InterfaceImplementationTypeWithOpenGenericOnBase<T> : IGenericInterfaceTypeWithRequirements<T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class InterfaceImplementationTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: IGenericInterfaceTypeWithRequirements<T>
		{
		}

		[RecognizedReflectionAccessPattern]
		static void TestDeepNestedTypesWithGenerics ()
		{
			RootTypeWithRequirements<TestType>.InnerTypeWithNoAddedGenerics.TestAccess ();
		}

		class RootTypeWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TRoot>
		{
			public class InnerTypeWithNoAddedGenerics
			{
				// The message is not ideal since we report the TRoot to come from RootTypeWithRequirements/InnerTypeWIthNoAddedGenerics
				// while it originates on RootTypeWithRequirements, but it's correct from IL's point of view.
				[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) },
					messageCode: "IL2087", message: new string[] {
						nameof(TRoot),
						"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.RootTypeWithRequirements<TRoot>.InnerTypeWithNoAddedGenerics",
						"type",
						"DataFlowTypeExtensions.RequiresPublicMethods(Type)" })]
				public static void TestAccess ()
				{
					typeof (TRoot).RequiresPublicFields ();
					typeof (TRoot).RequiresPublicMethods ();
				}
			}
		}

		[RecognizedReflectionAccessPattern]
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

			instance.PublicFieldsMethodParameter (null);
			instance.PublicMethodsMethodParameter (null);

			instance.PublicFieldsMethodReturnValue ();
			instance.PublicMethodsMethodReturnValue ();

			instance.PublicFieldsMethodLocalVariable ();
			instance.PublicMethodsMethodLocalVariable ();
		}

		class TypeGenericRequirementsOnMembers<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
		{
			[RecognizedReflectionAccessPattern]
			public TypeRequiresPublicFields<TOuter> PublicFieldsField;

			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")]
			public TypeRequiresPublicMethods<TOuter> PublicMethodsField;

			public TypeRequiresPublicFields<TOuter> PublicFieldsProperty {
				[RecognizedReflectionAccessPattern]
				get;
				[RecognizedReflectionAccessPattern]
				set;
			}
			public TypeRequiresPublicMethods<TOuter> PublicMethodsProperty {
				[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")]
				get;
				[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")]
				set;
			}

			[RecognizedReflectionAccessPattern]
			public void PublicFieldsMethodParameter (TypeRequiresPublicFields<TOuter> param) { }
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")]
			public void PublicMethodsMethodParameter (TypeRequiresPublicMethods<TOuter> param) { }

			[RecognizedReflectionAccessPattern]
			public TypeRequiresPublicFields<TOuter> PublicFieldsMethodReturnValue () { return null; }
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")] // Return value
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")] // Compiler generated local variable
			public TypeRequiresPublicMethods<TOuter> PublicMethodsMethodReturnValue () { return null; }

			[RecognizedReflectionAccessPattern]
			public void PublicFieldsMethodLocalVariable ()
			{
				TypeRequiresPublicFields<TOuter> t = null;
			}
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), "T", messageCode: "IL2091")]
			public void PublicMethodsMethodLocalVariable ()
			{
				TypeRequiresPublicMethods<TOuter> t = null;
			}
		}

		[RecognizedReflectionAccessPattern]
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

		[RecognizedReflectionAccessPattern]
		class PartialyInstantiatedFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TOuter, TestType>
		{
		}

		[RecognizedReflectionAccessPattern]
		class FullyInstantiatedOverPartiallyInstantiatedFields
			: PartialyInstantiatedFields<TestType>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (BaseForPartialInstantiation<,>), "TMethods", messageCode: "IL2091")]
		class PartialyInstantiatedMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TestType, TOuter>
		{
		}

		[RecognizedReflectionAccessPattern]
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

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		static void MethodRequiresPublicFields<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		static void MethodRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		static void MethodRequiresNothing<T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicMethods) + "<T>()::T", messageCode: "IL2091")]
		static void MethodRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicFields) + "<T>()::T", messageCode: "IL2091")]
		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicMethods) + "<T>()::T", messageCode: "IL2091")]
		static void MethodRequiresNothingPassThrough<T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
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
			[RecognizedReflectionAccessPattern]
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();
			[RecognizedReflectionAccessPattern]
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();
			[RecognizedReflectionAccessPattern]
			public virtual void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
				=> typeof (T).RequiresPublicFields ();

			[RecognizedReflectionAccessPattern]
			public static void StaticRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				=> typeof (T).RequiresPublicMethods ();
			[RecognizedReflectionAccessPattern]
			public void InstanceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]T> ()
				=> typeof (T).RequiresPublicMethods ();
			[RecognizedReflectionAccessPattern]
			public virtual void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]T> ()
				=> typeof (T).RequiresPublicMethods ();

			[RecognizedReflectionAccessPattern]
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
			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresPublicMethods) + "<T>()::T",
				messageCode: "IL2091", message: new string[] {
					"TInner",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>.StaticRequiresPublicFields<TInner>()",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresPublicMethods<T>()" })]
			public static void StaticRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				StaticRequiresPublicFields<TInner> ();
				StaticRequiresPublicMethods<TInner> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresPublicMethods) + "<T>()::T",
				messageCode: "IL2091", message: new string[] {
					"TOuter",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresPublicMethods<T>()" })]
			public static void StaticRequiresPublicFieldsNonGeneric ()
			{
				StaticRequiresPublicFields<TOuter> ();
				StaticRequiresPublicMethods<TOuter> ();
			}

			[RecognizedReflectionAccessPattern]
			public static void StaticPartialInstantiation ()
			{
				StaticRequiresMultipleGenericParams<TOuter, TestType> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams) + "<TFields,TMethods>()::TMethods",
				messageCode: "IL2091", message: new string[] {
					"TOuter",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
					"TMethods",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams<TFields,TMethods>()" })]
			public static void StaticPartialInstantiationUnrecognized ()
			{
				StaticRequiresMultipleGenericParams<TestType, TOuter> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.InstanceRequiresPublicMethods) + "<T>()::T",
				messageCode: "IL2091", message: new string[] {
					"TInner",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>.InstanceRequiresPublicFields<TInner>()",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.InstanceRequiresPublicMethods<T>()" })]
			public void InstanceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TInner> ()
			{
				InstanceRequiresPublicFields<TInner> ();
				InstanceRequiresPublicMethods<TInner> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.InstanceRequiresPublicMethods) + "<T>()::T",
				messageCode: "IL2091", message: new string[] {
					"TOuter",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.InstanceRequiresPublicMethods<T>()" })]
			public void InstanceRequiresPublicFieldsNonGeneric ()
			{
				InstanceRequiresPublicFields<TOuter> ();
				InstanceRequiresPublicMethods<TOuter> ();
			}

			[RecognizedReflectionAccessPattern]
			public override void VirtualRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (T).RequiresPublicFields ();
			}

			[RecognizedReflectionAccessPattern]
			public override void VirtualRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			[RecognizedReflectionAccessPattern]
			public void InterfaceRequiresPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (T).RequiresPublicFields (); ;
			}

			[RecognizedReflectionAccessPattern]
			public void InterfaceRequiresPublicMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
			{
				typeof (T).RequiresPublicMethods ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (IInterfaceWithGenericMethod), nameof (IInterfaceWithGenericMethod.InterfaceRequiresPublicMethods) + "<T>()::T",
				messageCode: "IL2091", message: new string[] {
					"TOuter",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
					"T",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.IInterfaceWithGenericMethod.InterfaceRequiresPublicMethods<T>()" })]
			public void CallInterface ()
			{
				IInterfaceWithGenericMethod interfaceInstance = (IInterfaceWithGenericMethod) this;
				interfaceInstance.InterfaceRequiresPublicFields<TOuter> ();
				interfaceInstance.InterfaceRequiresPublicMethods<TOuter> ();
			}
		}


		static void TestMultipleGenericParametersOnMethod ()
		{
			MethodMultipleWithDifferentRequirements_TestMultiple<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestFields<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestMethods<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestBoth<TestType, TestType, TestType, TestType> ();
			MethodMultipleWithDifferentRequirements_TestNothing<TestType, TestType, TestType, TestType> ();
		}

		[RecognizedReflectionAccessPattern]
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

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
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

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
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

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
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


		class MakeGenericType
		{
			public static void Test ()
			{
				TestNullType ();
				TestUnknownInput (null);
				TestNoArguments ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromGenericParam<TestType> ();

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);

				TestWithMultipleArgumentsWithRequirements ();

				TestWithNewConstraint ();
				TestWithStructConstraint ();
				TestWithUnmanagedConstraint ();
				TestWithNullable ();
			}

			// This is OK since we know it's null, so MakeGenericType is effectively a no-op (will throw)
			// so no validation necessary.
			[RecognizedReflectionAccessPattern]
			static void TestNullType ()
			{
				Type nullType = null;
				nullType.MakeGenericType (typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestUnknownInput (Type inputType)
			{
				inputType.MakeGenericType (typeof (TestType));
			}

			[RecognizedReflectionAccessPattern]
			static void TestNoArguments ()
			{
				typeof (TypeMakeGenericNoArguments).MakeGenericType ();
			}

			class TypeMakeGenericNoArguments
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithRequirements ()
			{
				// Currently this is not analyzable since we don't track array elements.
				// Would be really nice to support this kind of code in the future though.
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (T));
			}

			class GenericWithPublicFieldsArgument<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirements ()
			{
				typeof (GenericWithNoRequirements<>).MakeGenericType (typeof (TestType));
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirementsFromParam (Type type)
			{
				typeof (GenericWithNoRequirements<>).MakeGenericType (type);
			}

			class GenericWithNoRequirements<T>
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (GenericWithMultipleArgumentsWithRequirements<,>).MakeGenericType (typeof (TestType), typeof (TestType));
			}

			class GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TTwo>
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithNewConstraint ()
			{
				typeof (GenericWithNewConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithNewConstraint<T> where T : new()
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithStructConstraint ()
			{
				typeof (GenericWithStructConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithStructConstraint<T> where T : struct
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithUnmanagedConstraint ()
			{
				typeof (GenericWithUnmanagedConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithUnmanagedConstraint<T> where T : unmanaged
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNullable ()
			{
				typeof (Nullable<>).MakeGenericType (typeof (TestType));
			}
		}

		class MakeGenericMethod
		{
			public static void Test ()
			{
				TestNullMethod ();
				TestUnknownInput (null);
				TestWithNoArguments ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsViaRuntimeMethod ();

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);
				TestWithNoRequirementsViaRuntimeMethod ();

				TestWithMultipleArgumentsWithRequirements ();

				TestWithNewConstraint ();
				TestWithStructConstraint ();
				TestWithUnmanagedConstraint ();
			}

			[RecognizedReflectionAccessPattern]
			static void TestNullMethod ()
			{
				MethodInfo mi = null;
				mi.MakeGenericMethod (typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestUnknownInput (MethodInfo mi)
			{
				mi.MakeGenericMethod (typeof (TestType));
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod ();
			}

			static void NonGenericMethod ()
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (type);
			}

			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (T));
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			public static void GenericWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirementsFromParam (Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements), BindingFlags.Static)
					.MakeGenericMethod (type);
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithNoRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			public static void GenericWithNoRequirements<T> ()
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithMultipleArgumentsWithRequirements), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType), typeof (TestType));
			}

			static void GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TTwo> ()
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithNewConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithNewConstraint<T> () where T : new()
			{
				var t = new T ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithStructConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithStructConstraint<T> () where T : struct
			{
				var t = new T ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithUnmanagedConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithUnmanagedConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithUnmanagedConstraint<T> () where T : unmanaged
			{
				var t = new T ();
			}
		}

		[RecognizedReflectionAccessPattern]
		static void TestNewConstraintSatisfiesParameterlessConstructor<T> () where T : new()
		{
			RequiresParameterlessConstructor<T> ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestStructConstraintSatisfiesParameterlessConstructor<T> () where T : struct
		{
			RequiresParameterlessConstructor<T> ();
		}

		[RecognizedReflectionAccessPattern]
		static void TestUnmanagedConstraintSatisfiesParameterlessConstructor<T> () where T : unmanaged
		{
			RequiresParameterlessConstructor<T> ();
		}

		static void RequiresParameterlessConstructor<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] T> ()
		{
		}

		public class TestType
		{
		}

		public struct TestStruct
		{
		}
	}
}
