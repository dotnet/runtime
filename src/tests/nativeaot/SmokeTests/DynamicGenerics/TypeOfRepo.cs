// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;


//
// This file is used to replace simple typeof() instructions in a way that
// confuses the Reducer's analysis, such that it won't statically create any of 
// the instantiations that we really what to allocate dynamically using the 
// TypeLoader feature
//

// Common class/valuetype types
public enum Int64Enum : long { }
#if USC
public struct CommonType1 { }
public struct CommonType2 { }
public struct CommonType3 { }
public struct CommonType4 { }
public struct CommonType5 { }
public struct CommonType6 { }
public struct CommonType7 { }
public struct CommonType8 { }
public struct CommonType9 { }
public struct CommonType10 { }
public struct CommonType11 { }
#else
public class CommonType1 { }
public class CommonType2 { }
public class CommonType3 { }
public class CommonType4 { }
public class CommonType5 { }
public class CommonType6 { }
public class CommonType7 { }
public class CommonType8 { }
public class CommonType9 { }
public class CommonType10 { }
public class CommonType11 { }
#endif


namespace TypeOfRepo
{
    public partial class TypeOf
    {
        static Dictionary<string, Type> s_TypeRepo;

        static void InitTypeRepoDictionary()
        {
            if (s_TypeRepo == null)
            {
                s_TypeRepo = new Dictionary<string, Type>();
            }
        }

