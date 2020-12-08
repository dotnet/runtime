// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class RuntimeDeterminedTypesTests
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

        public RuntimeDeterminedTypesTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.Unknown);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);
            
            _testModule = systemModule;

            _referenceType = _testModule.GetType("Canonicalization", "ReferenceType");
            _otherReferenceType = _testModule.GetType("Canonicalization", "OtherReferenceType");
            _structType = _testModule.GetType("Canonicalization", "StructType");
            _otherStructType = _testModule.GetType("Canonicalization", "OtherStructType");
            _genericReferenceType = _testModule.GetType("Canonicalization", "GenericReferenceType`1");
            _genericStructType = _testModule.GetType("Canonicalization", "GenericStructType`1");
            _genericReferenceTypeWithThreeParams = _testModule.GetType("Canonicalization", "GenericReferenceTypeWithThreeParams`3");
            _genericStructTypeWithThreeParams = _testModule.GetType("Canonicalization", "GenericStructTypeWithThreeParams`3");
        }

        [Fact]
        public void TestReferenceTypeConversionToSharedForm()
        {
            var grtOverRt = _genericReferenceType.MakeInstantiatedType(_referenceType);
            var grtOverOtherRt = _genericReferenceType.MakeInstantiatedType(_otherReferenceType);
            var grtOverRtShared = grtOverRt.ConvertToSharedRuntimeDeterminedForm();
            var grtOverOtherRtShared = grtOverOtherRt.ConvertToSharedRuntimeDeterminedForm();

            // GenericReferenceType<ReferenceType> and GenericReferenceType<OtherReferenceType> have the same shared form
            Assert.Same(grtOverRtShared, grtOverOtherRtShared);

            // The instantiation argument of the shared form is a runtime determined type
            var typeArg = grtOverRtShared.Instantiation[0];
            Assert.IsType<RuntimeDeterminedType>(typeArg);

            // The canonical type used in the shared runtime form is __Canon
            var runtimeDeterminedType = (RuntimeDeterminedType)typeArg;
            Assert.Same(_context.CanonType, runtimeDeterminedType.CanonicalType);

            // The shared runtime form details type is the T from the generic definition
            Assert.Same(_genericReferenceType.Instantiation[0], runtimeDeterminedType.RuntimeDeterminedDetailsType);

            // Canonical form of GenericReferenceType<T__Canon> is same as canonical form of GenericReferenceType<ReferenceType>
            Assert.Same(
                grtOverRtShared.ConvertToCanonForm(CanonicalFormKind.Specific),
                grtOverRt.ConvertToCanonForm(CanonicalFormKind.Specific));

            // GenericReferenceType<ReferenceType> and GenericReferenceType<StructType[]> have the same shared form
            var grtOverArray = _genericReferenceType.MakeInstantiatedType(_structType.MakeArrayType());
            var grtOverArrayShared = grtOverArray.ConvertToSharedRuntimeDeterminedForm();
            Assert.Same(grtOverRtShared, grtOverArrayShared);

            // Converting GenericReferenceType<StructType> to shared form is a no-op
            var grtOverSt = _genericReferenceType.MakeInstantiatedType(_structType);
            var grtOverStShared = grtOverSt.ConvertToSharedRuntimeDeterminedForm();
            Assert.Same(grtOverStShared, grtOverSt);
        }

        [Fact]
        public void TestLargeReferenceTypeConversionToSharedForm()
        {
            var grtOverRtStRt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _referenceType, _structType, _referenceType);
            var grtOverRtStOtherRt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _referenceType, _structType, _otherReferenceType);
            var grtOverRtStRtShared = grtOverRtStRt.ConvertToSharedRuntimeDeterminedForm();
            var grtOverRtStOtherRtShared = grtOverRtStOtherRt.ConvertToSharedRuntimeDeterminedForm();

            // GenericReferenceTypeWithThreeParams<ReferenceType, StructType, ReferenceType>
            // GenericReferenceTypeWithThreeParams<ReferenceType, StructType, OtherReferenceType>
            // have the same shared runtime form.
            Assert.Same(grtOverRtStRtShared, grtOverRtStOtherRtShared);

            var grtOverStRtSt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _referenceType, _structType);
            var grtOverStOtherRtSt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _otherReferenceType, _structType);
            var grtOverStRtStShared = grtOverStRtSt.ConvertToSharedRuntimeDeterminedForm();
            var grtOverStOtherRtStShared = grtOverStOtherRtSt.ConvertToSharedRuntimeDeterminedForm();

            // GenericReferenceTypeWithThreeParams<StructType, ReferenceType, StructType>
            // GenericReferenceTypeWithThreeParams<StructType, OtherReferenceType, StructType>
            // have the same shared runtime form.
            Assert.Same(grtOverStRtStShared, grtOverStOtherRtStShared);

            // GenericReferenceTypeWithThreeParams<StructType, ReferenceType, StructType>
            // GenericReferenceTypeWithThreeParams<StructType, ReferenceType, OtherStructType>
            // have different shared runtime form.
            var grtOverStRtOtherSt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _structType, _referenceType, _otherStructType);
            var grtOverStRtOtherStShared = grtOverStRtOtherSt.ConvertToSharedRuntimeDeterminedForm();
            Assert.NotSame(grtOverStRtStShared, grtOverStRtOtherStShared);
        }

        [Fact]
        public void TestUniversalCanonUpgrade()
        {
            var gstOverUniversalCanon = _genericStructType.MakeInstantiatedType(_context.UniversalCanonType);
            var grtOverRtRtStOverUniversal = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _referenceType, _referenceType, gstOverUniversalCanon);
            var grtOverRtRtStOverUniversalShared = grtOverRtRtStOverUniversal.ConvertToSharedRuntimeDeterminedForm();

            // Shared runtime form of
            // GenericReferenceTypeWithThreeParams<ReferenceType, ReferenceType, GenericStructType<__UniversalCanon>> is
            // GenericReferenceTypeWithThreeParams<T__UniversalCanon, U__UniversalCanon, V__UniversalCanon>
            var arg0 = grtOverRtRtStOverUniversalShared.Instantiation[0];
            Assert.IsType<RuntimeDeterminedType>(arg0);
            Assert.Same(_context.UniversalCanonType, ((RuntimeDeterminedType)arg0).CanonicalType);
            var arg2 = grtOverRtRtStOverUniversalShared.Instantiation[2];
            Assert.IsType<RuntimeDeterminedType>(arg2);
            Assert.Same(_context.UniversalCanonType, ((RuntimeDeterminedType)arg2).CanonicalType);
        }

        [Fact]
        public void TestSignatureInstantiation()
        {
            var grtOverRtStRt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _referenceType, _structType, _referenceType);
            var grtOverRtStRtShared = grtOverRtStRt.ConvertToSharedRuntimeDeterminedForm();

            // GenericReferenceTypeWithThreeParams<T__Canon, StructType, V__Canon> substituted over
            // an instantiation of <ReferenceType, StructType, OtherReferenceType> is 
            // GenericReferenceTypeWithThreeParams<ReferenceType, StructType, OtherReferenceType>
            var grtOverRtStRtSharedInstantiated = grtOverRtStRtShared.GetNonRuntimeDeterminedTypeFromRuntimeDeterminedSubtypeViaSubstitution(
                new Instantiation(_referenceType, _structType, _otherReferenceType),
                Instantiation.Empty);

            var grtOverRtStOtherRt = _genericReferenceTypeWithThreeParams.MakeInstantiatedType(
                _referenceType, _structType, _otherReferenceType);

            Assert.Same(grtOverRtStOtherRt, grtOverRtStRtSharedInstantiated);
        }

        [Fact]
        public void TestInstantiationOverStructOverCanon()
        {
            var stOverCanon = _genericStructType.MakeInstantiatedType(_context.CanonType);
            var grtOverStOverCanon = _genericReferenceType.MakeInstantiatedType(
                stOverCanon);
            var grtOverStOverCanonShared = grtOverStOverCanon.ConvertToSharedRuntimeDeterminedForm();

            // GenericReferenceType<GenericStructType<__Canon>> converts to
            // GenericReferenceType<T__GenericStructType<__Canon>>
            var typeArg = grtOverStOverCanonShared.Instantiation[0];
            Assert.IsType<RuntimeDeterminedType>(typeArg);
            var runtimeDeterminedType = (RuntimeDeterminedType)typeArg;
            Assert.Same(stOverCanon, runtimeDeterminedType.CanonicalType);
            Assert.Same(_genericReferenceType.Instantiation[0], runtimeDeterminedType.RuntimeDeterminedDetailsType);
        }
    }
}
