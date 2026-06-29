// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal sealed class MockObjectHeaderData : TypedView
{
    private const string PaddingFieldName = "Padding";
    private const string SyncBlockValueFieldName = nameof(Data.ObjectHeader.SyncBlockValue);

    public static Layout<MockObjectHeaderData> CreateLayout(MockTarget.Architecture architecture)
    {
        SequentialLayoutBuilder builder = new("ObjectHeader", architecture);
        if (architecture.Is64Bit)
        {
            builder.AddUInt32Field(PaddingFieldName);
        }

        return builder
            .AddUInt32Field(SyncBlockValueFieldName)
            .Build<MockObjectHeaderData>();
    }

    public uint SyncBlockValue
    {
        get => ReadUInt32Field(SyncBlockValueFieldName);
        set => WriteUInt32Field(SyncBlockValueFieldName, value);
    }
}

internal sealed class MockObjectData : TypedView
{
    private const string MethodTableFieldName = "m_pMethTab";

    public static Layout<MockObjectData> CreateLayout(MockTarget.Architecture architecture)
    {
        int pointerSize = architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        LayoutBuilder builder = new("Object", architecture);
        builder.AddField(MethodTableFieldName, 0, pointerSize);
        builder.Size = pointerSize;
        return builder.Build<MockObjectData>();
    }

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }
}

internal sealed class MockStringObjectData : TypedView
{
    private const string MethodTableFieldName = "m_pMethTab";
    private const string StringLengthFieldName = "m_StringLength";
    private const string FirstCharFieldName = "m_FirstChar";

    public static Layout<MockStringObjectData> CreateLayout(MockTarget.Architecture architecture)
    {
        int pointerSize = architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        LayoutBuilder builder = new("String", architecture);
        builder.AddField(MethodTableFieldName, 0, pointerSize);
        builder.AddField(StringLengthFieldName, pointerSize, sizeof(uint));
        builder.AddField(FirstCharFieldName, pointerSize + sizeof(uint), sizeof(char));
        builder.Size = checked((pointerSize + sizeof(uint) + sizeof(char) + pointerSize - 1) & ~(pointerSize - 1));
        return builder.Build<MockStringObjectData>();
    }

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    public uint StringLength
    {
        get => ReadUInt32Field(StringLengthFieldName);
        set => WriteUInt32Field(StringLengthFieldName, value);
    }

    public ulong FirstCharAddress => GetFieldAddress(FirstCharFieldName);
}

internal sealed class MockArrayObjectData : TypedView
{
    private const string MethodTableFieldName = "m_pMethTab";
    private const string NumComponentsFieldName = "m_NumComponents";

    public static Layout<MockArrayObjectData> CreateLayout(MockTarget.Architecture architecture)
    {
        int pointerSize = architecture.Is64Bit ? sizeof(ulong) : sizeof(uint);
        LayoutBuilder builder = new("Array", architecture);
        builder.AddField(MethodTableFieldName, 0, pointerSize);
        builder.AddField(NumComponentsFieldName, pointerSize, sizeof(uint));
        builder.Size = checked((pointerSize + sizeof(uint) + pointerSize - 1) & ~(pointerSize - 1));
        return builder.Build<MockArrayObjectData>();
    }

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    public uint NumComponents
    {
        get => ReadUInt32Field(NumComponentsFieldName);
        set => WriteUInt32Field(NumComponentsFieldName, value);
    }
}

internal sealed class MockSyncTableEntry : TypedView
{
    private const string SyncBlockFieldName = "SyncBlock";
    private const string ObjectFieldName = "Object";

    public static Layout<MockSyncTableEntry> CreateLayout(MockTarget.Architecture architecture)
        => new SequentialLayoutBuilder("SyncTableEntry", architecture)
            .AddPointerField(SyncBlockFieldName)
            .AddPointerField(ObjectFieldName)
            .Build<MockSyncTableEntry>();

    public ulong SyncBlock
    {
        get => ReadPointerField(SyncBlockFieldName);
        set => WritePointerField(SyncBlockFieldName, value);
    }