        static TypeOf()
        {
            InitTypeRepoDictionary();

            // Common framework types
            s_TypeRepo["Object"] = typeof(object);
            s_TypeRepo["String"] = typeof(string);
            s_TypeRepo["Func1"] = typeof(Func<>);
            s_TypeRepo["Func2"] = typeof(Func<,>);
            s_TypeRepo["List"] = typeof(List<>);
            s_TypeRepo["Dictionary"] = typeof(Dictionary<,>);
            s_TypeRepo["Type"] = typeof(Type);
            s_TypeRepo["Double"] = typeof(double);
            s_TypeRepo["Int32"] = typeof(int);
            s_TypeRepo["Int16"] = typeof(short);
            s_TypeRepo["Bool"] = typeof(bool);
            s_TypeRepo["Short"] = typeof(short);
            s_TypeRepo["Long"] = typeof(long);
            s_TypeRepo["Float"] = typeof(float);
            s_TypeRepo["Char"] = typeof(char);

            // Common class/valuetype types
            s_TypeRepo["CommonType1"] = typeof(CommonType1);
            s_TypeRepo["CommonType2"] = typeof(CommonType2);
            s_TypeRepo["CommonType3"] = typeof(CommonType3);
            s_TypeRepo["CommonType4"] = typeof(CommonType4);
            s_TypeRepo["CommonType5"] = typeof(CommonType5);
            s_TypeRepo["CommonType6"] = typeof(CommonType6);
            s_TypeRepo["CommonType7"] = typeof(CommonType7);
            s_TypeRepo["CommonType8"] = typeof(CommonType8);
            s_TypeRepo["CommonType9"] = typeof(CommonType9);
            s_TypeRepo["CommonType10"] = typeof(CommonType10);
            s_TypeRepo["CommonType11"] = typeof(CommonType11);
            s_TypeRepo["Int64Enum"] = typeof(Int64Enum);


            // Types in activation.cs
            s_TypeRepo["My"] = typeof(My);
            s_TypeRepo["Foo"] = typeof(Foo);

            // Types in constraints.cs
            s_TypeRepo["CT_TypeWithNoConstraint"] = typeof(ConstraintsTests.TypeWithNoConstraint<>);
            s_TypeRepo["CT_TypeWithClassConstraint"] = typeof(ConstraintsTests.TypeWithClassConstraint<>);
            s_TypeRepo["CT_TypeWithNewConstraint"] = typeof(ConstraintsTests.TypeWithNewConstraint<>);
            s_TypeRepo["CT_TypeWithStructConstraint"] = typeof(ConstraintsTests.TypeWithStructConstraint<>);
            s_TypeRepo["CT_TypeRequiringIFoo"] = typeof(ConstraintsTests.TypeRequiringIFoo<>);
            s_TypeRepo["CT_TypeWithSelfReferenceConstraint"] = typeof(ConstraintsTests.TypeWithSelfReferenceConstraint<,>);
            s_TypeRepo["CT_TypeWithVariance"] = typeof(ConstraintsTests.TypeWithVariance<,>);
            s_TypeRepo["CT_TypeWithRecursiveConstraints"] = typeof(ConstraintsTests.TypeWithRecursiveConstraints<,>);
            s_TypeRepo["CT_TypeWithSelfReferenceIEnumerableConstraint"] = typeof(ConstraintsTests.TypeWithSelfReferenceIEnumerableConstraint<,>);

            // Types in blockedtypes.cs
            s_TypeRepo["BTT_GenericType"] = typeof(BlockedTypesTests.GenericType<>);

            // Types in arrays.cs
            s_TypeRepo["AT_ArrayTests"] = typeof(ArrayTests.ArrayTests);
            s_TypeRepo["AT_SomeClassForArrayTests"] = typeof(ArrayTests.SomeClassForArrayTests);
            s_TypeRepo["AT_SomeClassForArrayTests1"] = typeof(ArrayTests.SomeClassForArrayTests[]);
            s_TypeRepo["AT_SomeClassForArrayTests2"] = typeof(ArrayTests.SomeClassForArrayTests[][]);

            // Types in methodconstraints.cs
            s_TypeRepo["MCT_TypeWithNoConstraint"] = typeof(MethodConstraintsTests.TypeWithNoConstraint);
            s_TypeRepo["MCT_TypeWithClassConstraint"] = typeof(MethodConstraintsTests.TypeWithClassConstraint);
            s_TypeRepo["MCT_TypeWithNewConstraint"] = typeof(MethodConstraintsTests.TypeWithNewConstraint);
            s_TypeRepo["MCT_TypeWithStructConstraint"] = typeof(MethodConstraintsTests.TypeWithStructConstraint);
            s_TypeRepo["MCT_TypeRequiringIFoo"] = typeof(MethodConstraintsTests.TypeRequiringIFoo);
            s_TypeRepo["MCT_TypeWithSelfReferenceConstraint"] = typeof(MethodConstraintsTests.TypeWithSelfReferenceConstraint);
            s_TypeRepo["MCT_TypeWithVariance"] = typeof(MethodConstraintsTests.TypeWithVariance);
            s_TypeRepo["MCT_TypeWithRecursiveConstraints"] = typeof(MethodConstraintsTests.TypeWithRecursiveConstraints);
            s_TypeRepo["MCT_TypeWithSelfReferenceIEnumerableConstraint"] = typeof(MethodConstraintsTests.TypeWithSelfReferenceIEnumerableConstraint);
            s_TypeRepo["MCT_TypeWithMDArrayConstraints"] = typeof(MethodConstraintsTests.TypeWithMDArrayConstraints);
            s_TypeRepo["MCT_GenericType"] = typeof(MethodConstraintsTests.GenericType<,>);

            // Types in threadstatics.cs
            s_TypeRepo["TLS_MyType1"] = typeof(ThreadLocalStatics.MyType1<>);
            s_TypeRepo["TLS_MyDerived1"] = typeof(ThreadLocalStatics.MyDerived1<>);
            s_TypeRepo["TLS_MySuperDerived1_1"] = typeof(ThreadLocalStatics.MySuperDerived1_1<>);
            s_TypeRepo["TLS_MySuperDerived1_2"] = typeof(ThreadLocalStatics.MySuperDerived1_2<>);
            s_TypeRepo["TLS_MyType2"] = typeof(ThreadLocalStatics.MyType2<,>);
            s_TypeRepo["TLS_MyDerived2_1"] = typeof(ThreadLocalStatics.MyDerived2_1<,>);
            s_TypeRepo["TLS_MyDerived2_2"] = typeof(ThreadLocalStatics.MyDerived2_2<,>);
            s_TypeRepo["TLS_T1"] = typeof(ThreadLocalStatics.T1);
            s_TypeRepo["TLS_T2"] = typeof(ThreadLocalStatics.T2);
            s_TypeRepo["TLS_T4"] = typeof(ThreadLocalStatics.T4);
            s_TypeRepo["TLS_T5"] = typeof(ThreadLocalStatics.T5);

            // Types in statics.cs
            s_TypeRepo["ST_GenericTypeWithStaticFieldOfTypeT"] = typeof(StaticsTests.GenericTypeWithStaticFieldOfTypeT<>);
            s_TypeRepo["ST_GenericTypeWithNonGcStaticField"] = typeof(StaticsTests.GenericTypeWithNonGcStaticField<>);
            s_TypeRepo["ST_GenericTypeWithMultipleNonGcStaticFields"] = typeof(StaticsTests.GenericTypeWithMultipleNonGcStaticFields<>);
            s_TypeRepo["ST_SuperDerivedGeneric"] = typeof(StaticsTests.SuperDerivedGeneric<>);
            s_TypeRepo["ST_GenericTypeWithStaticTimeSpanField"] = typeof(StaticsTests.GenericTypeWithStaticTimeSpanField<>);
            s_TypeRepo["ST_GenericTypeWithGcStaticField"] = typeof(StaticsTests.GenericTypeWithGcStaticField<>);
            s_TypeRepo["ST_ClassWithStaticConstructor"] = typeof(StaticsTests.ClassWithStaticConstructor<>);
            s_TypeRepo["ST_AnotherClassWithStaticConstructor"] = typeof(StaticsTests.AnotherClassWithStaticConstructor<>);

            // Types in rdexperience.cs
            s_TypeRepo["RDE_Foo"] = typeof(RdExperienceTests.Foo<>);

            // Types in interfaces.cs
            s_TypeRepo["IT_Gen"] = typeof(InterfacesTests.Gen<>);
            s_TypeRepo["IT_Recursive"] = typeof(InterfacesTests.Recursive<>);
            s_TypeRepo["IT_WithInterfaceOverArrayType"] = typeof(InterfacesTests.WithInterfaceOverArrayType<>);
            s_TypeRepo["IT_DoublyRecursive"] = typeof(InterfacesTests.DoublyRecursive<>);
            s_TypeRepo["IT_Frobber"] = typeof(InterfacesTests.Frobber<>);
            s_TypeRepo["IT_IFrobber"] = typeof(InterfacesTests.IFrobber<>);
            s_TypeRepo["IT_UseFrobber"] = typeof(InterfacesTests.UseFrobber<,>);
            s_TypeRepo["IT_FrobtasticFrobberStruct"] = typeof(InterfacesTests.FrobtasticFrobberStruct);
            s_TypeRepo["IT_AnotherFrobtasticFrobberStruct"] = typeof(InterfacesTests.AnotherFrobtasticFrobberStruct);

            // Types in genericmethods.cs
            s_TypeRepo["GM_Gen"] = typeof(MakeGenMethod.Gen<>);
            s_TypeRepo["GM_NonGenericType"] = typeof(MakeGenMethod.NonGenericType);
            s_TypeRepo["GM_GenericType"] = typeof(MakeGenMethod.GenericType<>);
            s_TypeRepo["GM_Foo"] = typeof(MakeGenMethod.Foo<>);
            s_TypeRepo["GM_Bar"] = typeof(MakeGenMethod.Bar<>);
            s_TypeRepo["GM_MakeGenericMethodTest"] = typeof(MakeGenMethod.Test);

            // Types in fieldreflection.cs
            s_TypeRepo["FRT_BaseType2"] = typeof(FieldReflectionTests.BaseType2<>);
            s_TypeRepo["FRT_DerivedTypeWithVariousFields"] = typeof(FieldReflectionTests.DerivedTypeWithVariousFields<,>);
            s_TypeRepo["FRT_ReferenceTypeWithVariousFields"] = typeof(FieldReflectionTests.ReferenceTypeWithVariousFields<,>);
            s_TypeRepo["FRT_ReferenceTypeWithCCtor"] = typeof(FieldReflectionTests.ReferenceTypeWithCCtor<>);
            s_TypeRepo["FRT_ValueTypeWithVariousFields"] = typeof(FieldReflectionTests.ValueTypeWithVariousFields<,>);
            s_TypeRepo["FRT_ValueTypeWithCCtor"] = typeof(FieldReflectionTests.ValueTypeWithCCtor<>);

            // Types in expressions.cs
            s_TypeRepo["E_TestRunner"] = typeof(Expressions.TestRunner<>);
            s_TypeRepo["E_MyType1"] = typeof(Expressions.MyType1);
            s_TypeRepo["E_MyType2"] = typeof(Expressions.MyType2);

            // Types in dictionaries.cs
            {
                s_TypeRepo["D_Gen"] = typeof(Dictionaries.Gen<>);
                s_TypeRepo["D_GenBase"] = typeof(Dictionaries.GenBase<>);
                s_TypeRepo["D_Base"] = typeof(Dictionaries.Base);
                s_TypeRepo["D_IFace"] = typeof(Dictionaries.IFace<>);
                s_TypeRepo["D_IFace3"] = typeof(Dictionaries.IFace3<>);
                s_TypeRepo["D_SingleUseArrayOnlyGen"] = typeof(Dictionaries.SingleUseArrayOnlyGen<>);
                s_TypeRepo["D_GenericStruct"] = typeof(Dictionaries.GenericStruct<>);
                s_TypeRepo["D_NullableTest"] = typeof(Dictionaries.NullableTest<>);
                s_TypeRepo["D_DelegateTarget"] = typeof(Dictionaries.DelegateTarget<>);
                s_TypeRepo["D_DelWithNullable"] = typeof(Dictionaries.DelWithNullable<>);

                s_TypeRepo["TDT_MyStruct"] = typeof(TypeDictTestTypes.MyStruct);
                s_TypeRepo["TDT_MyClass1"] = typeof(TypeDictTestTypes.MyClass1<>);

                s_TypeRepo["MDT_Bar"] = typeof(MethodDictionaryTest.Bar<,>);

#if !USC
                s_TypeRepo["BTDT_Foo1"] = typeof(BaseTypeDict.Foo1);
#endif
                s_TypeRepo["BTDT_Foo2"] = typeof(BaseTypeDict.Foo2);
                s_TypeRepo["BTDT_Gen1"] = typeof(BaseTypeDict.Gen1<>);
                s_TypeRepo["BTDT_Gen2"] = typeof(BaseTypeDict.Gen2<>);
                s_TypeRepo["BTDT_MyClass1"] = typeof(BaseTypeDict.MyClass1);
                s_TypeRepo["BTDT_MyClass2"] = typeof(BaseTypeDict.MyClass2<>);
                s_TypeRepo["BTDT_MyClass3"] = typeof(BaseTypeDict.MyClass3<>);
                s_TypeRepo["BTDT_MyClass4"] = typeof(BaseTypeDict.MyClass4<>);
                s_TypeRepo["BTDT_MyClass4_2"] = typeof(BaseTypeDict.MyClass4_2<>);
                s_TypeRepo["BTDT_MyClass4_3"] = typeof(BaseTypeDict.MyClass4_3<>);
                s_TypeRepo["BTDT_GenBase2"] = typeof(BaseTypeDict.GenBase2<>);
                s_TypeRepo["BTDT_GenDerived2"] = typeof(BaseTypeDict.GenDerived2<,>);

                s_TypeRepo["DDT_Yahoo"] = typeof(DictDependency.Yahoo<,>);

                s_TypeRepo["CDT_CtorTest"] = typeof(CtorDict.CtorTest<,>);
                s_TypeRepo["CDT_SelfCtorTest"] = typeof(CtorDict.SelfCtorTest<,>);
                s_TypeRepo["CDT_NoDefaultCtorTest"] = typeof(CtorDict.NoDefaultCtorTest<,>);
                s_TypeRepo["CDT_MyType1"] = typeof(CtorDict.MyType1);
                s_TypeRepo["CDT_MyType2"] = typeof(CtorDict.MyType2);
                s_TypeRepo["CDT_MyType3"] = typeof(CtorDict.MyType3);
                s_TypeRepo["CDT_MyType4"] = typeof(CtorDict.MyType4<>);
                s_TypeRepo["CDT_MyType5"] = typeof(CtorDict.MyType5);
                s_TypeRepo["CDT_MyType6"] = typeof(CtorDict.MyType6);
                s_TypeRepo["CDT_MyType7"] = typeof(CtorDict.MyType7);
                s_TypeRepo["CDT_MyType8"] = typeof(CtorDict.MyType8);

                s_TypeRepo["MUST_GenericClass"] = typeof(MethodAndUnboxingStubTesting.GenericClass<,>);
                s_TypeRepo["MUST_GenericClass2"] = typeof(MethodAndUnboxingStubTesting.GenericClass2<,>);
                s_TypeRepo["MUST_GenericStruct"] = typeof(MethodAndUnboxingStubTesting.GenericStruct<,>);
                s_TypeRepo["MUST_GenericStruct2"] = typeof(MethodAndUnboxingStubTesting.GenericStruct2<,>);

                s_TypeRepo["EIT_Gen2"] = typeof(ExistingInstantiations.Gen2<,>);
                s_TypeRepo["EIT_MyClass1"] = typeof(ExistingInstantiations.MyClass1);
                s_TypeRepo["EIT_MyClass2"] = typeof(ExistingInstantiations.MyClass2);
                s_TypeRepo["EIT_MyClass3"] = typeof(ExistingInstantiations.MyClass3);
                s_TypeRepo["EIT_MyClass4"] = typeof(ExistingInstantiations.MyClass4);
            }

#if UNIVERSAL_GENERICS
            // Types in universal_generics.cs
            {
                s_TypeRepo["UG_MyGen"] = typeof(UniversalGen.MyGen<>);
                s_TypeRepo["UG_MyGenStruct"] = typeof(UniversalGen.MyGenStruct<>);
                s_TypeRepo["UG_MyListItem"] = typeof(UniversalGen.MyListItem);
                s_TypeRepo["UG_UCGSamples"] = typeof(UniversalGen.UCGSamples<,>);
                s_TypeRepo["UG_UnmanagedByRef"] = typeof(UniversalGen.UnmanagedByRef<>);
                s_TypeRepo["UG_InterlockedClass"] = typeof(UniversalGen.InterlockedClass<,>);
                s_TypeRepo["UG_UCGInstanceFields"] = typeof(UniversalGen.UCGInstanceFields<,>);
                s_TypeRepo["UG_UCGInstanceFieldsDerived"] = typeof(UniversalGen.UCGInstanceFieldsDerived<,>);
                s_TypeRepo["UG_UCGInstanceFieldsMostDerived"] = typeof(UniversalGen.UCGInstanceFieldsMostDerived<>);
                s_TypeRepo["UG_UCGStaticFields"] = typeof(UniversalGen.UCGStaticFields<,>);
                s_TypeRepo["UG_UCGThreadStaticFields"] = typeof(UniversalGen.UCGThreadStaticFields<,>);
                s_TypeRepo["UG_UCGClassConstructorType"] = typeof(UniversalGen.UCGClassConstructorType<>);
                s_TypeRepo["UG_UCGWrapperStruct"] = typeof(UniversalGen.UCGWrapperStruct);
                s_TypeRepo["UG_UCGStaticFieldsLayoutCompatDynamic"] = typeof(UniversalGen.UCGStaticFieldsLayoutCompatDynamic<,>);
                s_TypeRepo["UG_UCGGenDerivedTypeActivator"] = typeof(UniversalGen.GenDerivedType_Activator<>);
                s_TypeRepo["UG_GVMTestClass"] = typeof(UniversalGen.GVMTestClass<>);
                s_TypeRepo["UG_GVMTestStruct"] = typeof(UniversalGen.GVMTestStruct<>);
                s_TypeRepo["UG_MakeGVMCall"] = typeof(UniversalGen.MakeGVMCall<>);
                s_TypeRepo["CCT_CCTester"] = typeof(CallingConvention.CCTester<>);
                s_TypeRepo["CCT_UCGTestUseInterface"] = typeof(CallingConvention.UCGTestUseInterface<>);
                s_TypeRepo["CCT_UCGTestNonVirtualFunctionCallUse"] = typeof(CallingConvention.UCGTestNonVirtualFunctionCallUse<>);
                s_TypeRepo["VCT_UCGTestVirtualCalls"] = typeof(VirtualCalls.UCGTestVirtualCalls<>);
                s_TypeRepo["VCT_Derived_NoOverride"] = typeof(VirtualCalls.Derived_NoOverride<>);
                s_TypeRepo["VCT_Derived_WithOverride"] = typeof(VirtualCalls.Derived_WithOverride<>);
                s_TypeRepo["PCT_UCGTestVirtualCalls"] = typeof(PartialUSC.UCGTestVirtualCalls<,>);
                s_TypeRepo["PCT_NullableCaseTest"] = typeof(PartialUSC.NullableCaseTest<>);
                s_TypeRepo["PCT_ArrayCaseTest"] = typeof(PartialUSC.ArrayCaseTest<>);
                s_TypeRepo["PCT_Derived"] = typeof(PartialUSC.Derived<,>);
                s_TypeRepo["PCT_Derived2"] = typeof(PartialUSC.Derived2<,>);
                s_TypeRepo["PCT_Derived3"] = typeof(PartialUSC.Derived3<,>);
                s_TypeRepo["DI_TestType"] = typeof(DynamicInvoke.TestType<,,>);
                s_TypeRepo["ACI_ACI_Instantiator"] = typeof(ActivatorCreateInstance.ACI_Instantiator<,>);
                s_TypeRepo["ACI_NEW_Instantiator"] = typeof(ActivatorCreateInstance.NEW_Instantiator<,>);
                s_TypeRepo["ACI_GenReferenceType"] = typeof(ActivatorCreateInstance.GenReferenceType<>);
                s_TypeRepo["ACI_GenReferenceTypeNoDefaultCtor"] = typeof(ActivatorCreateInstance.GenReferenceTypeNoDefaultCtor<>);
                s_TypeRepo["ACI_AGenValueType"] = typeof(ActivatorCreateInstance.AGenValueType<>);
                s_TypeRepo["OnlyUseViaReflection"] = typeof(Heuristics.OnlyUseViaReflection<>);
                s_TypeRepo["OnlyUseViaReflectionGenMethod"] = typeof(Heuristics.OnlyUseViaReflectionGenMethod);
                s_TypeRepo["AVT_GenType"] = typeof(ArrayVarianceTest.GenType<,>);
                s_TypeRepo["HFA_TestClass"] = typeof(HFATest.TestClass<>);
                s_TypeRepo["HFA_GenStructWrapper"] = typeof(HFATest.GenStructWrapper<>);
            }

            // Types in UniversalConstrainedCalls.cs
            {
                s_TypeRepo["UCC_UCGConstrainedCall"] = typeof(UnivConstCalls.UCGConstrainedCall<,,>);
                s_TypeRepo["UCC_UCGReferenceConstrainedCall"] = typeof(UnivConstCalls.UCGReferenceConstrainedCall<,>);
                s_TypeRepo["UCC_ReferenceConstrainedCallType"] = typeof(UnivConstCalls.ReferenceConstrainedCallType);
                s_TypeRepo["UCC_NonGenericStructThatImplementsInterface"] = typeof(UnivConstCalls.NonGenericStructThatImplementsInterface);
                s_TypeRepo["UCC_NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs"] = typeof(UnivConstCalls.NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs);
                s_TypeRepo["UCC_GenericStructThatImplementsInterface"] = typeof(UnivConstCalls.GenericStructThatImplementsInterface<>);
            }
#endif
            // Types in B282745.cs
            {
                s_TypeRepo["B282475_MDArrayTestType"] = typeof(B282745.MDArrayTestType);
            }
#if UNIVERSAL_GENERICS
            // Types in fieldlayout.cs
            {
                s_TypeRepo["FieldLayout_StructTypeNotUsedAsANullable"] = typeof(FieldLayoutTests.StructTypeNotUsedAsANullable);
            }
#endif
        }

