// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Per-architecture <c>TransitionBlock</c> descriptor TypeInfo for the
/// calling-convention test harness. cDAC's data-descriptor convention
/// encodes ABI constants in the "field offsets" (<c>ArgumentRegistersOffset</c>,
/// <c>FirstGCRefMapSlot</c>, <c>OffsetOfArgs</c>, optional
/// <c>OffsetOfFloatArgumentRegisters</c>), with <c>Size</c> carrying the
/// transition-block size. The values come directly from the
/// <see cref="CallConvTestCase"/> so tests can reference the same constants.
/// </summary>
internal partial class MockDescriptors
{
    public static class CallingConvention
    {
        public static Target.TypeInfo CreateTransitionBlockTypeInfo(CallConvTestCase testCase)
            => TargetTestHelpers.CreateTypeInfo(CreateTransitionBlockLayout(testCase));

        public static Layout CreateTransitionBlockLayout(CallConvTestCase testCase)
        {
            // The cDAC descriptor convention uses field offsets to carry ABI constant
            // values; field size is unused at read-time, so 1 is just a placeholder.
            LayoutBuilder builder = new("TransitionBlock", testCase.MockArch)
            {
                Size = testCase.TransitionBlockSize,
            };
            builder.AddField("ArgumentRegistersOffset", testCase.ArgumentRegistersOffset, 1);
            builder.AddField("FirstGCRefMapSlot", testCase.FirstGCRefMapSlot, 1);
            builder.AddField("OffsetOfArgs", testCase.OffsetOfArgs, 1);
            if (testCase.OffsetOfFloatArgumentRegisters is int f)
                builder.AddField("OffsetOfFloatArgumentRegisters", f, 1);
            return builder.Build();
        }

        // ----- FieldDesc layout / Value-type MT allocator -----

        /// <summary>
        /// Production <c>FieldDesc</c> layout: two DWORDs of flag bits packed with
        /// the metadata token (DWord1) and the field offset + CorElementType (DWord2),
        /// followed by a pointer to the enclosing <c>MethodTable</c>.
        /// </summary>
        public static Layout<MockFieldDesc> CreateFieldDescLayout(MockTarget.Architecture arch)
            => MockFieldDesc.CreateLayout(arch);

        public static Target.TypeInfo CreateFieldDescTypeInfo(MockTarget.Architecture arch)
            => TargetTestHelpers.CreateTypeInfo(CreateFieldDescLayout(arch));

        /// <summary>
        /// Describes a single instance field for <see cref="AddValueTypeMethodTable"/>.
        /// Statics are excluded by definition — only instance fields are reported.
        /// </summary>
        public readonly record struct ValueTypeField(int Offset, CorElementType ElementType);

