// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;
using Internal.TypeSystem.Interop;

using Xunit;

namespace TypeSystemTests
{
    public class MarshalUtilsTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public MarshalUtilsTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Theory]
        [InlineData(WellKnownType.Void)]
        [InlineData(WellKnownType.Boolean)]
        [InlineData(WellKnownType.Char)]
        [InlineData(WellKnownType.SByte)]
        [InlineData(WellKnownType.Byte)]
        [InlineData(WellKnownType.Int16)]
        [InlineData(WellKnownType.UInt16)]
        [InlineData(WellKnownType.Int32)]
        [InlineData(WellKnownType.UInt32)]
        [InlineData(WellKnownType.Int64)]
        [InlineData(WellKnownType.UInt64)]
        [InlineData(WellKnownType.IntPtr)]
        [InlineData(WellKnownType.UIntPtr)]
        [InlineData(WellKnownType.Single)]
        [InlineData(WellKnownType.Double)]
        [InlineData(WellKnownType.RuntimeFieldHandle)]
        [InlineData(WellKnownType.RuntimeTypeHandle)]
        [InlineData(WellKnownType.RuntimeMethodHandle)]
        public void IsBlittableType_BilittableWellKnownTypes_ReturnsTrue(WellKnownType type) =>
            Assert.True(MarshalUtils.IsBlittableType(_context.GetWellKnownType(type)));

        [Theory]
        [InlineData(WellKnownType.String)]
        [InlineData(WellKnownType.ValueType)]
        [InlineData(WellKnownType.Enum)]
        [InlineData(WellKnownType.Array)]
        [InlineData(WellKnownType.MulticastDelegate)]
        [InlineData(WellKnownType.Exception)]
        [InlineData(WellKnownType.Object)]
        public void IsBlittableType_NonBilittableWellKnownTypes_ReturnsFalse(WellKnownType type) =>
            Assert.False(MarshalUtils.IsBlittableType(_context.GetWellKnownType(type)));

        [Theory]
        [InlineData("ClassWithExplicitByteBase")]
        [InlineData("ClassWithExplicitInt16Base")]
        [InlineData("ClassWithExplicitInt32Base")]
        [InlineData("ClassWithExplicitInt64Base")]
        [InlineData("ClassWithSequentialByteBase")]
        [InlineData("ClassWithSequentialInt16Base")]
        [InlineData("ClassWithSequentialInt32Base")]
        [InlineData("ClassWithSequentialInt64Base")]
        public void IsBlittableType_TypeWithBlittableBase_ReturnsTrue(string className)
        {
            TypeDesc classWithBlittableBase = _testModule.GetType("Marshalling", className);
            Assert.True(MarshalUtils.IsBlittableType(classWithBlittableBase));
        }

        [Theory]
        [InlineData("ClassWithExplicitEmptyBase")]
        [InlineData("ClassWithExplicitEmptySizeZeroBase")]
        [InlineData("ClassWithSequentialEmptyBase")]
        public void IsBlittableType_TypeWithEmptyBase_ReturnsTrue(string className)
        {
            TypeDesc classWithEmptyBase = _testModule.GetType("Marshalling", className);
            Assert.True(MarshalUtils.IsBlittableType(classWithEmptyBase));
        }
    }
}