        // Common framework types
        public static Type Object { get { return s_TypeRepo["Object"]; } }
        public static Type String { get { return s_TypeRepo["String"]; } }
        public static Type TempList { get { return s_TypeRepo["TempList"]; } }
        public static Type List { get { return s_TypeRepo["List"]; } }
        public static Type Dictionary { get { return s_TypeRepo["Dictionary"]; } }
        public static Type Func1 { get { return s_TypeRepo["Func1"]; } }
        public static Type Func2 { get { return s_TypeRepo["Func2"]; } }
        public static Type Type { get { return s_TypeRepo["Type"]; } }
        public static Type Double { get { return s_TypeRepo["Double"]; } }
        public static Type Int32 { get { return s_TypeRepo["Int32"]; } }
        public static Type Int16 { get { return s_TypeRepo["Int16"]; } }
        public static Type Bool { get { return s_TypeRepo["Bool"]; } }
        public static Type Short { get { return s_TypeRepo["Short"]; } }
        public static Type Long { get { return s_TypeRepo["Long"]; } }
        public static Type Float { get { return s_TypeRepo["Float"]; } }
        public static Type Char { get { return s_TypeRepo["Char"]; } }

        // Common class/valuetype types
        public static Type Int64Enum { get { return s_TypeRepo["Int64Enum"]; } }

