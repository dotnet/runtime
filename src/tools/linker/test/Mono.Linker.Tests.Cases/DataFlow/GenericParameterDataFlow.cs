using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
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

			TestNoWarningsInRUCMethod<TestType> ();
			TestNoWarningsInRUCType<TestType> ();
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

			[RequiresUnreferencedCode ("message")]
			public static void RUCTest ()
			{
				typeof (T).RequiresPublicMethods ();
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
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T",
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
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicFields<>), null, genericParameter: "T", messageCode: "IL2091")]
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")]
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
			new DerivedTypeWithInstantiationOverSelfOnBase ();
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

		class GenericBaseTypeWithRequiresAll<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class DerivedTypeWithInstantiationOverSelfOnBase : GenericBaseTypeWithRequirements<DerivedTypeWithInstantiationOverSelfOnBase>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericBaseTypeWithRequirements<>), null, genericParameter: "T", messageCode: "IL2091")]
		class DerivedTypeWithOpenGenericOnBase<T> : GenericBaseTypeWithRequirements<T>
		{
			[UnrecognizedReflectionAccessPattern (typeof (GenericBaseTypeWithRequirements<>), null, genericParameter: "T", messageCode: "IL2091")]
			public DerivedTypeWithOpenGenericOnBase () { }
		}

		[RecognizedReflectionAccessPattern]
		class DerivedTypeWithOpenGenericOnBaseWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T>
			: GenericBaseTypeWithRequirements<T>
		{
		}

		static void TestInterfaceTypeGenericRequirements ()
		{
			IGenericInterfaceTypeWithRequirements<TestType> instance = new InterfaceImplementationTypeWithInstantiatedGenericOnBase ();
			new InterfaceImplementationTypeWithInstantiationOverSelfOnBase ();
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

		interface IGenericInterfaceTypeWithRequiresAll<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T>
		{
		}

		[RecognizedReflectionAccessPattern]
		class InterfaceImplementationTypeWithInstantiationOverSelfOnBase : IGenericInterfaceTypeWithRequiresAll<InterfaceImplementationTypeWithInstantiationOverSelfOnBase>
		{
		}

		[UnrecognizedReflectionAccessPattern (typeof (IGenericInterfaceTypeWithRequirements<>), null, genericParameter: "T", messageCode: "IL2091")]
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

			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")]
			public TypeRequiresPublicMethods<TOuter> PublicMethodsField;

			public TypeRequiresPublicFields<TOuter> PublicFieldsProperty {
				[RecognizedReflectionAccessPattern]
				get;
				[RecognizedReflectionAccessPattern]
				set;
			}

			public TypeRequiresPublicMethods<TOuter> PublicMethodsProperty {
				[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")]
				get => null;
				[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")]
				set { }
			}

			[RecognizedReflectionAccessPattern]
			public void PublicFieldsMethodParameter (TypeRequiresPublicFields<TOuter> param) { }
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")]
			public void PublicMethodsMethodParameter (TypeRequiresPublicMethods<TOuter> param) { }

			[RecognizedReflectionAccessPattern]
			public TypeRequiresPublicFields<TOuter> PublicFieldsMethodReturnValue () { return null; }
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")] // Return value
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")] // Compiler generated local variable
			public TypeRequiresPublicMethods<TOuter> PublicMethodsMethodReturnValue () { return null; }

			[RecognizedReflectionAccessPattern]
			public void PublicFieldsMethodLocalVariable ()
			{
				TypeRequiresPublicFields<TOuter> t = null;
			}
			[UnrecognizedReflectionAccessPattern (typeof (TypeRequiresPublicMethods<>), null, genericParameter: "T", messageCode: "IL2091")]
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

		[UnrecognizedReflectionAccessPattern (typeof (BaseForPartialInstantiation<,>), null, genericParameter: "TMethods", messageCode: "IL2091")]
		class PartialyInstantiatedMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] TOuter>
			: BaseForPartialInstantiation<TestType, TOuter>
		{
			[UnrecognizedReflectionAccessPattern (typeof (BaseForPartialInstantiation<,>), null, genericParameter: "TMethods", messageCode: "IL2091")]
			public PartialyInstantiatedMethods () { }
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

		[RequiresUnreferencedCode ("message")]
		static void RUCMethodRequiresPublicMethods<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).RequiresPublicFields ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicFields), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		[UnrecognizedReflectionAccessPattern (typeof (DataFlowTypeExtensions), nameof (DataFlowTypeExtensions.RequiresPublicMethods), new Type[] { typeof (Type) }, messageCode: "IL2087")]
		static void MethodRequiresNothing<T> ()
		{
			typeof (T).RequiresPublicFields ();
			typeof (T).RequiresPublicMethods ();
			typeof (T).RequiresNone ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T", messageCode: "IL2091")]
		static void MethodRequiresPublicFieldsPassThrough<
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
		{
			MethodRequiresPublicFields<T> ();
			MethodRequiresPublicMethods<T> ();
			MethodRequiresNothing<T> ();
		}

		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicFields) + "<T>", new Type[0] { }, genericParameter: "T", messageCode: "IL2091")]
		[UnrecognizedReflectionAccessPattern (typeof (GenericParameterDataFlow), nameof (MethodRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T", messageCode: "IL2091")]
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
			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T",
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

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T",
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

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams) + "<TFields,TMethods>", new Type[0] { }, genericParameter: "TMethods",
				messageCode: "IL2091", message: new string[] {
					"TOuter",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.TypeWithInstantiatedGenericMethodViaGenericParameter<TOuter>",
					"TMethods",
					"Mono.Linker.Tests.Cases.DataFlow.GenericParameterDataFlow.BaseTypeWithGenericMethod.StaticRequiresMultipleGenericParams<TFields,TMethods>()" })]
			public static void StaticPartialInstantiationUnrecognized ()
			{
				StaticRequiresMultipleGenericParams<TestType, TOuter> ();
			}

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.InstanceRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T",
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

			[UnrecognizedReflectionAccessPattern (typeof (BaseTypeWithGenericMethod), nameof (BaseTypeWithGenericMethod.InstanceRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T",
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

			[UnrecognizedReflectionAccessPattern (typeof (IInterfaceWithGenericMethod), nameof (IInterfaceWithGenericMethod.InterfaceRequiresPublicMethods) + "<T>", new Type[0] { }, genericParameter: "T",
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
				TestWithUnknownTypeArray (null);
				TestWithArrayUnknownIndexSet (0);
				TestWithArrayUnknownLengthSet (1);
				TestNoArguments ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromParamWithMismatch (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsFromGenericParamWithMismatch<TestType> ();

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

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithUnknownTypeArray (Type[] types)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithArrayUnknownIndexSet (int indexToSet)
			{
				Type[] types = new Type[1];
				types[indexToSet] = typeof (TestType);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[UnrecognizedReflectionAccessPattern (typeof (Type), nameof (Type.MakeGenericType), new Type[] { typeof (Type[]) },
				messageCode: "IL2055")]
			static void TestWithArrayUnknownLengthSet (int arrayLen)
			{
				Type[] types = new Type[arrayLen];
				types[0] = typeof (TestType);
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (types);
			}

			[RecognizedReflectionAccessPattern]
			static void TestNoArguments ()
			{
				typeof (TypeMakeGenericNoArguments).MakeGenericType ();
			}

			class TypeMakeGenericNoArguments
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithRequirements ()
			{
				// Currently this is not analyzable since we don't track array elements.
				// Would be really nice to support this kind of code in the future though.
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TestType));
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithRequirementsFromParam (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			// https://github.com/dotnet/linker/issues/2428
			// [ExpectedWarning ("IL2071", "'T'")]
			[ExpectedWarning ("IL2070", "'this'")]
			static void TestWithRequirementsFromParamWithMismatch (
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (type);
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithRequirementsFromGenericParam<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (T));
			}

			// https://github.com/dotnet/linker/issues/2428
			// [ExpectedWarning ("IL2091", "'T'")]
			[ExpectedWarning ("IL2090", "'this'")] // Note that this actually produces a warning which should not be possible to produce right now
			static void TestWithRequirementsFromGenericParamWithMismatch<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInput> ()
			{
				typeof (GenericWithPublicFieldsArgument<>).MakeGenericType (typeof (TInput));
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

			[RecognizedReflectionAccessPattern]
			static void TestWithMultipleArgumentsWithRequirements ()
			{
				typeof (GenericWithMultipleArgumentsWithRequirements<,>).MakeGenericType (typeof (TestType), typeof (TestType));
			}

			class GenericWithMultipleArgumentsWithRequirements<
				TOne,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TTwo>
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNewConstraint ()
			{
				typeof (GenericWithNewConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithNewConstraint<T> where T : new()
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithStructConstraint ()
			{
				typeof (GenericWithStructConstraint<>).MakeGenericType (typeof (TestType));
			}

			class GenericWithStructConstraint<T> where T : struct
			{
			}

			[RecognizedReflectionAccessPattern]
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
				TestUnknownMethod (null);
				TestUnknownMethodButNoTypeArguments (null);
				TestWithUnknownTypeArray (null);
				TestWithArrayUnknownIndexSet (0);
				TestWithArrayUnknownIndexSetByRef (0);
				TestWithArrayUnknownLengthSet (1);
				TestWithArrayPassedToAnotherMethod ();
				TestWithNoArguments ();
				TestWithArgumentsButNonGenericMethod ();

				TestWithRequirements ();
				TestWithRequirementsFromParam (null);
				TestWithRequirementsFromGenericParam<TestType> ();
				TestWithRequirementsViaRuntimeMethod ();
				TestWithRequirementsButNoTypeArguments ();

				TestWithMultipleKnownGenericParameters ();
				TestWithOneUnknownGenericParameter (null);
				TestWithPartiallyInitializedGenericTypeArray ();
				TestWithConditionalGenericTypeSet (true);

				TestWithNoRequirements ();
				TestWithNoRequirementsFromParam (null);
				TestWithNoRequirementsViaRuntimeMethod ();
				TestWithNoRequirementsUnknownType (null);
				TestWithNoRequirementsWrongNumberOfTypeParameters ();
				TestWithNoRequirementsUnknownArrayElement ();

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
			static void TestUnknownMethod (MethodInfo mi)
			{
				mi.MakeGenericMethod (typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestUnknownMethodButNoTypeArguments (MethodInfo mi)
			{
				// Thechnically linker could figure this out, but it's not worth the complexity - such call will always fail at runtime.
				mi.MakeGenericMethod (Type.EmptyTypes);
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithUnknownTypeArray (Type[] types)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithArrayUnknownIndexSet (int indexToSet)
			{
				Type[] types = new Type[1];
				types[indexToSet] = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithArrayUnknownIndexSetByRef (int indexToSet)
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				ref Type t = ref types[indexToSet];
				t = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithArrayUnknownLengthSet (int arrayLen)
			{
				Type[] types = new Type[arrayLen];
				types[0] = typeof (TestType);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			static void MethodThatTakesArrayParameter (Type[] types)
			{
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithArrayPassedToAnotherMethod ()
			{
				Type[] types = new Type[1];
				types[0] = typeof (TestType);
				MethodThatTakesArrayParameter (types);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithNoArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod ();
			}

			// This should not warn since we can't be always sure about the exact overload as we don't support
			// method parameter signature matching, and thus the GetMethod may return multiple potential methods.
			// It can happen that some are generic and some are not. The analysis should not fail on this.
			[RecognizedReflectionAccessPattern]
			static void TestWithArgumentsButNonGenericMethod ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (NonGenericMethod), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void NonGenericMethod ()
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithRequirements ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType));
			}

			[RecognizedReflectionAccessPattern]
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


			[RecognizedReflectionAccessPattern]
			static void TestWithRequirementsViaRuntimeMethod ()
			{
				typeof (MakeGenericMethod).GetRuntimeMethod (nameof (GenericWithRequirements), Type.EmptyTypes)
					.MakeGenericMethod (typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithRequirementsButNoTypeArguments ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithRequirements), BindingFlags.Static)
					.MakeGenericMethod (Type.EmptyTypes);
			}

			public static void GenericWithRequirements<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T> ()
			{
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithMultipleKnownGenericParameters ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType), typeof (TestType), typeof (TestType));
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithOneUnknownGenericParameter (Type[] types)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (typeof (TestType), typeof (TestStruct), types[0]);
			}

			[UnrecognizedReflectionAccessPattern (typeof (MethodInfo), nameof (MethodInfo.MakeGenericMethod), new Type[] { typeof (Type[]) },
				messageCode: "IL2060")]
			static void TestWithPartiallyInitializedGenericTypeArray ()
			{
				Type[] types = new Type[3];
				types[0] = typeof (TestType);
				types[1] = typeof (TestStruct);
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithConditionalGenericTypeSet (bool thirdParameterIsStruct)
			{
				Type[] types = new Type[3];
				types[0] = typeof (TestType);
				types[1] = typeof (TestStruct);
				if (thirdParameterIsStruct) {
					types[2] = typeof (TestStruct);
				} else {
					types[2] = typeof (TestType);
				}
				typeof (MakeGenericMethod).GetMethod (nameof (GenericMultipleParameters), BindingFlags.Static)
					.MakeGenericMethod (types);
			}

			public static void GenericMultipleParameters<
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] T,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] U,
				[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)] V> ()
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

			// There are no requirements, so no warnings
			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirementsUnknownType (Type type)
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (type);
			}

			// There are no requirements, so no warnings
			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirementsWrongNumberOfTypeParameters ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (typeof (TestType), typeof (TestType));
			}

			// There are no requirements, so no warnings
			[RecognizedReflectionAccessPattern]
			static void TestWithNoRequirementsUnknownArrayElement ()
			{
				Type[] types = new Type[1];
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNoRequirements))
					.MakeGenericMethod (types);
			}

			public static void GenericWithNoRequirements<T> ()
			{
			}


			[RecognizedReflectionAccessPattern]
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

			[RecognizedReflectionAccessPattern]
			static void TestWithNewConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithNewConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithNewConstraint<T> () where T : new()
			{
				var t = new T ();
			}

			[RecognizedReflectionAccessPattern]
			static void TestWithStructConstraint ()
			{
				typeof (MakeGenericMethod).GetMethod (nameof (GenericWithStructConstraint), BindingFlags.Static | BindingFlags.NonPublic)
					.MakeGenericMethod (typeof (TestType));
			}

			static void GenericWithStructConstraint<T> () where T : struct
			{
				var t = new T ();
			}

			[RecognizedReflectionAccessPattern]
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

		// Warn about calls to static methods:
		[ExpectedWarning ("IL2026", "TypeRequiresPublicFields<T>.RUCTest()", "message")]
		[ExpectedWarning ("IL2026", "RUCMethodRequiresPublicMethods<T>()", "message")]
		// And about type/method generic parameters on the RUC methods:
		[ExpectedWarning ("IL2091", "TypeRequiresPublicFields<T>")]
		[ExpectedWarning ("IL2091", "RUCMethodRequiresPublicMethods<T>()")]
		static void TestNoWarningsInRUCMethod<T> ()
		{
			TypeRequiresPublicFields<T>.RUCTest ();
			RUCMethodRequiresPublicMethods<T> ();
		}

		// Warn about calls to the static methods and the ctor on the RUC type:
		[ExpectedWarning ("IL2026", "RUCTypeRequiresPublicFields<T>.StaticMethod", "message")]
		[ExpectedWarning ("IL2026", "RUCTypeRequiresPublicFields<T>.StaticMethodRequiresPublicMethods<U>", "message")]
		[ExpectedWarning ("IL2026", "RUCTypeRequiresPublicFields<T>.RUCTypeRequiresPublicFields", "message")]
		// And about method generic parameters:
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>.InstanceMethodRequiresPublicMethods<U>()")]
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>.StaticMethodRequiresPublicMethods<U>()")]
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>.VirtualMethodRequiresPublicMethods<U>()")]
		// And about type generic parameters: (one for each reference to the type):
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // StaticMethod
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // StaticMethodRequiresPublicMethods<T>
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // RUCTypeRequiresPublicFields<T> local
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // RUCTypeRequiresPublicFields<T> ctor
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // InstanceMethod
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // InstanceMethodRequiresPublicMethods<T>
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // VirtualMethod
		[ExpectedWarning ("IL2091", "RUCTypeRequiresPublicFields<T>")] // VirtualMethodRequiresPublicMethods<T>
		static void TestNoWarningsInRUCType<T> ()
		{
			RUCTypeRequiresPublicFields<T>.StaticMethod ();
			RUCTypeRequiresPublicFields<T>.StaticMethodRequiresPublicMethods<T> ();
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

		public class TestType
		{
		}

		public struct TestStruct
		{
		}
	}
}
