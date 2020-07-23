// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class WellKnownTypeTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public WellKnownTypeTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestIsValueType()
        {
            Assert.True(_context.GetWellKnownType(WellKnownType.Boolean).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Char).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.SByte).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Byte).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Int16).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.UInt16).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Int32).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.UInt32).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Int64).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.UInt64).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.IntPtr).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.UIntPtr).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Single).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Double).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.ValueType).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.Enum).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.Nullable).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.Object).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.String).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.Array).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.MulticastDelegate).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.RuntimeTypeHandle).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.RuntimeMethodHandle).IsValueType);
            Assert.True(_context.GetWellKnownType(WellKnownType.RuntimeFieldHandle).IsValueType);
            Assert.False(_context.GetWellKnownType(WellKnownType.Exception).IsValueType);
        }

        [Fact]
        public void TestIsPrimitive()
        {
            Assert.True(_context.GetWellKnownType(WellKnownType.Boolean).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Char).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.SByte).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Byte).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Int16).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.UInt16).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Int32).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.UInt32).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Int64).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.UInt64).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.IntPtr).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.UIntPtr).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Single).IsPrimitive);
            Assert.True(_context.GetWellKnownType(WellKnownType.Double).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.ValueType).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.Enum).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.Nullable).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.Object).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.String).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.Array).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.MulticastDelegate).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.RuntimeTypeHandle).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.RuntimeMethodHandle).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.RuntimeFieldHandle).IsPrimitive);
            Assert.False(_context.GetWellKnownType(WellKnownType.Exception).IsPrimitive);
        }

        [Fact]
        public void TestPrimitiveSizes()
        {
            Assert.Equal(1, _context.GetWellKnownType(WellKnownType.Boolean).InstanceFieldSize.AsInt);
            Assert.Equal(2, _context.GetWellKnownType(WellKnownType.Char).InstanceFieldSize.AsInt);
            Assert.Equal(1, _context.GetWellKnownType(WellKnownType.SByte).InstanceFieldSize.AsInt);
            Assert.Equal(1, _context.GetWellKnownType(WellKnownType.Byte).InstanceFieldSize.AsInt);
            Assert.Equal(2, _context.GetWellKnownType(WellKnownType.Int16).InstanceFieldSize.AsInt);
            Assert.Equal(2, _context.GetWellKnownType(WellKnownType.UInt16).InstanceFieldSize.AsInt);
            Assert.Equal(4, _context.GetWellKnownType(WellKnownType.Int32).InstanceFieldSize.AsInt);
            Assert.Equal(4, _context.GetWellKnownType(WellKnownType.UInt32).InstanceFieldSize.AsInt);
            Assert.Equal(8, _context.GetWellKnownType(WellKnownType.Int64).InstanceFieldSize.AsInt);
            Assert.Equal(8, _context.GetWellKnownType(WellKnownType.UInt64).InstanceFieldSize.AsInt);
            Assert.Equal(_context.Target.PointerSize, _context.GetWellKnownType(WellKnownType.IntPtr).InstanceFieldSize.AsInt);
            Assert.Equal(_context.Target.PointerSize, _context.GetWellKnownType(WellKnownType.UIntPtr).InstanceFieldSize.AsInt);
            Assert.Equal(4, _context.GetWellKnownType(WellKnownType.Single).InstanceFieldSize.AsInt);
            Assert.Equal(8, _context.GetWellKnownType(WellKnownType.Double).InstanceFieldSize.AsInt);
        }

        [Fact]
        public void TestModuleType()
        {
            Assert.True(_testModule.GetGlobalModuleType().IsModuleType);
        }
    }
}