        public static Type CommonType1 { get { return s_TypeRepo["CommonType1"]; } }
        public static Type CommonType2 { get { return s_TypeRepo["CommonType2"]; } }
        public static Type CommonType3 { get { return s_TypeRepo["CommonType3"]; } }
        public static Type CommonType4 { get { return s_TypeRepo["CommonType4"]; } }
        public static Type CommonType5 { get { return s_TypeRepo["CommonType5"]; } }
        public static Type CommonType6 { get { return s_TypeRepo["CommonType6"]; } }
        public static Type CommonType7 { get { return s_TypeRepo["CommonType7"]; } }
        public static Type CommonType8 { get { return s_TypeRepo["CommonType8"]; } }
        public static Type CommonType9 { get { return s_TypeRepo["CommonType9"]; } }
        public static Type CommonType10 { get { return s_TypeRepo["CommonType10"]; } }
        public static Type CommonType11 { get { return s_TypeRepo["CommonType11"]; } }

        // Types in activation.cs
        public static Type My { get { return s_TypeRepo["My"]; } }
        public static Type Foo { get { return s_TypeRepo["Foo"]; } }

        // Types in constraints.cs
        public static Type CT_TypeWithNoConstraint { get { return s_TypeRepo["CT_TypeWithNoConstraint"]; } }
        public static Type CT_TypeWithClassConstraint { get { return s_TypeRepo["CT_TypeWithClassConstraint"]; } }
        public static Type CT_TypeWithNewConstraint { get { return s_TypeRepo["CT_TypeWithNewConstraint"]; } }
        public static Type CT_TypeWithStructConstraint { get { return s_TypeRepo["CT_TypeWithStructConstraint"]; } }
        public static Type CT_TypeRequiringIFoo { get { return s_TypeRepo["CT_TypeRequiringIFoo"]; } }
        public static Type CT_TypeWithSelfReferenceConstraint { get { return s_TypeRepo["CT_TypeWithSelfReferenceConstraint"]; } }
        public static Type CT_TypeWithVariance { get { return s_TypeRepo["CT_TypeWithVariance"]; } }
        public static Type CT_TypeWithRecursiveConstraints { get { return s_TypeRepo["CT_TypeWithRecursiveConstraints"]; } }
        public static Type CT_TypeWithSelfReferenceIEnumerableConstraint { get { return s_TypeRepo["CT_TypeWithSelfReferenceIEnumerableConstraint"]; } }

