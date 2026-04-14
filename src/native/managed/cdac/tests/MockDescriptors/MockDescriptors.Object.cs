// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

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
            Builder.AddHeapFragment(fragment);
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
            Builder.AddHeapFragment(fragment);
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
            Builder.AddHeapFragment(fragment);
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