    public ulong Object
    {
        get => ReadPointerField(ObjectFieldName);
        set => WritePointerField(ObjectFieldName, value);
    }
}

internal sealed class MockDelegateObjectData : TypedView
{
    public const string MethodTableFieldName = "m_pMethTab";
    public const string HelperObjectFieldName = "HelperObject";
    public const string TargetFieldName = "Target";
    public const string MethodPtrFieldName = "MethodPtr";
    public const string MethodPtrAuxFieldName = "MethodPtrAux";
    public const string ExtraDataFieldName = "ExtraData";

    public static Layout<MockDelegateObjectData> CreateLayout(MockTarget.Architecture architecture)
    {
        SequentialLayoutBuilder builder = new("Delegate", architecture);
        return builder
            .AddPointerField(MethodTableFieldName)
            .AddPointerField(HelperObjectFieldName)
            .AddPointerField(TargetFieldName)
            .AddPointerField(MethodPtrFieldName)
            .AddPointerField(MethodPtrAuxFieldName)
            .AddNIntField(ExtraDataFieldName)
            .Build<MockDelegateObjectData>();
    }

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    public ulong HelperObject
    {
        get => ReadPointerField(HelperObjectFieldName);
        set => WritePointerField(HelperObjectFieldName, value);
    }

    public ulong Target
    {
        get => ReadPointerField(TargetFieldName);
        set => WritePointerField(TargetFieldName, value);
    }

    public ulong MethodPtr
    {
        get => ReadPointerField(MethodPtrFieldName);
        set => WritePointerField(MethodPtrFieldName, value);
    }

    public ulong MethodPtrAux
    {
        get => ReadPointerField(MethodPtrAuxFieldName);
        set => WritePointerField(MethodPtrAuxFieldName, value);
    }

    public long ExtraData
    {
        // Stored at pointer width; on 32-bit, WritePointer truncates the upper bits
        // so the signed bit pattern of `value` is preserved (e.g. -1 → 0xFFFFFFFF).
        get => unchecked((long)ReadPointerField(ExtraDataFieldName));
        set => WritePointerField(ExtraDataFieldName, unchecked((ulong)value));
    }
}

internal sealed class MockContinuationObjectData : TypedView
{
    public const string MethodTableFieldName = "m_pMethTab";
    public const string NextFieldName = "Next";
    public const string ResumeInfoFieldName = "ResumeInfo";
    public const string FlagsFieldName = "Flags";
    public const string StateFieldName = "State";

    public static Layout<MockContinuationObjectData> CreateLayout(MockTarget.Architecture architecture)
    {
        SequentialLayoutBuilder builder = new("ContinuationObject", architecture);
        return builder
            .AddPointerField(MethodTableFieldName)
            .AddPointerField(NextFieldName)
            .AddPointerField(ResumeInfoFieldName)
            .AddField(FlagsFieldName, sizeof(int))
            .AddField(StateFieldName, sizeof(int))
            .Build<MockContinuationObjectData>();
    }

    public ulong MethodTable
    {
        get => ReadPointerField(MethodTableFieldName);
        set => WritePointerField(MethodTableFieldName, value);
    }

    public ulong Next
    {
        get => ReadPointerField(NextFieldName);
        set => WritePointerField(NextFieldName, value);
    }

    public ulong ResumeInfo
    {
        get => ReadPointerField(ResumeInfoFieldName);
        set => WritePointerField(ResumeInfoFieldName, value);
    }

    public int Flags
    {
        get => unchecked((int)ReadUInt32Field(FlagsFieldName));
        set => WriteUInt32Field(FlagsFieldName, unchecked((uint)value));
    }

    public int State
    {
        get => unchecked((int)ReadUInt32Field(StateFieldName));
        set => WriteUInt32Field(StateFieldName, unchecked((uint)value));
    }
}

internal sealed class MockAsyncResumeInfoData : TypedView
{
    public const string ResumeFieldName = "Resume";
    public const string DiagnosticIPFieldName = "DiagnosticIP";

