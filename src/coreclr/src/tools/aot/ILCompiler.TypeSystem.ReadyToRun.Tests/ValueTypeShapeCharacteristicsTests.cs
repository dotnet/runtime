// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class ValueTypeShapeCharacteristicsTests
    {
        const ValueTypeShapeCharacteristics Float32Aggregate = ValueTypeShapeCharacteristics.Float32Aggregate;
        const ValueTypeShapeCharacteristics Float64Aggregate = ValueTypeShapeCharacteristics.Float64Aggregate;

        TestTypeSystemContext _context;
        ModuleDesc _testModule;

        public ValueTypeShapeCharacteristicsTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.ARM);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestHfaPrimitives()
        {
            DefType singleType = _context.GetWellKnownType(WellKnownType.Single);
            DefType doubleType = _context.GetWellKnownType(WellKnownType.Double);

            Assert.True(singleType.IsHomogeneousAggregate);
            Assert.Equal(Float32Aggregate, singleType.ValueTypeShapeCharacteristics);
            
            Assert.True(doubleType.IsHomogeneousAggregate);
            Assert.Equal(Float64Aggregate, doubleType.ValueTypeShapeCharacteristics);
        }

        [Fact]
        public void TestSimpleHfa()
        {
            var simpleHfaFloatStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "SimpleHfaFloatStruct");
            Assert.True(simpleHfaFloatStruct.IsHomogeneousAggregate);
            Assert.Equal(Float32Aggregate, simpleHfaFloatStruct.ValueTypeShapeCharacteristics);

            var simpleHfaFloatStructWithManyFields = _testModule.GetType("ValueTypeShapeCharacteristics", "SimpleHfaFloatStructWithManyFields");
            Assert.True(simpleHfaFloatStructWithManyFields.IsHomogeneousAggregate);
            Assert.Equal(Float32Aggregate, simpleHfaFloatStructWithManyFields.ValueTypeShapeCharacteristics);

            var simpleHfaDoubleStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "SimpleHfaDoubleStruct");
            Assert.True(simpleHfaDoubleStruct.IsHomogeneousAggregate);
            Assert.Equal(Float64Aggregate, simpleHfaDoubleStruct.ValueTypeShapeCharacteristics);
        }

        [Fact]
        public void TestCompositeHfa()
        {
            var compositeHfaFloatStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "CompositeHfaFloatStruct");
            Assert.True(compositeHfaFloatStruct.IsHomogeneousAggregate);
            Assert.Equal(Float32Aggregate, compositeHfaFloatStruct.ValueTypeShapeCharacteristics);

            var compositeHfaDoubleStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "CompositeHfaDoubleStruct");
            Assert.True(compositeHfaDoubleStruct.IsHomogeneousAggregate);
            Assert.Equal(Float64Aggregate, compositeHfaDoubleStruct.ValueTypeShapeCharacteristics);
        }

        [Fact]
        public void TestHfaNegative()
        {
            var nonHAEmptyStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "NonHAEmptyStruct");
            Assert.False(nonHAEmptyStruct.IsHomogeneousAggregate);

            var nonHAStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "NonHAStruct");
            Assert.False(nonHAStruct.IsHomogeneousAggregate);

            var nonHAMixedStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "NonHAMixedStruct");
            Assert.False(nonHAMixedStruct.IsHomogeneousAggregate);

            var nonHACompositeStruct = _testModule.GetType("ValueTypeShapeCharacteristics", "NonHACompositeStruct");
            Assert.False(nonHACompositeStruct.IsHomogeneousAggregate);

            var nonHAStructWithManyFields = _testModule.GetType("ValueTypeShapeCharacteristics", "NonHAStructWithManyFields");
            Assert.False(nonHAStructWithManyFields.IsHomogeneousAggregate);

            var objectType = _context.GetWellKnownType(WellKnownType.Object);
            Assert.False(objectType.IsHomogeneousAggregate);
        }
    }
}
