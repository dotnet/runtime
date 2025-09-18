// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class Object
    {
        private const ulong DefaultAllocationRangeStart = 0x00000000_10000000;
        private const ulong DefaultAllocationRangeEnd = 0x00000000_20000000;

        private const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;

        internal const ulong TestArrayBoundsZeroGlobalAddress = 0x00000000_100000b0;

        private const ulong TestSyncTableEntriesGlobalAddress = 0x00000000_100000c0;
        // The sync table entries address range is manually managed in AddObjectWithSyncBlock
        private const ulong TestSyncTableEntriesAddress = 0x00000000_f0000000;

        internal const ulong TestObjectToMethodTableUnmask = 0x7;
        internal const ulong TestSyncBlockValueToObjectOffset = sizeof(uint);

        internal readonly RuntimeTypeSystem RTSBuilder;
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;

        internal MockMemorySpace.BumpAllocator ManagedObjectAllocator { get; }

        internal MockMemorySpace.BumpAllocator SyncBlockAllocator { get; }

        internal TargetPointer TestStringMethodTableAddress { get; private set; }

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        public Object(RuntimeTypeSystem rtsBuilder)
            : this(rtsBuilder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public Object(RuntimeTypeSystem rtsBuilder, (ulong Start, ulong End) allocationRange)
        {
            RTSBuilder = rtsBuilder;
            ManagedObjectAllocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            const ulong TestSyncBlocksAddress = 0x00000000_e0000000;
            SyncBlockAllocator = Builder.CreateAllocator(start: TestSyncBlocksAddress, end: TestSyncBlocksAddress + 0x1000);

            Types = GetTypes();

            AddGlobalPointers();
            Globals = rtsBuilder.Globals.Concat(
            [
                (nameof(Constants.Globals.ObjectToMethodTableUnmask), TestObjectToMethodTableUnmask),
                (nameof(Constants.Globals.StringMethodTable), TestStringMethodTableGlobalAddress),
                (nameof(Constants.Globals.ArrayBoundsZero), TestArrayBoundsZeroGlobalAddress),
                (nameof(Constants.Globals.SyncTableEntries), TestSyncTableEntriesGlobalAddress),
                (nameof(Constants.Globals.ObjectHeaderSize), Builder.TargetTestHelpers.ObjHeaderSize),
                (nameof(Constants.Globals.SyncBlockValueToObjectOffset), TestSyncBlockValueToObjectOffset),
            ]).ToArray();
        }

        private Dictionary<DataType, Target.TypeInfo> GetTypes()
        {
            Dictionary<DataType, Target.TypeInfo> types = GetTypesForTypeFields(
                Builder.TargetTestHelpers,
                [
                    ObjectFields,
                    StringFields,
                    ArrayFields,
                    SyncTableEntryFields,
                    SyncBlockFields,
                    AwareLockFields,
                    InteropSyncBlockFields,
                ]);
            Debug.Assert(types[DataType.Array].Size == Builder.TargetTestHelpers.ArrayBaseSize);
            types = types.Concat(RTSBuilder.Types).ToDictionary();
            return types;
        }

        private void AddGlobalPointers()
        {
            AddStringMethodTablePointer();
            AddSyncTableEntriesPointer();
        }

        private void AddStringMethodTablePointer()
        {
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment stringMethodTableFragment = RTSBuilder.TypeSystemAllocator.Allocate((ulong)targetTestHelpers.PointerSize /*HACK*/, "String Method Table (fake)");
            TestStringMethodTableAddress = stringMethodTableFragment.Address;
            MockMemorySpace.HeapFragment fragment = new() { Name = "Address of String Method Table", Address = TestStringMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(fragment.Data, stringMethodTableFragment.Address);
            builder.AddHeapFragment(fragment);
        }

        private void AddSyncTableEntriesPointer()
        {
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment fragment = new() { Name = "Address of Sync Table Entries", Address = TestSyncTableEntriesGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(fragment.Data, TestSyncTableEntriesAddress);
            builder.AddHeapFragment(fragment);
        }

        internal TargetPointer AddObject(TargetPointer methodTable, uint prefixSize =0)
        {
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo objectTypeInfo = Types[DataType.Object];
            uint totalSize = objectTypeInfo.Size.Value + prefixSize;
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate(totalSize, $"Object : MT = '{methodTable}'");

            Span<byte> dest = fragment.Data.AsSpan((int)prefixSize);
            targetTestHelpers.WritePointer(dest.Slice(objectTypeInfo.Fields["m_pMethTab"].Offset), methodTable);
            builder.AddHeapFragment(fragment);
            return fragment.Address + prefixSize; // return pointer to the object, not the prefix;
        }

        internal TargetPointer AddObjectWithSyncBlock(TargetPointer methodTable, uint syncBlockIndex, TargetPointer rcw, TargetPointer ccw)
        {
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            const uint IsSyncBlockIndexBits = 0x08000000;
            const uint SyncBlockIndexMask = (1 << 26) - 1;
            if ((syncBlockIndex & SyncBlockIndexMask) != syncBlockIndex)
                throw new ArgumentOutOfRangeException(nameof(syncBlockIndex), "Invalid sync block index");

            TargetPointer address = AddObject(methodTable, prefixSize: (uint)TestSyncBlockValueToObjectOffset);

            // Add the sync table value before the object
            uint syncTableValue = IsSyncBlockIndexBits | syncBlockIndex;
            TargetPointer syncTableValueAddr = address - TestSyncBlockValueToObjectOffset;
            Span<byte> syncTableValueDest = builder.BorrowAddressRange(syncTableValueAddr, sizeof(uint));
            targetTestHelpers.Write(syncTableValueDest, syncTableValue);

            // Add the actual sync block and associated data
            AddSyncBlock(syncBlockIndex, rcw, ccw);
            return address;
        }

        private void AddSyncBlock(uint index, TargetPointer rcw, TargetPointer ccw)
        {
            Dictionary<DataType, Target.TypeInfo> types = Types;
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            // Tests write the sync blocks starting at TestSyncBlocksAddress
            Target.TypeInfo syncBlockTypeInfo = types[DataType.SyncBlock];
            Target.TypeInfo interopSyncBlockTypeInfo = types[DataType.InteropSyncBlockInfo];
            uint syncBlockSize = syncBlockTypeInfo.Size.Value;;
            uint interopSyncBlockInfoSize = syncBlockSize + interopSyncBlockTypeInfo.Size.Value;


            MockMemorySpace.HeapFragment syncBlock = SyncBlockAllocator.Allocate(interopSyncBlockInfoSize, $"Sync Block {index}");
            TargetPointer syncBlockAddr = syncBlock.Address;

            // Add the sync table entry - pointing at the sync block
            Target.TypeInfo syncTableEntryInfo = types[DataType.SyncTableEntry];
            uint syncTableEntrySize = (uint)targetTestHelpers.SizeOfTypeInfo(syncTableEntryInfo);
            ulong syncTableEntryAddr = TestSyncTableEntriesAddress + index * syncTableEntrySize;
            MockMemorySpace.HeapFragment syncTableEntry = new() { Name = $"SyncTableEntries[{index}]", Address = syncTableEntryAddr, Data = new byte[syncTableEntrySize] };
            Span<byte> syncTableEntryData = syncTableEntry.Data;
            targetTestHelpers.WritePointer(syncTableEntryData.Slice(syncTableEntryInfo.Fields[nameof(Data.SyncTableEntry.SyncBlock)].Offset), syncBlockAddr);

            // Add the sync block - pointing at the interop sync block info
            TargetPointer interopInfoAddr = syncBlockAddr + syncBlockSize;
            Span<byte> syncBlockData = syncBlock.Data;
            targetTestHelpers.WritePointer(syncBlockData.Slice(syncBlockTypeInfo.Fields[nameof(Data.SyncBlock.InteropInfo)].Offset), interopInfoAddr);

            // Add the interop sync block info
            Span<byte> interopInfoData = syncBlock.Data.AsSpan((int)syncBlockSize);
            targetTestHelpers.WritePointer(interopInfoData.Slice(interopSyncBlockTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.RCW)].Offset), rcw);
            targetTestHelpers.WritePointer(interopInfoData.Slice(interopSyncBlockTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.CCW)].Offset), ccw);

            builder.AddHeapFragments([syncTableEntry, syncBlock]);
        }

        internal TargetPointer AddStringObject(string value)
        {
            MockMemorySpace.Builder builder = Builder;
            Dictionary<DataType, Target.TypeInfo> types = Types;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo objectTypeInfo = types[DataType.Object];
            Target.TypeInfo stringTypeInfo = types[DataType.String];
            int size = (int)stringTypeInfo.Size.Value + value.Length * sizeof(char);
            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)size, $"String = '{value}'");
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(objectTypeInfo.Fields["m_pMethTab"].Offset), TestStringMethodTableAddress);
            targetTestHelpers.Write(dest.Slice(stringTypeInfo.Fields["m_StringLength"].Offset), (uint)value.Length);
            MemoryMarshal.Cast<char, byte>(value).CopyTo(dest.Slice(stringTypeInfo.Fields["m_FirstChar"].Offset));
            builder.AddHeapFragment(fragment);
            return fragment.Address;
        }

        internal TargetPointer AddArrayObject(Array array)
        {
            MockMemorySpace.Builder builder = Builder;
            Dictionary<DataType, Target.TypeInfo> types = Types;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            bool isSingleDimensionZeroLowerBound = array.Rank == 1 && array.GetLowerBound(0) == 0;

            // Bounds are part of the array object for non-single dimension or non-zero lower bound arrays
            //   << fields that are part of the array type info >>
            //   int32_t bounds[rank];
            //   int32_t lowerBounds[rank];
            Target.TypeInfo objectTypeInfo = types[DataType.Object];
            Target.TypeInfo arrayTypeInfo = types[DataType.Array];
            int size = (int)arrayTypeInfo.Size.Value;
            if (!isSingleDimensionZeroLowerBound)
                size += array.Rank * sizeof(int) * 2;

            uint flags = (uint)(MethodTableFlags_1.WFLAGS_HIGH.HasComponentSize | MethodTableFlags_1.WFLAGS_HIGH.Category_Array) | (uint)array.Length;
            if (isSingleDimensionZeroLowerBound)
                flags |= (uint)MethodTableFlags_1.WFLAGS_HIGH.Category_IfArrayThenSzArray;

            string name = string.Join(',', array);

            TargetPointer arrayClassAddress = RTSBuilder.AddArrayClass(name,
                attr: 0, numMethods: 0, numNonVirtualSlots: 0, rank: (byte)array.Rank);
            TargetPointer methodTableAddress = RTSBuilder.AddMethodTable(name,
                mtflags: flags, mtflags2: default, baseSize: targetTestHelpers.ArrayBaseBaseSize,
                module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: 0);
            RTSBuilder.SetEEClassAndCanonMTRefs(arrayClassAddress, methodTableAddress);

            MockMemorySpace.HeapFragment fragment = ManagedObjectAllocator.Allocate((uint)size, $"Array = '{string.Join(',', array)}'");
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(objectTypeInfo.Fields["m_pMethTab"].Offset), methodTableAddress);
            targetTestHelpers.Write(dest.Slice(arrayTypeInfo.Fields["m_NumComponents"].Offset), (uint)array.Length);
            builder.AddHeapFragment(fragment);
            return fragment.Address;
        }
    }
}