        // Types in blockedtypes.cs
        public static Type BTT_GenericType { get { return s_TypeRepo["BTT_GenericType"]; } }

        // Types in arrays.cs
        public static Type AT_ArrayTests { get { return s_TypeRepo["AT_ArrayTests"]; } }
        public static Type AT_SomeClassForArrayTests { get { return s_TypeRepo["AT_SomeClassForArrayTests"]; } }
        public static Type AT_SomeClassForArrayTests1 { get { return s_TypeRepo["AT_SomeClassForArrayTests1"]; } }
        public static Type AT_SomeClassForArrayTests2 { get { return s_TypeRepo["AT_SomeClassForArrayTests2"]; } }

        // Types in methodconstraints.cs
        public static Type MCT_TypeWithNoConstraint { get { return s_TypeRepo["MCT_TypeWithNoConstraint"]; } }
        public static Type MCT_TypeWithClassConstraint { get { return s_TypeRepo["MCT_TypeWithClassConstraint"]; } }
        public static Type MCT_TypeWithNewConstraint { get { return s_TypeRepo["MCT_TypeWithNewConstraint"]; } }
        public static Type MCT_TypeWithStructConstraint { get { return s_TypeRepo["MCT_TypeWithStructConstraint"]; } }
        public static Type MCT_TypeRequiringIFoo { get { return s_TypeRepo["MCT_TypeRequiringIFoo"]; } }
        public static Type MCT_TypeWithSelfReferenceConstraint { get { return s_TypeRepo["MCT_TypeWithSelfReferenceConstraint"]; } }
        public static Type MCT_TypeWithVariance { get { return s_TypeRepo["MCT_TypeWithVariance"]; } }
        public static Type MCT_TypeWithRecursiveConstraints { get { return s_TypeRepo["MCT_TypeWithRecursiveConstraints"]; } }
        public static Type MCT_TypeWithSelfReferenceIEnumerableConstraint { get { return s_TypeRepo["MCT_TypeWithSelfReferenceIEnumerableConstraint"]; } }
        public static Type MCT_TypeWithMDArrayConstraints { get { return s_TypeRepo["MCT_TypeWithMDArrayConstraints"]; } }
        public static Type MCT_GenericType { get { return s_TypeRepo["MCT_GenericType"]; } }

