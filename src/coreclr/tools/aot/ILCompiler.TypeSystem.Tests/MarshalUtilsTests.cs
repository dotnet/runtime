// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;
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
            TypeDesc classWithBlittableBase = _testModule.GetType("Marshalling"u8, Encoding.UTF8.GetBytes(className));
            Assert.True(MarshalUtils.IsBlittableType(classWithBlittableBase));
        }

        [Theory]
        [InlineData("ClassWithExplicitEmptyBase")]
        [InlineData("ClassWithExplicitEmptySizeZeroBase")]
        [InlineData("ClassWithSequentialEmptyBase")]
        public void IsBlittableType_TypeWithEmptyBase_ReturnsTrue(string className)
        {
            TypeDesc classWithEmptyBase = _testModule.GetType("Marshalling"u8, Encoding.UTF8.GetBytes(className));
            Assert.True(MarshalUtils.IsBlittableType(classWithEmptyBase));
        }

        [Theory]
        [InlineData("ClassWithReferenceField")]
        [InlineData("StructWithReferenceField")]
        public void IsBlittableType_MissingHandleTypes_ReferenceFieldReturnsFalse(string typeName)
        {
            TypeDesc type = _testModule.GetType("Marshalling"u8, Encoding.UTF8.GetBytes(typeName));
            Assert.False(MarshalUtils.IsBlittableType(type));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void IsHandleType_MissingHandleType_ReturnsFalse(bool isCriticalHandle)
        {
            TypeDesc type = _testModule.GetType("Marshalling"u8, "SimpleEmptyClass"u8);
            byte[] handleName = Encoding.UTF8.GetBytes(isCriticalHandle ? "CriticalHandle" : "SafeHandle");
            Assert.Null(_context.SystemModule.GetType("System.Runtime.InteropServices"u8, handleName, throwIfNotFound: false));

            Assert.False(isCriticalHandle ? InteropTypes.IsCriticalHandle(_context, type) : InteropTypes.IsSafeHandle(_context, type));
            Assert.Throws<InvalidOperationException>(() => isCriticalHandle ? InteropTypes.GetCriticalHandle(_context) : InteropTypes.GetSafeHandle(_context));
        }

        [Theory]
        [InlineData("SimpleEmptyClass", MarshallerType.Argument, nameof(MarshallerKind.Invalid))]
        [InlineData("SimpleEmptyClass", MarshallerType.Field, nameof(MarshallerKind.Invalid))]
        [InlineData("ClassWithSequentialEmptyBase", MarshallerType.Argument, nameof(MarshallerKind.LayoutClassPtr))]
        [InlineData("ClassWithSequentialEmptyBase", MarshallerType.Field, nameof(MarshallerKind.LayoutClass))]
        [InlineData("ClassWithExplicitInt32Base", MarshallerType.Argument, nameof(MarshallerKind.LayoutClassPtr))]
        [InlineData("ClassWithExplicitInt32Base", MarshallerType.Field, nameof(MarshallerKind.LayoutClass))]
        public void GetMarshallerKind_MissingHandleTypes_ClassifiesUnrelatedType(string className, MarshallerType marshallerType, string expectedKind)
        {
            Assert.Null(_context.SystemModule.GetType("System.Runtime.InteropServices"u8, "SafeHandle"u8, throwIfNotFound: false));
            Assert.Null(_context.SystemModule.GetType("System.Runtime.InteropServices"u8, "CriticalHandle"u8, throwIfNotFound: false));
            TypeDesc type = _testModule.GetType("Marshalling"u8, Encoding.UTF8.GetBytes(className));

            MarshallerKind kind = MarshalHelpers.GetMarshallerKind(type, parameterIndex: null, customModifierData: null,
                marshalAs: null, isReturn: false, isAnsi: false, marshallerType, out MarshallerKind elementKind);

            Assert.Equal(expectedKind, kind.ToString());
            Assert.Equal(MarshallerKind.Invalid, elementKind);
        }

        [Theory]
        [InlineData("IUnknownWithIidParameterIndex", NativeTypeKind.IUnknown)]
        [InlineData("IDispatchWithIidParameterIndex", NativeTypeKind.IDispatch)]
        [InlineData("InterfaceWithIidParameterIndex", NativeTypeKind.Intf)]
        public void MarshalAsComInterface_WithIidParameterIndex_ParsesCorrectly(string methodName, NativeTypeKind nativeType)
        {
            MetadataType marshallingType = _testModule.GetType("Marshalling"u8, "MethodsWithMarshalAs"u8);
            MethodDesc marshalAsMethod = marshallingType.GetMethod(Encoding.UTF8.GetBytes(methodName), null);

            ParameterMetadata parameter = marshalAsMethod.GetParameterMetadata()[1];

            Assert.Equal(nativeType, parameter.MarshalAsDescriptor.Type);
        }
    }
}