        /// <summary>
        /// Allocates a value-type MethodTable + EEClass + FieldDesc array in mock
        /// memory. Returns the MT address; tests embed that pointer into a
        /// stored-sig blob via <c>ELEMENT_TYPE_INTERNAL</c> to reference the
        /// value type without going through the metadata reader.
        /// </summary>
        public static MockMethodTable AddValueTypeMethodTable(
            MockDescriptors.RuntimeTypeSystem rts,
            string name,
            int structSize,
            IReadOnlyList<ValueTypeField> fields)
        {
            MockTarget.Architecture arch = rts.Builder.TargetTestHelpers.Arch;

            MockEEClass eeClass = rts.AddEEClass(name);
            eeClass.NumInstanceFields = (ushort)fields.Count;
            eeClass.NumMethods = 0;

            // Allocate the FieldDesc array.
            if (fields.Count > 0)
            {
                Layout<MockFieldDesc> fdLayout = CreateFieldDescLayout(arch);
                MockMemorySpace.HeapFragment fdArray = rts.TypeSystemAllocator.Allocate(
                    (ulong)(fdLayout.Size * fields.Count), $"FieldDescs[{name}]");
                eeClass.FieldDescList = fdArray.Address;

                MockMethodTable enclosingMT = rts.AddMethodTable(name);
                // Wire up MT <-> EEClass first since FieldDescs back-reference the MT.
                eeClass.MethodTable = enclosingMT.Address;
                enclosingMT.EEClassOrCanonMT = eeClass.Address;

                // ValueType category flag (low 16 bits of MTFlags Category_Mask) + IsValueType bit.
                const uint Category_ValueType = 0x00040000;
                enclosingMT.MTFlags = Category_ValueType;

                // BaseSize must be pointer-aligned for TypeValidation to accept this MT
                // (real value-type MTs always satisfy this). Encode the actual struct size
                // via BaseSizePadding so GetNumInstanceFieldBytes = BaseSize - BaseSizePadding
                // returns the requested structSize.
                int ptrSize = arch.Is64Bit ? 8 : 4;
                uint alignedBaseSize = (uint)((structSize + (ptrSize - 1)) & ~(ptrSize - 1));
                if (alignedBaseSize == 0) alignedBaseSize = (uint)ptrSize;
                enclosingMT.BaseSize = alignedBaseSize;
                eeClass.BaseSizePadding = (byte)(alignedBaseSize - structSize);

                // Reserve a vtable slot so an instance method on this value type can validate.
                enclosingMT.NumVirtuals = 1;

                for (int i = 0; i < fields.Count; i++)
                {
                    ulong fdAddr = fdArray.Address + (ulong)(i * fdLayout.Size);
                    MockFieldDesc fd = fdLayout.Create(
                        fdArray.Data.AsMemory(i * fdLayout.Size, fdLayout.Size),
                        fdAddr);

                    // DWord1: token (low 24 bits) + flags. Token field encodes RID; we
                    // just use the (1-based) field index since the classifier doesn't
                    // resolve tokens for ELEMENT_TYPE_INTERNAL sigs.
                    fd.DWord1 = (uint)(i + 1) & 0xFFFFFF;

                    // DWord2: offset (low 27 bits) | (CorElementType << 27).
                    fd.DWord2 = ((uint)fields[i].Offset & 0x07FFFFFFu)
                        | (((uint)fields[i].ElementType & 0x1Fu) << 27);

                    fd.MTOfEnclosingClass = enclosingMT.Address;
                }

                return enclosingMT;
            }

            // Empty-struct path: no FieldDesc array, just an MT.
            MockMethodTable emptyMT = rts.AddMethodTable(name);
            eeClass.MethodTable = emptyMT.Address;
            emptyMT.EEClassOrCanonMT = eeClass.Address;
            emptyMT.MTFlags = 0x00040000; // Category_ValueType
            int emptyPtrSize = arch.Is64Bit ? 8 : 4;
            uint emptyAlignedBaseSize = (uint)((structSize + (emptyPtrSize - 1)) & ~(emptyPtrSize - 1));
            if (emptyAlignedBaseSize == 0) emptyAlignedBaseSize = (uint)emptyPtrSize;
            emptyMT.BaseSize = emptyAlignedBaseSize;
            eeClass.BaseSizePadding = (byte)(emptyAlignedBaseSize - structSize);
            return emptyMT;
        }

        public static MockMethodTable AddVectorMethodTable(
            MockDescriptors.RuntimeTypeSystem rts,
            string vectorTypeName,
            int vectorByteSize,
            uint typeDefToken)
        {
            const uint Category_ValueType = 0x00040000;
            const uint GenericsMask_GenericInst = 0x00000010;
            const uint IsIntrinsicType = (uint)MethodTableFlags_1.WFLAGS2_ENUM.IsIntrinsicType;
            const int MTFlags2TypeDefRidShift = 8;

            MockTarget.Architecture arch = rts.Builder.TargetTestHelpers.Arch;
            MockEEClass eeClass = rts.AddEEClass(vectorTypeName);
            eeClass.CorTypeAttr = (uint)(System.Reflection.TypeAttributes.Public | System.Reflection.TypeAttributes.Sealed | System.Reflection.TypeAttributes.SequentialLayout);
            eeClass.NumMethods = 0;

            MockMethodTable methodTable = rts.AddMethodTable(vectorTypeName);
            eeClass.MethodTable = methodTable.Address;
            methodTable.EEClassOrCanonMT = eeClass.Address;
            methodTable.MTFlags = Category_ValueType | GenericsMask_GenericInst;
            methodTable.MTFlags2 = ((typeDefToken & 0x00FFFFFFu) << MTFlags2TypeDefRidShift) | IsIntrinsicType;
            methodTable.Module = rts.SystemObjectMethodTable.Module;
            methodTable.ParentMethodTable = rts.SystemObjectMethodTable.Address;
            methodTable.NumVirtuals = 1;

            int ptrSize = arch.Is64Bit ? 8 : 4;
            uint alignedBaseSize = (uint)((vectorByteSize + (ptrSize - 1)) & ~(ptrSize - 1));
            if (alignedBaseSize == 0)
                alignedBaseSize = (uint)ptrSize;

            methodTable.BaseSize = alignedBaseSize;
            eeClass.BaseSizePadding = (byte)(alignedBaseSize - vectorByteSize);
            methodTable.PerInstInfo = AddSingleTypeInstantiation(rts, CreatePrimitiveTypeArg(rts, CorElementType.I4).Address);
            return methodTable;
        }