        // Types in threadstatics.cs
        public static Type TLS_MyType1 { get { return s_TypeRepo["TLS_MyType1"]; } }
        public static Type TLS_MyDerived1 { get { return s_TypeRepo["TLS_MyDerived1"]; } }
        public static Type TLS_MySuperDerived1_1 { get { return s_TypeRepo["TLS_MySuperDerived1_1"]; } }
        public static Type TLS_MySuperDerived1_2 { get { return s_TypeRepo["TLS_MySuperDerived1_2"]; } }
        public static Type TLS_MyType2 { get { return s_TypeRepo["TLS_MyType2"]; } }
        public static Type TLS_MyDerived2_1 { get { return s_TypeRepo["TLS_MyDerived2_1"]; } }
        public static Type TLS_MyDerived2_2 { get { return s_TypeRepo["TLS_MyDerived2_2"]; } }
        public static Type TLS_T1 { get { return s_TypeRepo["TLS_T1"]; } }
        public static Type TLS_T2 { get { return s_TypeRepo["TLS_T2"]; } }
        public static Type TLS_T4 { get { return s_TypeRepo["TLS_T4"]; } }
        public static Type TLS_T5 { get { return s_TypeRepo["TLS_T5"]; } }

        // Types in statics.cs
        public static Type ST_GenericTypeWithStaticFieldOfTypeT { get { return s_TypeRepo["ST_GenericTypeWithStaticFieldOfTypeT"]; } }
        public static Type ST_GenericTypeWithNonGcStaticField { get { return s_TypeRepo["ST_GenericTypeWithNonGcStaticField"]; } }
        public static Type ST_GenericTypeWithMultipleNonGcStaticFields { get { return s_TypeRepo["ST_GenericTypeWithMultipleNonGcStaticFields"]; } }
        public static Type ST_SuperDerivedGeneric { get { return s_TypeRepo["ST_SuperDerivedGeneric"]; } }
        public static Type ST_GenericTypeWithGcStaticField { get { return s_TypeRepo["ST_GenericTypeWithGcStaticField"]; } }
        public static Type ST_GenericTypeWithStaticTimeSpanField { get { return s_TypeRepo["ST_GenericTypeWithStaticTimeSpanField"]; } }
        public static Type ST_ClassWithStaticConstructor { get { return s_TypeRepo["ST_ClassWithStaticConstructor"]; } }
        public static Type ST_AnotherClassWithStaticConstructor { get { return s_TypeRepo["ST_AnotherClassWithStaticConstructor"]; } }

        // Types in rdexperience.cs
        public static Type RDE_Foo { get { return s_TypeRepo["RDE_Foo"]; } }

        // Types in interfaces.cs
        public static Type IT_Gen { get { return s_TypeRepo["IT_Gen"]; } }
        public static Type IT_Recursive { get { return s_TypeRepo["IT_Recursive"]; } }
        public static Type IT_WithInterfaceOverArrayType { get { return s_TypeRepo["IT_WithInterfaceOverArrayType"]; } }
        public static Type IT_DoublyRecursive { get { return s_TypeRepo["IT_DoublyRecursive"]; } }
        public static Type IT_Frobber { get { return s_TypeRepo["IT_Frobber"]; } }
        public static Type IT_IFrobber { get { return s_TypeRepo["IT_IFrobber"]; } }
        public static Type IT_UseFrobber { get { return s_TypeRepo["IT_UseFrobber"]; } }
        public static Type IT_FrobtasticFrobberStruct { get { return s_TypeRepo["IT_FrobtasticFrobberStruct"]; } }
        public static Type IT_AnotherFrobtasticFrobberStruct { get { return s_TypeRepo["IT_AnotherFrobtasticFrobberStruct"]; } }

        // Types in genericmethods.cs
        public static Type GM_Gen { get { return s_TypeRepo["GM_Gen"]; } }
        public static Type GM_NonGenericType { get { return s_TypeRepo["GM_NonGenericType"]; } }
        public static Type GM_GenericType { get { return s_TypeRepo["GM_GenericType"]; } }
        public static Type GM_Foo { get { return s_TypeRepo["GM_Foo"]; } }
        public static Type GM_Bar { get { return s_TypeRepo["GM_Bar"]; } }
        public static Type GM_MakeGenericMethodTest { get { return s_TypeRepo["GM_MakeGenericMethodTest"]; } }

        // Types in fieldreflection.cs
        public static Type FRT_BaseType2 { get { return s_TypeRepo["FRT_BaseType2"]; } }
        public static Type FRT_DerivedTypeWithVariousFields { get { return s_TypeRepo["FRT_DerivedTypeWithVariousFields"]; } }
        public static Type FRT_ReferenceTypeWithVariousFields { get { return s_TypeRepo["FRT_ReferenceTypeWithVariousFields"]; } }
        public static Type FRT_ReferenceTypeWithCCtor { get { return s_TypeRepo["FRT_ReferenceTypeWithCCtor"]; } }
        public static Type FRT_ValueTypeWithVariousFields { get { return s_TypeRepo["FRT_ValueTypeWithVariousFields"]; } }
        public static Type FRT_ValueTypeWithCCtor { get { return s_TypeRepo["FRT_ValueTypeWithCCtor"]; } }

