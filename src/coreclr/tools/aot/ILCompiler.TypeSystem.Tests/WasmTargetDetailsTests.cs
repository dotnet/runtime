// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Internal.TypeSystem;
using Xunit;

namespace TypeSystemTests
{
    public class WasmTargetDetailsTests
    {
        [Theory]
        [InlineData(TargetArchitecture.Wasm32, 4)]
        [InlineData(TargetArchitecture.Wasm64, 8)]
        public void TargetAndPrimitiveSizes(TargetArchitecture architecture, int pointerSize)
        {
            var (context, _) = CreateContext(architecture);
            Assert.True(context.Target.IsWasm);
            Assert.False(context.Target.SupportsRelativePointers);
            Assert.Equal(pointerSize, context.Target.PointerSize);
            Assert.Equal(pointerSize == 4, context.Target.SupportsAlign8);
            Assert.Equal(pointerSize, context.Target.GetObjectAlignment(LayoutInt.One).AsInt);
            Assert.Equal(8, context.Target.GetObjectAlignment(new LayoutInt(8)).AsInt);

            DefType int32 = context.GetWellKnownType(WellKnownType.Int32);
            TypeDesc functionPointer = context.GetFunctionPointerType(new MethodSignature(
                MethodSignatureFlags.Static, 0, int32, new TypeDesc[] { int32 }));
            TypeDesc[] nativeSizedTypes =
            [
                context.GetWellKnownType(WellKnownType.IntPtr),
                context.GetWellKnownType(WellKnownType.UIntPtr),
                int32.MakePointerType(), int32.MakeByRefType(), functionPointer,
                int32.MakeArrayType(), context.GetWellKnownType(WellKnownType.Object),
            ];
            Assert.All(nativeSizedTypes, type => Assert.Equal(pointerSize, type.GetElementSize().AsInt));
            Assert.Equal(4, int32.GetElementSize().AsInt);
            Assert.Equal(4, context.GetWellKnownType(WellKnownType.UInt32).GetElementSize().AsInt);
            Assert.Equal(8, context.GetWellKnownType(WellKnownType.Int64).GetElementSize().AsInt);
        }

        [Theory]
        [InlineData(TargetArchitecture.Wasm32, 4)]
        [InlineData(TargetArchitecture.Wasm64, 8)]
        public void SequentialAndPackedNativeFields(TargetArchitecture architecture, int pointerSize)
        {
            var (_, module) = CreateContext(architecture);
            MetadataType native = module.GetType("WasmLayout"u8, "NativeFields"u8);
            AssertLayout(native, pointerSize, 6 * pointerSize,
                0, pointerSize, 2 * pointerSize, 3 * pointerSize, 4 * pointerSize, 5 * pointerSize);
            Assert.Equal(new[] { 1, pointerSize, pointerSize, pointerSize, pointerSize, 4 },
                native.GetFields().Select(field => field.FieldType.GetElementSize().AsInt));
            Assert.False(native.ContainsGCPointers);

            MetadataType packed = module.GetType("WasmLayout"u8, "PackedNativeFields"u8);
            AssertLayout(packed, 1, pointerSize + 5, 0, 1, pointerSize + 1);
        }

        [Theory]
        [InlineData(TargetArchitecture.Wasm32, 4)]
        [InlineData(TargetArchitecture.Wasm64, 8)]
        public void ReferenceAndGenericFields(TargetArchitecture architecture, int pointerSize)
        {
            var (context, module) = CreateContext(architecture);
            MetadataType references = module.GetType("WasmLayout"u8, "ReferenceFields"u8);
            AssertLayout(references, pointerSize, 3 * pointerSize, 0, pointerSize, 2 * pointerSize);
            Assert.True(references.ContainsGCPointers);
            Assert.Equal("101", GCPointerMap.FromInstanceLayout(references).ToString());
            Assert.Equal(4, ((ArrayType)references.GetField("array"u8).FieldType).ElementType.GetElementSize().AsInt);

            MetadataType generic = module.GetType("WasmLayout"u8, "GenericFields`1"u8);
            InstantiatedType nativeGeneric = generic.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.IntPtr));
            AssertLayout(nativeGeneric, pointerSize, 3 * pointerSize, 0, pointerSize, 2 * pointerSize);
            Assert.False(nativeGeneric.ContainsGCPointers);

            InstantiatedType intGeneric = generic.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Int32));
            AssertLayout(intGeneric, 4, 12, 0, 4, 8);

            InstantiatedType referenceGeneric = generic.MakeInstantiatedType(context.GetWellKnownType(WellKnownType.Object));
            AssertLayout(referenceGeneric, pointerSize, 3 * pointerSize, 0, pointerSize, 2 * pointerSize);
            Assert.True(referenceGeneric.ContainsGCPointers);
            Assert.Equal("010", GCPointerMap.FromInstanceLayout(referenceGeneric).ToString());
        }

        [Theory]
        [InlineData(TargetArchitecture.Wasm32, 4)]
        [InlineData(TargetArchitecture.Wasm64, 8)]
        public void ClassAndByRefLayout(TargetArchitecture architecture, int pointerSize)
        {
            var (_, module) = CreateContext(architecture);
            MetadataType type = module.GetType("Sequential"u8, "ClassDoubleBool"u8);
            Assert.Equal(8, type.InstanceByteAlignment.AsInt);
            Assert.Equal(pointerSize + 9, type.InstanceByteCountUnaligned.AsInt);
            Assert.Equal(pointerSize == 4 ? 16 : 24, type.InstanceByteCount.AsInt);
            Assert.Equal(pointerSize, type.GetField("double1"u8).Offset.AsInt);
            Assert.Equal(pointerSize + 8, type.GetField("bool1"u8).Offset.AsInt);

            MetadataType byRef = module.GetType("Explicit"u8, "ByRefStruct"u8);
            Assert.True(byRef.ContainsByRefs);
            AssertLayout(byRef, pointerSize, pointerSize, 0);
        }

        private static (TestTypeSystemContext Context, ModuleDesc Module) CreateContext(TargetArchitecture architecture)
        {
            // The fixture supplies metadata, so guest layout never depends on the host CoreLib or pointer size.
            var context = new TestTypeSystemContext(architecture, TargetOS.Browser);
            ModuleDesc module = context.CreateModuleForSimpleName("CoreTestAssembly");
            context.SetSystemModule(module);
            return (context, module);
        }

        private static void AssertLayout(DefType type, int alignment, int size, params int[] offsets)
        {
            Assert.Equal(alignment, type.InstanceFieldAlignment.AsInt);
            Assert.Equal(size, type.InstanceFieldSize.AsInt);
            Assert.Equal(offsets, type.GetFields().Select(field => field.Offset.AsInt));
        }
    }
}