        private static MockMethodTable CreatePrimitiveTypeArg(MockDescriptors.RuntimeTypeSystem rts, CorElementType elementType)
        {
            MockEEClass eeClass = rts.AddEEClass($"{elementType}TypeArg");
            eeClass.InternalCorElementType = (byte)elementType;

            MockMethodTable methodTable = rts.AddMethodTable($"{elementType}TypeArg");
            methodTable.MTFlags = (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_TruePrimitive;
            methodTable.BaseSize = (uint)rts.Builder.TargetTestHelpers.PointerSize;
            methodTable.Module = rts.SystemObjectMethodTable.Module;
            methodTable.ParentMethodTable = rts.SystemObjectMethodTable.Address;
            methodTable.NumVirtuals = 1;
            eeClass.MethodTable = methodTable.Address;
            methodTable.EEClassOrCanonMT = eeClass.Address;
            return methodTable;
        }

        private static ulong AddSingleTypeInstantiation(MockDescriptors.RuntimeTypeSystem rts, ulong typeArgAddress)
        {
            TargetTestHelpers helpers = rts.Builder.TargetTestHelpers;
            int pointerSize = helpers.PointerSize;

            MockMemorySpace.HeapFragment perInstInfoFragment = rts.TypeSystemAllocator.Allocate(
                (ulong)(pointerSize * 2), "PerInstInfo[1]");
            helpers.Write(perInstInfoFragment.Data.AsSpan(0, sizeof(ushort)), (ushort)1);
            helpers.Write(perInstInfoFragment.Data.AsSpan(sizeof(ushort), sizeof(ushort)), (ushort)1);

            MockMemorySpace.HeapFragment dictionaryFragment = rts.TypeSystemAllocator.Allocate(
                (ulong)pointerSize, "GenericDictionary[1]");
            helpers.WritePointer(dictionaryFragment.Data, typeArgAddress);
            helpers.WritePointer(perInstInfoFragment.Data.AsSpan(pointerSize, pointerSize), dictionaryFragment.Address);

            return perInstInfoFragment.Address + (ulong)pointerSize;
        }
    }
}

/// <summary>
/// Mock view of <c>Data.FieldDesc</c> for the calling-convention test harness.
/// Layout: two uint flag/offset words + a pointer to the enclosing MT.
/// </summary>
internal sealed class MockFieldDesc : TypedView
{
    private const string DWord1FieldName = nameof(Data.FieldDesc.DWord1);
    private const string DWord2FieldName = nameof(Data.FieldDesc.DWord2);
    private const string MTOfEnclosingClassFieldName = nameof(Data.FieldDesc.MTOfEnclosingClass);

    public static Layout<MockFieldDesc> CreateLayout(MockTarget.Architecture arch)
        => new SequentialLayoutBuilder("FieldDesc", arch)
            .AddUInt32Field(DWord1FieldName)
            .AddUInt32Field(DWord2FieldName)
            .AddPointerField(MTOfEnclosingClassFieldName)
            .Build<MockFieldDesc>();

    public uint DWord1
    {
        get => ReadUInt32Field(DWord1FieldName);
        set => WriteUInt32Field(DWord1FieldName, value);
    }

    public uint DWord2
    {
        get => ReadUInt32Field(DWord2FieldName);
        set => WriteUInt32Field(DWord2FieldName, value);
    }

    public ulong MTOfEnclosingClass
    {
        get => ReadPointerField(MTOfEnclosingClassFieldName);
        set => WritePointerField(MTOfEnclosingClassFieldName, value);
    }
}

