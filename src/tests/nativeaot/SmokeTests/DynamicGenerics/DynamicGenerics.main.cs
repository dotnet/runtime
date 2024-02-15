using Xunit;

public class EntryPointMain
{
    [Fact]
    public static int TestEntryPoint()
    {
        CoreFXTestLibrary.Internal.TestInfo[] tests = new CoreFXTestLibrary.Internal.TestInfo[]
        {
            new CoreFXTestLibrary.Internal.TestInfo("My.TestActivatorCreateInstance", () => global::My.TestActivatorCreateInstance(), null),
            new CoreFXTestLibrary.Internal.TestInfo("My.TestDefaultCtorInLazyGenerics", () => global::My.TestDefaultCtorInLazyGenerics(), null),
            new CoreFXTestLibrary.Internal.TestInfo("Expressions.ExpressionsTesting.TestLdTokenResults", () => global::Expressions.ExpressionsTesting.TestLdTokenResults(), null),
            new CoreFXTestLibrary.Internal.TestInfo("Expressions.ExpressionsTesting.TestLdTokenResultsWithStructTypes", () => global::Expressions.ExpressionsTesting.TestLdTokenResultsWithStructTypes(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestInstanceMethod", () => global::MakeGenMethod.Test.TestInstanceMethod(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestStaticMethod", () => global::MakeGenMethod.Test.TestStaticMethod(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestGenericMethodsWithEnumParametersHavingDefaultValues", () => global::MakeGenMethod.Test.TestGenericMethodsWithEnumParametersHavingDefaultValues(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestNoDictionaries", () => global::MakeGenMethod.Test.TestNoDictionaries(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestGenMethodOnGenType", () => global::MakeGenMethod.Test.TestGenMethodOnGenType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestReverseLookups", () => global::MakeGenMethod.Test.TestReverseLookups(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MakeGenMethod.Test.TestReverseLookupsWithArrayArg", () => global::MakeGenMethod.Test.TestReverseLookupsWithArrayArg(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayTests.ArrayTests.TestArrays", () => global::ArrayTests.ArrayTests.TestArrays(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayTests.ArrayTests.TestDynamicArrays", () => global::ArrayTests.ArrayTests.TestDynamicArrays(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayTests.ArrayTests.TestMDArrays", () => global::ArrayTests.ArrayTests.TestMDArrays(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayTests.ArrayTests.TestArrayIndexOfNullableStructOfCanon_USG", () => global::ArrayTests.ArrayTests.TestArrayIndexOfNullableStructOfCanon_USG(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayTests.ArrayTests.TestArrayIndexOfNullableStructOfCanon_Canon", () => global::ArrayTests.ArrayTests.TestArrayIndexOfNullableStructOfCanon_Canon(), null),
            new CoreFXTestLibrary.Internal.TestInfo("BlockedTypesTests.TestBlockedTypes", () => global::BlockedTypesTests.TestBlockedTypes(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ConstraintsTests.TestInvalidInstantiations", () => global::ConstraintsTests.TestInvalidInstantiations(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ConstraintsTests.TestSpecialConstraints", () => global::ConstraintsTests.TestSpecialConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ConstraintsTests.TestTypeConstraints", () => global::ConstraintsTests.TestTypeConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MethodConstraintsTests.TestInvalidInstantiations", () => global::MethodConstraintsTests.TestInvalidInstantiations(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MethodConstraintsTests.TestSpecialConstraints", () => global::MethodConstraintsTests.TestSpecialConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MethodConstraintsTests.TestTypeConstraints", () => global::MethodConstraintsTests.TestTypeConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MethodConstraintsTests.TestMDTypeConstraints", () => global::MethodConstraintsTests.TestMDTypeConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("Dictionaries.DictionariesTest.TestBasicDictionaryEntryTypes", () => global::Dictionaries.DictionariesTest.TestBasicDictionaryEntryTypes(), null),
            new CoreFXTestLibrary.Internal.TestInfo("Dictionaries.DictionariesTest.StaticMethodFolding_Test", () => global::Dictionaries.DictionariesTest.StaticMethodFolding_Test(), null),
            new CoreFXTestLibrary.Internal.TestInfo("Dictionaries.DictionariesTest.NullableTesting", () => global::Dictionaries.DictionariesTest.NullableTesting(), null),
            new CoreFXTestLibrary.Internal.TestInfo("TypeDictTestTypes.DictionariesTest.TestGenericTypeDictionary", () => global::TypeDictTestTypes.DictionariesTest.TestGenericTypeDictionary(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MethodDictionaryTest.DictionariesTest.TestMethodDictionaries", () => global::MethodDictionaryTest.DictionariesTest.TestMethodDictionaries(), null),
            new CoreFXTestLibrary.Internal.TestInfo("BaseTypeDict.Test.TestVirtCallTwoGenParams", () => global::BaseTypeDict.Test.TestVirtCallTwoGenParams(), null),
            new CoreFXTestLibrary.Internal.TestInfo("BaseTypeDict.Test.TestUsingPrimitiveTypes", () => global::BaseTypeDict.Test.TestUsingPrimitiveTypes(), null),
            new CoreFXTestLibrary.Internal.TestInfo("BaseTypeDict.Test.TestBaseTypeDictionaries", () => global::BaseTypeDict.Test.TestBaseTypeDictionaries(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DictDependency.Test.TestIndirectDictionaryDependencies", () => global::DictDependency.Test.TestIndirectDictionaryDependencies(), null),
            new CoreFXTestLibrary.Internal.TestInfo("CtorDict.DictionaryTesting.TestAllocationDictionaryEntryTypes", () => global::CtorDict.DictionaryTesting.TestAllocationDictionaryEntryTypes(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MethodAndUnboxingStubTesting.Test.TestNoConstraints", () => global::MethodAndUnboxingStubTesting.Test.TestNoConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ExistingInstantiations.Test.TestWithExistingInst", () => global::ExistingInstantiations.Test.TestWithExistingInst(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ExistingInstantiations.Test.TestInstantiationsWithExistingArrayTypeArgs", () => global::ExistingInstantiations.Test.TestInstantiationsWithExistingArrayTypeArgs(), null),
            new CoreFXTestLibrary.Internal.TestInfo("TemplateDependencyFromGenArgs.TestRunner.TemplateDependencyFromGenArgsTest", () => global::TemplateDependencyFromGenArgs.TestRunner.TemplateDependencyFromGenArgsTest(), null),
#if UNIVERSAL_GENERICS
            new CoreFXTestLibrary.Internal.TestInfo("FieldLayoutTests.TestFieldLayoutMatchesBetweenStaticAndDynamic_Long", () => global::FieldLayoutTests.TestFieldLayoutMatchesBetweenStaticAndDynamic_Long(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldLayoutTests.TestFieldLayoutMatchesBetweenStaticAndDynamic_Int64Enum", () => global::FieldLayoutTests.TestFieldLayoutMatchesBetweenStaticAndDynamic_Int64Enum(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldLayoutTests.TestBoxingUSGCreatedNullable", () => global::FieldLayoutTests.TestBoxingUSGCreatedNullable(), null),
#endif
            new CoreFXTestLibrary.Internal.TestInfo("FieldReflectionTests.TestInstanceFieldsOnDerivedType", () => global::FieldReflectionTests.TestInstanceFieldsOnDerivedType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldReflectionTests.TestInstanceFields", () => global::FieldReflectionTests.TestInstanceFields(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldReflectionTests.TestStaticFields", () => global::FieldReflectionTests.TestStaticFields(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldReflectionTests.TestInitializedStaticFields", () => global::FieldReflectionTests.TestInitializedStaticFields(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldReflectionTests.TestFieldSetValueOnInstantiationsThatAlreadyExistButAreNotKnownToReflection", () => global::FieldReflectionTests.TestFieldSetValueOnInstantiationsThatAlreadyExistButAreNotKnownToReflection(), null),
            new CoreFXTestLibrary.Internal.TestInfo("InterfacesTests.TestGenericCollapsingInInterfaceMap", () => global::InterfacesTests.TestGenericCollapsingInInterfaceMap(), null),
            new CoreFXTestLibrary.Internal.TestInfo("InterfacesTests.TestImplementedInterfaces", () => global::InterfacesTests.TestImplementedInterfaces(), null),
            new CoreFXTestLibrary.Internal.TestInfo("InterfacesTests.TestBaseType", () => global::InterfacesTests.TestBaseType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("InterfacesTests.TestInterfaceInvoke", () => global::InterfacesTests.TestInterfaceInvoke(), null),
            new CoreFXTestLibrary.Internal.TestInfo("InterfacesTests.TestConstrainedCall", () => global::InterfacesTests.TestConstrainedCall(), null),
#if !MULTIMODULE_BUILD
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestGetRange", () => global::DynamicListTests.TestGetRange(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestAddRange", () => global::DynamicListTests.TestAddRange(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestAddRemove", () => global::DynamicListTests.TestAddRemove(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestIListOfT", () => global::DynamicListTests.TestIListOfT(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestICollectionOfT", () => global::DynamicListTests.TestICollectionOfT(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestIList", () => global::DynamicListTests.TestIList(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestICollection", () => global::DynamicListTests.TestICollection(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestIReadOnlyListOfT", () => global::DynamicListTests.TestIReadOnlyListOfT(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestIReadOnlyCollectionOfT", () => global::DynamicListTests.TestIReadOnlyCollectionOfT(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestToArray", () => global::DynamicListTests.TestToArray(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestContains", () => global::DynamicListTests.TestContains(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestSortWithComparison", () => global::DynamicListTests.TestSortWithComparison(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicListTests.TestSortWithComparer", () => global::DynamicListTests.TestSortWithComparer(), null),
#endif
            //new CoreFXTestLibrary.Internal.TestInfo("RdExperienceTests.TestRdExperience", () => global::RdExperienceTests.TestRdExperience(), null),
            new CoreFXTestLibrary.Internal.TestInfo("StaticsTests.TestStatics", () => global::StaticsTests.TestStatics(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ThreadLocalStatics.TLSTesting.ThreadLocalStatics_Test", () => global::ThreadLocalStatics.TLSTesting.ThreadLocalStatics_Test(), null),
#if UNIVERSAL_GENERICS
            new CoreFXTestLibrary.Internal.TestInfo("UnivConstCalls.Test.TestRefTypeCallsOnNonGenClass", () => global::UnivConstCalls.Test.TestRefTypeCallsOnNonGenClass(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnivConstCalls.Test.TestUSCCallsOnNonGenStruct", () => global::UnivConstCalls.Test.TestUSCCallsOnNonGenStruct(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnivConstCalls.Test.TestUSCCallsOnSharedGenStruct", () => global::UnivConstCalls.Test.TestUSCCallsOnSharedGenStruct(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnivConstCalls.Test.TestUSCCallsOnUSCGenStruct", () => global::UnivConstCalls.Test.TestUSCCallsOnUSCGenStruct(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnivConstCalls.Test.TestUSCNonGenInterfaceCallsOnStructs", () => global::UnivConstCalls.Test.TestUSCNonGenInterfaceCallsOnStructs(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestInterlockedPrimitives", () => global::UniversalGen.Test.TestInterlockedPrimitives(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestArraysAndGC", () => global::UniversalGen.Test.TestArraysAndGC(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSGByRefFunctionCalls", () => global::UniversalGen.Test.TestUSGByRefFunctionCalls(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSGSamples", () => global::UniversalGen.Test.TestUSGSamples(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestMakeGenericType", () => global::UniversalGen.Test.TestMakeGenericType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSCInstanceFieldUsage", () => global::UniversalGen.Test.TestUSCInstanceFieldUsage(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSCStaticFieldUsage", () => global::UniversalGen.Test.TestUSCStaticFieldUsage(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSCThreadStaticFieldUsage", () => global::UniversalGen.Test.TestUSCThreadStaticFieldUsage(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSCStaticFieldLayoutCompat", () => global::UniversalGen.Test.TestUSCStaticFieldLayoutCompat(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSCClassConstructorImplicit", () => global::UniversalGen.Test.TestUSCClassConstructorImplicit(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUSCClassConstructorExplicit", () => global::UniversalGen.Test.TestUSCClassConstructorExplicit(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UniversalGen.Test.TestUniversalGenericsGvmCall", () => global::UniversalGen.Test.TestUniversalGenericsGvmCall(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUSC.Test.TestVirtualCallsPartialUSGVTableMismatch", () => global::PartialUSC.Test.TestVirtualCallsPartialUSGVTableMismatch(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUSC.Test.TestVirtualCalls", () => global::PartialUSC.Test.TestVirtualCalls(), null),
            new CoreFXTestLibrary.Internal.TestInfo("VirtualCalls.Test.TestVirtualCalls", () => global::VirtualCalls.Test.TestVirtualCalls(), null),
            new CoreFXTestLibrary.Internal.TestInfo("CallingConvention.Test.TestInstancesOfKnownAndUnknownSizes", () => global::CallingConvention.Test.TestInstancesOfKnownAndUnknownSizes(), null),
            new CoreFXTestLibrary.Internal.TestInfo("CallingConvention.Test.TestCallInstanceFunction", () => global::CallingConvention.Test.TestCallInstanceFunction(), null),
            new CoreFXTestLibrary.Internal.TestInfo("CallingConvention.Test.TestCallInterface", () => global::CallingConvention.Test.TestCallInterface(), null),
            new CoreFXTestLibrary.Internal.TestInfo("CallingConvention.Test.CallingConventionTest", () => global::CallingConvention.Test.CallingConventionTest(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DynamicInvoke.Test.TestDynamicInvoke", () => global::DynamicInvoke.Test.TestDynamicInvoke(), null),
            new CoreFXTestLibrary.Internal.TestInfo("TypeLayout.Test.TestTypeGCDescs", () => global::TypeLayout.Test.TestTypeGCDescs(), null),
            new CoreFXTestLibrary.Internal.TestInfo("TypeLayout.Test.StructsOfPrimitives", () => global::TypeLayout.Test.StructsOfPrimitives(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ActivatorCreateInstance.Test.TestCreateInstance", () => global::ActivatorCreateInstance.Test.TestCreateInstance(), null),
            new CoreFXTestLibrary.Internal.TestInfo("MultiThreadUSCCall.Test.CallsWithGCCollects", () => global::MultiThreadUSCCall.Test.CallsWithGCCollects(), null),
            new CoreFXTestLibrary.Internal.TestInfo("Heuristics.TestHeuristics.TestReflectionHeuristics", () => global::Heuristics.TestHeuristics.TestReflectionHeuristics(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayVarianceTest.Test.RunTest", () => global::ArrayVarianceTest.Test.RunTest(), null),
            new CoreFXTestLibrary.Internal.TestInfo("IsInstTest.TestRunner.RunIsInstAndCheckCastTest", () => global::IsInstTest.TestRunner.RunIsInstAndCheckCastTest(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DelegateCallTest.TestRunner.TestCallMethodThroughUsgDelegate", () => global::DelegateCallTest.TestRunner.TestCallMethodThroughUsgDelegate(), null),
            new CoreFXTestLibrary.Internal.TestInfo("FieldLayoutBugRepro.Runner.EntryPoint", () => global::FieldLayoutBugRepro.Runner.EntryPoint(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DelegateTest.TestRunner.TestMethodCellsWithUSGTargetsUsedOnNonUSGInstantiations", () => global::DelegateTest.TestRunner.TestMethodCellsWithUSGTargetsUsedOnNonUSGInstantiations(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayExceptionsTest.Runner.ArrayExceptionsTest_String_Object", () => global::ArrayExceptionsTest.Runner.ArrayExceptionsTest_String_Object(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayExceptionsTest.Runner.ArrayExceptionsTest_Int32_Int32", () => global::ArrayExceptionsTest.Runner.ArrayExceptionsTest_Int32_Int32(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayExceptionsTest.Runner.ArrayExceptionsTest_Int32_IntBasedEnum", () => global::ArrayExceptionsTest.Runner.ArrayExceptionsTest_Int32_IntBasedEnum(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayExceptionsTest.Runner.ArrayExceptionsTest_UInt32_Int32", () => global::ArrayExceptionsTest.Runner.ArrayExceptionsTest_UInt32_Int32(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToString", () => global::UnboxAnyTests.Runner.TestUnboxAnyToString(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToInt", () => global::UnboxAnyTests.Runner.TestUnboxAnyToInt(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToIntBasedEnum", () => global::UnboxAnyTests.Runner.TestUnboxAnyToIntBasedEnum(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToNullableInt", () => global::UnboxAnyTests.Runner.TestUnboxAnyToNullableInt(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToNullableIntBasedEnum", () => global::UnboxAnyTests.Runner.TestUnboxAnyToNullableIntBasedEnum(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToShort_NonUSG", () => global::UnboxAnyTests.Runner.TestUnboxAnyToShort_NonUSG(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToShortBasedEnum_NonUSG", () => global::UnboxAnyTests.Runner.TestUnboxAnyToShortBasedEnum_NonUSG(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToNullableShort_NonUSG", () => global::UnboxAnyTests.Runner.TestUnboxAnyToNullableShort_NonUSG(), null),
            new CoreFXTestLibrary.Internal.TestInfo("UnboxAnyTests.Runner.TestUnboxAnyToNullableShortBasedEnum_NonUSG", () => global::UnboxAnyTests.Runner.TestUnboxAnyToNullableShortBasedEnum_NonUSG(), null),
            new CoreFXTestLibrary.Internal.TestInfo("HFATest.Runner.HFATestEntryPoint", () => global::HFATest.Runner.HFATestEntryPoint(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ComparerOfTTests.Runner.TestStructThatImplementsIComparable", () => global::ComparerOfTTests.Runner.TestStructThatImplementsIComparable(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ComparerOfTTests.Runner.TestStructThatImplementsIComparableOfObject", () => global::ComparerOfTTests.Runner.TestStructThatImplementsIComparableOfObject(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ComparerOfTTests.Runner.TestBoringStruct", () => global::ComparerOfTTests.Runner.TestBoringStruct(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DefaultValueDelegateParameterTests.Runner.TestCallUniversalGenericDelegate", () => global::DefaultValueDelegateParameterTests.Runner.TestCallUniversalGenericDelegate(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayOfGenericStructGCTests.Runner.TestArrayOfGenericStructGCTests", () => global::ArrayOfGenericStructGCTests.Runner.TestArrayOfGenericStructGCTests(), null),
            new CoreFXTestLibrary.Internal.TestInfo("ArrayOfGenericStructGCTests.Runner.TestNonPointerSizedFinalField", () => global::ArrayOfGenericStructGCTests.Runner.TestNonPointerSizedFinalField(), null),
            new CoreFXTestLibrary.Internal.TestInfo("DelegatesToStructMethods.Runner.TestDelegateInvokeToMethods", () => global::DelegatesToStructMethods.Runner.TestDelegateInvokeToMethods(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestOverrideMethodOnDerivedTypeWhereInstantiationArgsAreDifferentThanBaseType", () => global::PartialUniversalGen.Test.TestOverrideMethodOnDerivedTypeWhereInstantiationArgsAreDifferentThanBaseType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatDerivesFromBaseInstantiatedOverArray", () => global::PartialUniversalGen.Test.TestUniversalGenericThatDerivesFromBaseInstantiatedOverArray(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGeneric", () => global::PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGeneric(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatImplementsInterfaceOverArrayType", () => global::PartialUniversalGen.Test.TestUniversalGenericThatImplementsInterfaceOverArrayType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericMethod", () => global::PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericMethod(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericMethodWithActivatorCreateInstance", () => global::PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericMethodWithActivatorCreateInstance(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericType", () => global::PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericMethodWithConstraints", () => global::PartialUniversalGen.Test.TestUniversalGenericThatUsesCanonicalGenericMethodWithConstraints(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestDependenciesOfPartialUniversalCanonicalCode", () => global::PartialUniversalGen.Test.TestDependenciesOfPartialUniversalCanonicalCode(), null),
            new CoreFXTestLibrary.Internal.TestInfo("PartialUniversalGen.Test.TestCornerCaseSealedVTableSlot", () => global::PartialUniversalGen.Test.TestCornerCaseSealedVTableSlot(), null),
#endif
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testIntMDArrayWithPointerLikeValues", () => global::B282745.testIntMDArrayWithPointerLikeValues(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testLongMDArrayWithPointerLikeValues", () => global::B282745.testLongMDArrayWithPointerLikeValues(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testMDArrayWithPointerLikeValuesOfKnownStructType", () => global::B282745.testMDArrayWithPointerLikeValuesOfKnownStructType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testMDArrayWithPointerLikeValuesOfKnownStructTypeLargerType", () => global::B282745.testMDArrayWithPointerLikeValuesOfKnownStructTypeLargerType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testMDArrayWithPointerLikeValuesOfUnknownStructTypeWithNonGCValuesAtZeroOffset", () => global::B282745.testMDArrayWithPointerLikeValuesOfUnknownStructTypeWithNonGCValuesAtZeroOffset(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testMDArrayWithPointerLikeValuesOfUnknownStructReferenceType", () => global::B282745.testMDArrayWithPointerLikeValuesOfUnknownStructReferenceType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testMDArrayWithPointerLikeValuesOfUnknownStructPrimitiveType", () => global::B282745.testMDArrayWithPointerLikeValuesOfUnknownStructPrimitiveType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("B282745.testMDArrayWith3Dimensions", () => global::B282745.testMDArrayWith3Dimensions(), null),
#if UNIVERSAL_GENERICS
            new CoreFXTestLibrary.Internal.TestInfo("B279085.TestB279085Repro", () => global::B279085.TestB279085Repro(), null),
#endif
            new CoreFXTestLibrary.Internal.TestInfo("GenericVirtualMethods.TestCalls", () => global::GenericVirtualMethods.TestCalls(), null),
            new CoreFXTestLibrary.Internal.TestInfo("GenericVirtualMethods.TestLdFtnToGetStaticMethodOnGenericType", () => global::GenericVirtualMethods.TestLdFtnToGetStaticMethodOnGenericType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("GenericVirtualMethods.TestLdFtnToInstanceGenericMethod", () => global::GenericVirtualMethods.TestLdFtnToInstanceGenericMethod(), null),
            // https://github.com/dotnet/corert/issues/3460
            //new CoreFXTestLibrary.Internal.TestInfo("GenericVirtualMethods.TestGenericExceptionType", () => global::GenericVirtualMethods.TestGenericExceptionType(), null),
            new CoreFXTestLibrary.Internal.TestInfo("GenericVirtualMethods.TestCoAndContraVariantCalls", () => global::GenericVirtualMethods.TestCoAndContraVariantCalls(), null)
        };

        // This is a placeholder for the RunTests() call below that expects an
        // args array as a parameter. Tests using the Merged Wrapper system rely
        // on the TestEntryPoint(), which receives no parameters, whereas the
        // legacy system had a Main(string[] args) signature. However, in this
        // specific test's case, the args array was always empty.
        string[] args = new string[] { };

        bool passed = CoreFXTestLibrary.Internal.Runner.RunTests(tests, args);
        CoreFXTestLibrary.Logger.LogInformation("Passed: {0}, Failed: {1}, Number of Tests Run: {2}",
                                                CoreFXTestLibrary.Internal.Runner.NumPassedTests,
                                                CoreFXTestLibrary.Internal.Runner.NumFailedTests,
                                                CoreFXTestLibrary.Internal.Runner.NumTests);

        if (passed && CoreFXTestLibrary.Internal.Runner.NumPassedTests > 0)
        {
            CoreFXTestLibrary.Logger.LogInformation("All tests PASSED.");
            return 100;
        }
        else
        {
            CoreFXTestLibrary.Logger.LogInformation("{0} tests FAILED!", CoreFXTestLibrary.Internal.Runner.NumFailedTests);
            return CoreFXTestLibrary.Internal.Runner.NumFailedTests == 100 ? 101 : CoreFXTestLibrary.Internal.Runner.NumFailedTests;
        }
    }
}