        // Types in expressions.cs
        public static Type E_TestRunner { get { return s_TypeRepo["E_TestRunner"]; } }
        public static Type E_MyType1 { get { return s_TypeRepo["E_MyType1"]; } }
        public static Type E_MyType2 { get { return s_TypeRepo["E_MyType2"]; } }

        // Types in dictionaries.cs
        public static Type D_Gen { get { return s_TypeRepo["D_Gen"]; } }
        public static Type D_GenBase { get { return s_TypeRepo["D_GenBase"]; } }
        public static Type D_Base { get { return s_TypeRepo["D_Base"]; } }
        public static Type D_IFace { get { return s_TypeRepo["D_IFace"]; } }
        public static Type D_IFace3 { get { return s_TypeRepo["D_IFace3"]; } }
        public static Type D_SingleUseArrayOnlyGen { get { return s_TypeRepo["D_SingleUseArrayOnlyGen"]; } }
        public static Type D_GenericStruct { get { return s_TypeRepo["D_GenericStruct"]; } }
        public static Type D_NullableTest { get { return s_TypeRepo["D_NullableTest"]; } }
        public static Type D_DelegateTarget { get { return s_TypeRepo["D_DelegateTarget"]; } }
        public static Type D_DelWithNullable { get { return s_TypeRepo["D_DelWithNullable"]; } }
        public static Type TDT_MyStruct { get { return s_TypeRepo["TDT_MyStruct"]; } }
        public static Type TDT_MyClass1 { get { return s_TypeRepo["TDT_MyClass1"]; } }
        public static Type MDT_Bar { get { return s_TypeRepo["MDT_Bar"]; } }
        public static Type BTDT_Foo1 { get { return s_TypeRepo["BTDT_Foo1"]; } }
        public static Type BTDT_Foo2 { get { return s_TypeRepo["BTDT_Foo2"]; } }
        public static Type BTDT_Gen1 { get { return s_TypeRepo["BTDT_Gen1"]; } }
        public static Type BTDT_Gen2 { get { return s_TypeRepo["BTDT_Gen2"]; } }
        public static Type BTDT_MyClass1 { get { return s_TypeRepo["BTDT_MyClass1"]; } }
        public static Type BTDT_MyClass2 { get { return s_TypeRepo["BTDT_MyClass2"]; } }
        public static Type BTDT_MyClass3 { get { return s_TypeRepo["BTDT_MyClass3"]; } }
        public static Type BTDT_MyClass4 { get { return s_TypeRepo["BTDT_MyClass4"]; } }
        public static Type BTDT_MyClass4_2 { get { return s_TypeRepo["BTDT_MyClass4_2"]; } }
        public static Type BTDT_MyClass4_3 { get { return s_TypeRepo["BTDT_MyClass4_3"]; } }
        public static Type BTDT_GenBase2 { get { return s_TypeRepo["BTDT_GenBase2"]; } }
        public static Type BTDT_GenDerived2 { get { return s_TypeRepo["BTDT_GenDerived2"]; } }
        public static Type DDT_Yahoo { get { return s_TypeRepo["DDT_Yahoo"]; } }
        public static Type CDT_CtorTest { get { return s_TypeRepo["CDT_CtorTest"]; } }
        public static Type CDT_SelfCtorTest { get { return s_TypeRepo["CDT_SelfCtorTest"]; } }
        public static Type CDT_NoDefaultCtorTest { get { return s_TypeRepo["CDT_NoDefaultCtorTest"]; } }
        public static Type CDT_MyType1 { get { return s_TypeRepo["CDT_MyType1"]; } }
        public static Type CDT_MyType2 { get { return s_TypeRepo["CDT_MyType2"]; } }
        public static Type CDT_MyType3 { get { return s_TypeRepo["CDT_MyType3"]; } }
        public static Type CDT_MyType4 { get { return s_TypeRepo["CDT_MyType4"]; } }
        public static Type CDT_MyType5 { get { return s_TypeRepo["CDT_MyType5"]; } }
        public static Type CDT_MyType6 { get { return s_TypeRepo["CDT_MyType6"]; } }
        public static Type CDT_MyType7 { get { return s_TypeRepo["CDT_MyType7"]; } }
        public static Type CDT_MyType8 { get { return s_TypeRepo["CDT_MyType8"]; } }
        public static Type MUST_GenericClass { get { return s_TypeRepo["MUST_GenericClass"]; } }
        public static Type MUST_GenericClass2 { get { return s_TypeRepo["MUST_GenericClass2"]; } }
        public static Type MUST_GenericStruct { get { return s_TypeRepo["MUST_GenericStruct"]; } }
        public static Type MUST_GenericStruct2 { get { return s_TypeRepo["MUST_GenericStruct2"]; } }
        public static Type EIT_Gen2 { get { return s_TypeRepo["EIT_Gen2"]; } }
        public static Type EIT_MyClass1 { get { return s_TypeRepo["EIT_MyClass1"]; } }
        public static Type EIT_MyClass2 { get { return s_TypeRepo["EIT_MyClass2"]; } }
        public static Type EIT_MyClass3 { get { return s_TypeRepo["EIT_MyClass3"]; } }
        public static Type EIT_MyClass4 { get { return s_TypeRepo["EIT_MyClass4"]; } }