    public static Layout<MockAsyncResumeInfoData> CreateLayout(MockTarget.Architecture architecture)
    {
        SequentialLayoutBuilder builder = new("AsyncResumeInfo", architecture);
        return builder
            .AddPointerField(ResumeFieldName)
            .AddPointerField(DiagnosticIPFieldName)
            .Build<MockAsyncResumeInfoData>();
    }

    public ulong Resume
    {
        get => ReadPointerField(ResumeFieldName);
        set => WritePointerField(ResumeFieldName, value);
    }

    public ulong DiagnosticIP
    {
        get => ReadPointerField(DiagnosticIPFieldName);
        set => WritePointerField(DiagnosticIPFieldName, value);
    }
}

internal partial class MockDescriptors
{
    internal sealed class MockObjectBuilder
    {
        private const ulong DefaultAllocationRangeStart = 0x00000000_10000000;
        private const ulong DefaultAllocationRangeEnd = 0x00000000_20000000;

        internal const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;
        internal const ulong TestArrayBoundsZeroGlobalAddress = 0x00000000_100000b0;
        internal const ulong TestSyncTableEntriesGlobalAddress = 0x00000000_100000c0;
        internal const ulong TestObjectToMethodTableUnmask = 0x7;
        internal const ulong TestSyncBlockValueToObjectOffset = sizeof(uint);

        private const ulong TestSyncTableEntriesAddress = 0x00000000_f0000000;
        private const ulong TestSyncBlocksAddress = 0x00000000_e0000000;

        internal RuntimeTypeSystem RTSBuilder { get; }
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;
        internal MockMemorySpace.BumpAllocator ManagedObjectAllocator { get; }
        internal MockSyncBlockBuilder SyncBlockBuilder { get; }
        internal Layout<MockObjectHeaderData> ObjectHeaderLayout { get; }
        internal Layout<MockObjectData> ObjectLayout { get; }
        internal Layout<MockStringObjectData> StringLayout { get; }
        internal Layout<MockArrayObjectData> ArrayLayout { get; }
        internal Layout<MockDelegateObjectData> DelegateLayout { get; }
        internal Layout<MockContinuationObjectData> ContinuationLayout { get; }
        internal Layout<MockAsyncResumeInfoData> AsyncResumeInfoLayout { get; }
        internal Layout<MockSyncTableEntry> SyncTableEntryLayout { get; }
        internal ulong TestStringMethodTableAddress { get; private set; }
        internal Layout<MockSyncBlock> SyncBlockLayout => SyncBlockBuilder.SyncBlockLayout;
        internal Layout<MockInteropSyncBlockInfo> InteropSyncBlockInfoLayout => SyncBlockBuilder.InteropSyncBlockInfoLayout;

        public MockObjectBuilder(RuntimeTypeSystem rtsBuilder)
            : this(rtsBuilder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        {
        }

        public MockObjectBuilder(RuntimeTypeSystem rtsBuilder, (ulong Start, ulong End) allocationRange)
        {
            ArgumentNullException.ThrowIfNull(rtsBuilder);

            RTSBuilder = rtsBuilder;
            ManagedObjectAllocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
            MockMemorySpace.BumpAllocator syncBlockAllocator = Builder.CreateAllocator(TestSyncBlocksAddress, TestSyncBlocksAddress + 0x1000);
            SyncBlockBuilder = new MockSyncBlockBuilder(Builder, syncBlockAllocator, initializeCacheAndGlobals: false);

            ObjectHeaderLayout = MockObjectHeaderData.CreateLayout(Builder.TargetTestHelpers.Arch);
            ObjectLayout = MockObjectData.CreateLayout(Builder.TargetTestHelpers.Arch);
            StringLayout = MockStringObjectData.CreateLayout(Builder.TargetTestHelpers.Arch);
            ArrayLayout = MockArrayObjectData.CreateLayout(Builder.TargetTestHelpers.Arch);
            DelegateLayout = MockDelegateObjectData.CreateLayout(Builder.TargetTestHelpers.Arch);
            ContinuationLayout = MockContinuationObjectData.CreateLayout(Builder.TargetTestHelpers.Arch);
            AsyncResumeInfoLayout = MockAsyncResumeInfoData.CreateLayout(Builder.TargetTestHelpers.Arch);
            SyncTableEntryLayout = MockSyncTableEntry.CreateLayout(Builder.TargetTestHelpers.Arch);

            Debug.Assert(ArrayLayout.Size == Builder.TargetTestHelpers.ArrayBaseSize);

            AddStringMethodTablePointer();
            AddSyncTableEntriesPointer();
        }

        internal ulong AddObject(ulong methodTable, uint prefixSize = 0)
        {
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)(ObjectLayout.Size + prefixSize), $"Object : MT = '{methodTable}'");
            MockObjectData mockObject = ObjectLayout.Create(fragment.Data.AsMemory((int)prefixSize, ObjectLayout.Size), fragment.Address + prefixSize);
            mockObject.MethodTable = methodTable;
            return mockObject.Address;
        }

