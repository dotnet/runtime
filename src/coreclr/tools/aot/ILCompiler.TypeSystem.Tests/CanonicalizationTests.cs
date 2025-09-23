﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class CanonicalizationTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        private MetadataType _referenceType;
        private MetadataType _otherReferenceType;
        private MetadataType _structType;
        private MetadataType _otherStructType;
        private MetadataType _genericReferenceType;
        private MetadataType _genericStructType;
        private MetadataType _genericReferenceTypeWithThreeParams;
        private MetadataType _genericStructTypeWithThreeParams;

        public CanonicalizationTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.Unknown);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;

            _referenceType = _testModule.GetType("Canonicalization"u8, "ReferenceType"u8);
            _otherReferenceType = _testModule.GetType("Canonicalization"u8, "OtherReferenceType"u8);
            _structType = _testModule.GetType("Canonicalization"u8, "StructType"u8);
            _otherStructType = _testModule.GetType("Canonicalization"u8, "OtherStructType"u8);
            _genericReferenceType = _testModule.GetType("Canonicalization"u8, "GenericReferenceType`1"u8);
            _genericStructType = _testModule.GetType("Canonicalization"u8, "GenericStructType`1"u8);
            _genericReferenceTypeWithThreeParams = _testModule.GetType("Canonicalization"u8, "GenericReferenceTypeWithThreeParams`3"u8);
            _genericStructTypeWithThreeParams = _testModule.GetType("Canonicalization"u8, "GenericStructTypeWithThreeParams`3"u8);
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestGenericTypes(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;

            // Canonical forms of reference type over two different reference types are equivalent
            var referenceOverReference = _genericReferenceType.MakeInstantiatedType(_referenceType);
            var referenceOverOtherReference = _genericReferenceType.MakeInstantiatedType(_otherReferenceType);
            Assert.Same(
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Specific),
                referenceOverOtherReference.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Universal),
                referenceOverOtherReference.ConvertToCanonForm(CanonicalFormKind.Universal));

            var referenceOverReferenceOverReference = _genericReferenceType.MakeInstantiatedType(referenceOverReference);
            Assert.Same(
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Specific),
                referenceOverReferenceOverReference.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Universal),
                referenceOverReferenceOverReference.ConvertToCanonForm(CanonicalFormKind.Universal));

            var threeParamReferenceOverS1R1S1 = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _referenceType, _structType);
            var threeParamReferenceOverS1R2S1 = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _otherReferenceType, _structType);
            var threeParamReferenceOverS1R2S2 = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _otherReferenceType, _otherStructType);
            Assert.Same(
                threeParamReferenceOverS1R1S1.ConvertToCanonForm(CanonicalFormKind.Specific),
                threeParamReferenceOverS1R2S1.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                threeParamReferenceOverS1R1S1.ConvertToCanonForm(CanonicalFormKind.Universal),
                threeParamReferenceOverS1R2S1.ConvertToCanonForm(CanonicalFormKind.Universal));
            Assert.Same(
                threeParamReferenceOverS1R1S1.ConvertToCanonForm(CanonicalFormKind.Universal),
                threeParamReferenceOverS1R2S2.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Universal canonical forms of reference type over reference and value types are equivalent
            var referenceOverStruct = _genericReferenceType.MakeInstantiatedType(_structType);
            var referenceOverOtherStruct = _genericReferenceType.MakeInstantiatedType(_otherStructType);
            Assert.Same(
                referenceOverStruct.ConvertToCanonForm(CanonicalFormKind.Universal),
                referenceOverOtherStruct.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Canon forms of reference type instantiated over a generic valuetype over any reference type
            var genericStructOverReference = _genericStructType.MakeInstantiatedType(_referenceType);
            var genericStructOverOtherReference = _genericStructType.MakeInstantiatedType(_otherReferenceType);
            var referenceOverGenericStructOverReference = _genericReferenceType.MakeInstantiatedType(genericStructOverReference);
            var referenceOverGenericStructOverOtherReference = _genericReferenceType.MakeInstantiatedType(genericStructOverOtherReference);
            Assert.Same(
                referenceOverGenericStructOverReference.ConvertToCanonForm(CanonicalFormKind.Specific),
                referenceOverGenericStructOverOtherReference.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.NotSame(
                referenceOverGenericStructOverReference.ConvertToCanonForm(CanonicalFormKind.Specific),
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverGenericStructOverReference.ConvertToCanonForm(CanonicalFormKind.Universal),
                referenceOverGenericStructOverOtherReference.ConvertToCanonForm(CanonicalFormKind.Universal));
            Assert.Same(
                referenceOverGenericStructOverReference.ConvertToCanonForm(CanonicalFormKind.Universal),
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Canon of a type instantiated over a signature variable is the same type when just canonicalizing as specific,
            // but the universal canon form when performing universal canonicalization.
            var genericStructOverSignatureVariable = _genericStructType.MakeInstantiatedType(_context.GetSignatureVariable(0, false));
            Assert.Same(
                genericStructOverSignatureVariable,
                genericStructOverSignatureVariable.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.NotSame(
                genericStructOverSignatureVariable,
                genericStructOverSignatureVariable.ConvertToCanonForm(CanonicalFormKind.Universal));
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestGenericTypesNegative(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;

            // Two different types instantiated over the same type are not canonically equivalent
            var referenceOverReference = _genericReferenceType.MakeInstantiatedType(_referenceType);
            var structOverReference = _genericStructType.MakeInstantiatedType(_referenceType);
            Assert.NotSame(
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Specific),
                structOverReference.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.NotSame(
                referenceOverReference.ConvertToCanonForm(CanonicalFormKind.Universal),
                structOverReference.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Specific canonical forms of reference type over reference and value types are not equivalent
            var referenceOverStruct = _genericReferenceType.MakeInstantiatedType(_structType);
            var referenceOverOtherStruct = _genericReferenceType.MakeInstantiatedType(_otherStructType);
            Assert.NotSame(
                referenceOverStruct.ConvertToCanonForm(CanonicalFormKind.Specific),
                referenceOverOtherStruct.ConvertToCanonForm(CanonicalFormKind.Specific));

            var threeParamReferenceOverS1R2S1 = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _otherReferenceType, _structType);
            var threeParamReferenceOverS1R2S2 = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _otherReferenceType, _otherStructType);
            Assert.NotSame(
                threeParamReferenceOverS1R2S1.ConvertToCanonForm(CanonicalFormKind.Specific),
                threeParamReferenceOverS1R2S2.ConvertToCanonForm(CanonicalFormKind.Specific));
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestArrayTypes(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;

            // Generic type instantiated over an array has the same canonical form as generic type over any other reference type
            var genericStructOverArrayOfInt = _genericStructType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32).MakeArrayType());
            var genericStructOverReferenceType = _genericStructType.MakeInstantiatedType(_referenceType);
            Assert.Same(
                genericStructOverArrayOfInt.ConvertToCanonForm(CanonicalFormKind.Specific),
                genericStructOverReferenceType.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                genericStructOverArrayOfInt.ConvertToCanonForm(CanonicalFormKind.Universal),
                genericStructOverReferenceType.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Canonical form of SzArray and Multidim array are not the same
            var arrayOfReferenceType = _referenceType.MakeArrayType();
            var mdArrayOfReferenceType = _referenceType.MakeArrayType(1);
            Assert.NotSame(
                arrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Specific),
                mdArrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.NotSame(
                arrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Universal),
                mdArrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Canonical forms of arrays over different reference types are same
            var arrayOfOtherReferenceType = _otherReferenceType.MakeArrayType();
            Assert.Same(
                arrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Specific),
                arrayOfOtherReferenceType.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                arrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Universal),
                arrayOfOtherReferenceType.ConvertToCanonForm(CanonicalFormKind.Universal));

            // Canonical forms of arrays of value types are only same for universal canon form
            var arrayOfStruct = _structType.MakeArrayType();
            Assert.NotSame(
                arrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Specific),
                arrayOfStruct.ConvertToCanonForm(CanonicalFormKind.Specific));
            Assert.Same(
                arrayOfReferenceType.ConvertToCanonForm(CanonicalFormKind.Universal),
                arrayOfStruct.ConvertToCanonForm(CanonicalFormKind.Universal));
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestMethodsOnGenericTypes(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;

            var referenceOverReference = _genericReferenceType.MakeInstantiatedType(_referenceType);
            var referenceOverOtherReference = _genericReferenceType.MakeInstantiatedType(_otherReferenceType);
            Assert.NotSame(
                referenceOverReference.GetMethod("Method"u8, null),
                referenceOverOtherReference.GetMethod("Method"u8, null));
            Assert.Same(
                referenceOverReference.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific),
                referenceOverOtherReference.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverReference.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal),
                referenceOverOtherReference.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal));

            var referenceOverStruct = _genericReferenceType.MakeInstantiatedType(_structType);
            Assert.NotSame(
                referenceOverReference.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific),
                referenceOverStruct.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverReference.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal),
                referenceOverStruct.GetMethod("Method"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal));

            Assert.Same(
                referenceOverReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_referenceType).GetCanonMethodTarget(CanonicalFormKind.Specific),
                referenceOverOtherReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_otherReferenceType).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_referenceType).GetCanonMethodTarget(CanonicalFormKind.Universal),
                referenceOverOtherReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_otherReferenceType).GetCanonMethodTarget(CanonicalFormKind.Universal));

            Assert.NotSame(
                referenceOverReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_referenceType).GetCanonMethodTarget(CanonicalFormKind.Specific),
                referenceOverOtherReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_structType).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_referenceType).GetCanonMethodTarget(CanonicalFormKind.Universal),
                referenceOverOtherReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_structType).GetCanonMethodTarget(CanonicalFormKind.Universal));

            Assert.NotSame(
                referenceOverStruct.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_referenceType).GetCanonMethodTarget(CanonicalFormKind.Specific),
                referenceOverOtherReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_structType).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                referenceOverStruct.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_referenceType).GetCanonMethodTarget(CanonicalFormKind.Universal),
                referenceOverOtherReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_structType).GetCanonMethodTarget(CanonicalFormKind.Universal));

            Assert.NotSame(
                referenceOverReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_structType),
                referenceOverReference.GetMethod("GenericMethod"u8, null).MakeInstantiatedMethod(_structType).GetCanonMethodTarget(CanonicalFormKind.Specific));
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestArrayMethods(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;

            var arrayOfReferenceType = _referenceType.MakeArrayType(1);
            var arrayOfOtherReferenceType = _otherReferenceType.MakeArrayType(1);

            Assert.Same(
                arrayOfReferenceType.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific),
                arrayOfOtherReferenceType.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                arrayOfReferenceType.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal),
                arrayOfOtherReferenceType.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal));

            var arrayOfStruct = _structType.MakeArrayType(1);

            Assert.NotSame(
                arrayOfReferenceType.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific),
                arrayOfStruct.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Specific));
            Assert.Same(
                arrayOfReferenceType.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal),
                arrayOfStruct.GetMethod("Set"u8, null).GetCanonMethodTarget(CanonicalFormKind.Universal));
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestUpgradeToUniversalCanon(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;

            var gstOverUniversalCanon = _genericStructType.MakeInstantiatedType(_context.UniversalCanonType);
            var grtOverRtRtStOverUniversal = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _referenceType, _referenceType, gstOverUniversalCanon);
            var grtOverRtRtStOverUniversalCanon = grtOverRtRtStOverUniversal.ConvertToCanonForm(CanonicalFormKind.Specific);

            // Specific form gets upgraded to universal in the presence of universal canon.
            // GenericReferenceTypeWithThreeParams<ReferenceType, ReferenceType, GenericStructType<__UniversalCanon>> is
            // GenericReferenceTypeWithThreeParams<T__UniversalCanon, U__UniversalCanon, V__UniversalCanon>
            Assert.Same(_context.UniversalCanonType, grtOverRtRtStOverUniversalCanon.Instantiation[0]);
            Assert.Same(_context.UniversalCanonType, grtOverRtRtStOverUniversalCanon.Instantiation[2]);
        }

        [Theory]
        [InlineData(CanonicalizationMode.Standard)]
        [InlineData(CanonicalizationMode.RuntimeDetermined)]
        public void TestDowngradeFromUniversalCanon(CanonicalizationMode algorithmType)
        {
            _context.CanonMode = algorithmType;
            var grtOverUniversalCanon = _genericReferenceType.MakeInstantiatedType(_context.UniversalCanonType);
            var gstOverGrtOverUniversalCanon = _genericStructType.MakeInstantiatedType(grtOverUniversalCanon);
            var gstOverCanon = _genericStructType.MakeInstantiatedType(_context.CanonType);
            Assert.Same(gstOverCanon, gstOverGrtOverUniversalCanon.ConvertToCanonForm(CanonicalFormKind.Specific));

            var gstOverGstOverGrtOverUniversalCanon = _genericStructType.MakeInstantiatedType(gstOverGrtOverUniversalCanon);
            var gstOverGstOverCanon = _genericStructType.MakeInstantiatedType(gstOverCanon);
            Assert.Same(gstOverGstOverCanon, gstOverGstOverGrtOverUniversalCanon.ConvertToCanonForm(CanonicalFormKind.Specific));
        }

        [Fact]
        public void TestCanonicalizationOfRuntimeDeterminedUniversalGeneric()
        {
            var gstOverUniversalCanon = _genericStructType.MakeInstantiatedType(_context.UniversalCanonType);
            var rdtUniversalCanon = (RuntimeDeterminedType)gstOverUniversalCanon.ConvertToSharedRuntimeDeterminedForm().Instantiation[0];
            Assert.Same(_context.UniversalCanonType, rdtUniversalCanon.CanonicalType);

            var gstOverRdtUniversalCanon = _genericStructType.MakeInstantiatedType(rdtUniversalCanon);
            Assert.Same(gstOverUniversalCanon, gstOverRdtUniversalCanon.ConvertToCanonForm(CanonicalFormKind.Specific));
        }
    }
}