        // Types in universal_generics.cs
        public static Type UG_MyGen { get { return s_TypeRepo["UG_MyGen"]; } }
        public static Type UG_MyGenStruct { get { return s_TypeRepo["UG_MyGenStruct"]; } }
        public static Type UG_MyListItem { get { return s_TypeRepo["UG_MyListItem"]; } }
        public static Type UG_UCGSamples { get { return s_TypeRepo["UG_UCGSamples"]; } }
        public static Type UG_UnmanagedByRef { get { return s_TypeRepo["UG_UnmanagedByRef"]; } }
        public static Type UG_InterlockedClass { get { return s_TypeRepo["UG_InterlockedClass"]; } }
        public static Type UG_UCGInstanceFields { get { return s_TypeRepo["UG_UCGInstanceFields"]; } }
        public static Type UG_UCGInstanceFieldsDerived { get { return s_TypeRepo["UG_UCGInstanceFieldsDerived"]; } }
        public static Type UG_UCGInstanceFieldsMostDerived { get { return s_TypeRepo["UG_UCGInstanceFieldsMostDerived"]; } }
        public static Type UG_UCGStaticFields { get { return s_TypeRepo["UG_UCGStaticFields"]; } }
        public static Type UG_UCGThreadStaticFields { get { return s_TypeRepo["UG_UCGThreadStaticFields"]; } }
        public static Type UG_UCGClassConstructorType { get { return s_TypeRepo["UG_UCGClassConstructorType"]; } }
        public static Type UG_UCGWrapperStruct { get { return s_TypeRepo["UG_UCGWrapperStruct"]; } }
        public static Type UG_UCGStaticFieldsLayoutCompatDynamic { get { return s_TypeRepo["UG_UCGStaticFieldsLayoutCompatDynamic"]; } }
        public static Type UG_UCGGenDerivedTypeActivator { get { return s_TypeRepo["UG_UCGGenDerivedTypeActivator"]; } }
        public static Type UG_GVMTestClass { get { return s_TypeRepo["UG_GVMTestClass"]; } }
        public static Type UG_GVMTestStruct { get { return s_TypeRepo["UG_GVMTestStruct"]; } }
        public static Type UG_MakeGVMCall { get { return s_TypeRepo["UG_MakeGVMCall"]; } }
        public static Type CCT_CCTester { get { return s_TypeRepo["CCT_CCTester"]; } }
        public static Type CCT_UCGTestUseInterface { get { return s_TypeRepo["CCT_UCGTestUseInterface"]; } }
        public static Type CCT_UCGTestNonVirtualFunctionCallUse { get { return s_TypeRepo["CCT_UCGTestNonVirtualFunctionCallUse"]; } }
        public static Type VCT_UCGTestVirtualCalls { get { return s_TypeRepo["VCT_UCGTestVirtualCalls"]; } }
        public static Type VCT_Derived_NoOverride { get { return s_TypeRepo["VCT_Derived_NoOverride"]; } }
        public static Type VCT_Derived_WithOverride { get { return s_TypeRepo["VCT_Derived_WithOverride"]; } }
        public static Type PCT_UCGTestVirtualCalls { get { return s_TypeRepo["PCT_UCGTestVirtualCalls"]; } }
        public static Type PCT_NullableCaseTest { get { return s_TypeRepo["PCT_NullableCaseTest"]; } }
        public static Type PCT_ArrayCaseTest { get { return s_TypeRepo["PCT_ArrayCaseTest"]; } }
        public static Type PCT_Derived { get { return s_TypeRepo["PCT_Derived"]; } }
        public static Type PCT_Derived2 { get { return s_TypeRepo["PCT_Derived2"]; } }
        public static Type PCT_Derived3 { get { return s_TypeRepo["PCT_Derived3"]; } }
        public static Type DI_TestType { get { return s_TypeRepo["DI_TestType"]; } }
        public static Type ACI_ACI_Instantiator { get { return s_TypeRepo["ACI_ACI_Instantiator"]; } }
        public static Type ACI_NEW_Instantiator { get { return s_TypeRepo["ACI_NEW_Instantiator"]; } }
        public static Type ACI_GenReferenceType { get { return s_TypeRepo["ACI_GenReferenceType"]; } }
        public static Type ACI_GenReferenceTypeNoDefaultCtor { get { return s_TypeRepo["ACI_GenReferenceTypeNoDefaultCtor"]; } }
        public static Type ACI_AGenValueType { get { return s_TypeRepo["ACI_AGenValueType"]; } }
        public static Type OnlyUseViaReflection { get { return s_TypeRepo["OnlyUseViaReflection"]; } }
        public static Type OnlyUseViaReflectionGenMethod { get { return s_TypeRepo["OnlyUseViaReflectionGenMethod"]; } }
        public static Type AVT_GenType { get { return s_TypeRepo["AVT_GenType"]; } }
        public static Type HFA_TestClass { get { return s_TypeRepo["HFA_TestClass"]; } }
        public static Type HFA_GenStructWrapper { get { return s_TypeRepo["HFA_GenStructWrapper"]; } }

        // Types in UniversalConstrainedCalls.cs
        public static Type UCC_UCGConstrainedCall { get { return s_TypeRepo["UCC_UCGConstrainedCall"]; } }
        public static Type UCC_UCGReferenceConstrainedCall { get { return s_TypeRepo["UCC_UCGReferenceConstrainedCall"]; } }
        public static Type UCC_ReferenceConstrainedCallType { get { return s_TypeRepo["UCC_ReferenceConstrainedCallType"]; } }
        public static Type UCC_NonGenericStructThatImplementsInterface { get { return s_TypeRepo["UCC_NonGenericStructThatImplementsInterface"]; } }
        public static Type UCC_NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs { get { return s_TypeRepo["UCC_NonGenericStructThatImplementsInterfaceAndOverridesObjectFuncs"]; } }
        public static Type UCC_GenericStructThatImplementsInterface { get { return s_TypeRepo["UCC_GenericStructThatImplementsInterface"]; } }

        // Types in B282745.cs
        public static Type B282475_MDArrayTestType { get { return s_TypeRepo["B282475_MDArrayTestType"]; } }

        // Types in fieldlayout.cs
        public static Type FieldLayout_StructTypeNotUsedAsANullable { get { return s_TypeRepo["FieldLayout_StructTypeNotUsedAsANullable"]; } }
    }
}