        internal ulong AddObjectWithSyncBlock(ulong methodTable, uint syncBlockIndex, ulong rcw, ulong ccw, ulong ccf)
        {
            const uint IsSyncBlockIndexBits = 0x08000000;
            const uint SyncBlockIndexMask = (1 << 26) - 1;
            if ((syncBlockIndex & SyncBlockIndexMask) != syncBlockIndex)
            {
                throw new ArgumentOutOfRangeException(nameof(syncBlockIndex), "Invalid sync block index");
            }

            ulong address = AddObject(methodTable, prefixSize: (uint)ObjectHeaderLayout.Size);

            uint syncTableValue = IsSyncBlockIndexBits | syncBlockIndex;
            ulong syncTableValueAddress = address - TestSyncBlockValueToObjectOffset;
            Builder.TargetTestHelpers.Write(Builder.BorrowAddressRange(syncTableValueAddress, sizeof(uint)), syncTableValue);

            AddSyncBlock(syncBlockIndex, rcw, ccw, ccf);
            return address;
        }

        internal ulong AddStringObject(string value)
        {
            ArgumentNullException.ThrowIfNull(value);

            int size = StringLayout.Size + (value.Length * sizeof(char));
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)size, $"String = '{value}'");
            MockStringObjectData mockString = StringLayout.Create(fragment.Data.AsMemory(), fragment.Address);
            mockString.MethodTable = TestStringMethodTableAddress;
            mockString.StringLength = (uint)value.Length;
            MemoryMarshal.Cast<char, byte>(value).CopyTo(fragment.Data.AsSpan((int)(mockString.FirstCharAddress - fragment.Address)));
            return fragment.Address;
        }

        internal ulong AddArrayObject(Array array)
        {
            ArgumentNullException.ThrowIfNull(array);

            bool isSingleDimensionZeroLowerBound = array.Rank == 1 && array.GetLowerBound(0) == 0;

            int size = ArrayLayout.Size;
            if (!isSingleDimensionZeroLowerBound)
            {
                size += array.Rank * sizeof(int) * 2;
            }

            uint flags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array) | (uint)array.Length;
            if (isSingleDimensionZeroLowerBound)
            {
                flags |= (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_IfArrayThenSzArray;
            }

            uint baseSize = Builder.TargetTestHelpers.ArrayBaseBaseSize;
            if (!isSingleDimensionZeroLowerBound)
            {
                baseSize += (uint)(array.Rank * sizeof(int) * 2);
            }

            string name = string.Join(',', array);
            MockEEClass eeClass = RTSBuilder.AddEEClass(name);
            MockMethodTable methodTable = RTSBuilder.AddMethodTable(name);
            methodTable.MTFlags = flags;
            methodTable.BaseSize = baseSize;
            eeClass.MethodTable = methodTable.Address;
            methodTable.EEClassOrCanonMT = eeClass.Address;

            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)size, $"Array = '{name}'");
            MockArrayObjectData arrayObject = ArrayLayout.Create(fragment);
            arrayObject.MethodTable = methodTable.Address;
            arrayObject.NumComponents = (uint)array.Length;
            return fragment.Address;
        }

        internal ulong AddDelegateObject(ulong methodTable, ulong target, ulong methodPtr, ulong methodPtrAux, long extraData)
        {
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)DelegateLayout.Size, $"Delegate : MT = '{methodTable}'");
            MockDelegateObjectData mockDelegate = DelegateLayout.Create(fragment);
            mockDelegate.MethodTable = methodTable;
            mockDelegate.Target = target;
            mockDelegate.MethodPtr = methodPtr;
            mockDelegate.MethodPtrAux = methodPtrAux;
            mockDelegate.ExtraData = extraData;
            return fragment.Address;
        }

        internal ulong AddContinuationObject(ulong methodTable, ulong next, ulong resumeInfo, int state, int flags = 0)
        {
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)ContinuationLayout.Size, $"Continuation : MT = '{methodTable}'");
            MockContinuationObjectData mockContinuation = ContinuationLayout.Create(fragment);
            mockContinuation.MethodTable = methodTable;
            mockContinuation.Next = next;
            mockContinuation.ResumeInfo = resumeInfo;
            mockContinuation.Flags = flags;
            mockContinuation.State = state;
            return fragment.Address;
        }

        internal ulong AddAsyncResumeInfo(ulong diagnosticIP, ulong resume = 0)
        {
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)AsyncResumeInfoLayout.Size, $"AsyncResumeInfo : DiagnosticIP = '{diagnosticIP}'");
            MockAsyncResumeInfoData mockResumeInfo = AsyncResumeInfoLayout.Create(fragment);
            mockResumeInfo.Resume = resume;
            mockResumeInfo.DiagnosticIP = diagnosticIP;
            return fragment.Address;
        }

        private void AddStringMethodTablePointer()
        {
            TargetTestHelpers targetTestHelpers = Builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment stringMethodTableFragment = RTSBuilder.TypeSystemAllocator.Allocate((ulong)targetTestHelpers.PointerSize, "String Method Table (fake)");
            TestStringMethodTableAddress = stringMethodTableFragment.Address;

            MockMemorySpace.HeapFragment fragment = new()
            {
                Name = "Address of String Method Table",
                Address = TestStringMethodTableGlobalAddress,
                Data = new byte[targetTestHelpers.PointerSize],
            };
            targetTestHelpers.WritePointer(fragment.Data, stringMethodTableFragment.Address);
            Builder.AddHeapFragment(fragment);
        }

        private void AddSyncTableEntriesPointer()
        {
            TargetTestHelpers targetTestHelpers = Builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment fragment = new()
            {
                Name = "Address of Sync Table Entries",
                Address = TestSyncTableEntriesGlobalAddress,
                Data = new byte[targetTestHelpers.PointerSize],
            };
            targetTestHelpers.WritePointer(fragment.Data, TestSyncTableEntriesAddress);
            Builder.AddHeapFragment(fragment);
        }

        private void AddSyncBlock(uint index, ulong rcw, ulong ccw, ulong ccf)
        {
            MockSyncBlock syncBlock = SyncBlockBuilder.AddSyncBlock(rcw, ccw, ccf, name: $"Sync Block {index}");

            ulong syncTableEntryAddress = TestSyncTableEntriesAddress + ((ulong)index * (ulong)SyncTableEntryLayout.Size);
            MockMemorySpace.HeapFragment syncTableEntryFragment = new()
            {
                Name = $"SyncTableEntries[{index}]",
                Address = syncTableEntryAddress,
                Data = new byte[SyncTableEntryLayout.Size],
            };
            MockSyncTableEntry syncTableEntry = SyncTableEntryLayout.Create(syncTableEntryFragment);
            syncTableEntry.SyncBlock = syncBlock.Address;

            Builder.AddHeapFragment(syncTableEntryFragment);
        }
    }
}
