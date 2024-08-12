// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class MockDescriptors
{
    private static readonly Target.TypeInfo MethodTableTypeInfo = new()
    {
        Fields = {
            { nameof(Data.MethodTable.MTFlags), new() { Offset = 4, Type = DataType.uint32}},
            { nameof(Data.MethodTable.BaseSize), new() { Offset = 8, Type = DataType.uint32}},
            { nameof(Data.MethodTable.MTFlags2), new() { Offset = 12, Type = DataType.uint32}},
            { nameof(Data.MethodTable.EEClassOrCanonMT), new () { Offset = 16, Type = DataType.nuint}},
            { nameof(Data.MethodTable.Module), new () { Offset = 24, Type = DataType.pointer}},
            { nameof(Data.MethodTable.ParentMethodTable), new () { Offset = 40, Type = DataType.pointer}},
            { nameof(Data.MethodTable.NumInterfaces), new () { Offset = 48, Type = DataType.uint16}},
            { nameof(Data.MethodTable.NumVirtuals), new () { Offset = 50, Type = DataType.uint16}},
            { nameof(Data.MethodTable.PerInstInfo), new () { Offset = 56, Type = DataType.pointer}},
        }
    };

    private static readonly Target.TypeInfo EEClassTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof (Data.EEClass.MethodTable), new () { Offset = 8, Type = DataType.pointer}},
            { nameof (Data.EEClass.CorTypeAttr), new () { Offset = 16, Type = DataType.uint32}},
            { nameof (Data.EEClass.NumMethods), new () { Offset = 20, Type = DataType.uint16}},
            { nameof (Data.EEClass.InternalCorElementType), new () { Offset = 22, Type = DataType.uint8}},
            { nameof (Data.EEClass.NumNonVirtualSlots), new () { Offset = 24, Type = DataType.uint16}},
        }
    };

    private static readonly Target.TypeInfo ArrayClassTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof (Data.ArrayClass.Rank), new () { Offset = 0x70, Type = DataType.uint8}},
        }
    };

    private static readonly Target.TypeInfo ObjectTypeInfo = new()
    {
        Fields = {
            { "m_pMethTab", new() { Offset = 0, Type = DataType.pointer} },
        }
    };

    private static readonly Target.TypeInfo StringTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { "m_StringLength", new() { Offset = 0x8, Type = DataType.uint32} },
            { "m_FirstChar", new() { Offset = 0xc, Type = DataType.uint16} },
        }
    };

    private static readonly Target.TypeInfo ArrayTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { "m_NumComponents", new() { Offset = 0x8, Type = DataType.uint32} },
        },
    };

    private static readonly Target.TypeInfo SyncTableEntryInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof(Data.SyncTableEntry.SyncBlock), new() { Offset = 0, Type = DataType.pointer} },
        },
    };

    private static readonly Target.TypeInfo SyncBlockTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof(Data.SyncBlock.InteropInfo), new() { Offset = 0, Type = DataType.pointer} },
        },
    };

    private static readonly Target.TypeInfo InteropSyncBlockTypeInfo = new Target.TypeInfo()
    {
        Fields = {
            { nameof(Data.InteropSyncBlockInfo.RCW), new() { Offset = 0, Type = DataType.pointer} },
            { nameof(Data.InteropSyncBlockInfo.CCW), new() { Offset = 0x8, Type = DataType.pointer} },
        },
    };

    public static class RuntimeTypeSystem
    {
        internal const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;
        internal const ulong TestFreeObjectMethodTableAddress = 0x00000000_7a0000a8;

        internal static readonly Dictionary<DataType, Target.TypeInfo> Types = new()
        {
            [DataType.MethodTable] = MethodTableTypeInfo,
            [DataType.EEClass] = EEClassTypeInfo,
            [DataType.ArrayClass] = ArrayClassTypeInfo,
        };

        internal static readonly (string Name, ulong Value, string? Type)[] Globals =
        [
            (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
            (nameof(Constants.Globals.MethodDescAlignment), 8, nameof(DataType.uint64)),
        ];

        internal static MockMemorySpace.Builder AddGlobalPointers(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
        {
            return AddFreeObjectMethodTable(targetTestHelpers, builder);
        }

        private static MockMemorySpace.Builder AddFreeObjectMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
        {
            MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(globalAddr.Data, TestFreeObjectMethodTableAddress);
            return builder.AddHeapFragments([
                globalAddr,
                new () { Name = "Free Object Method Table", Address = TestFreeObjectMethodTableAddress, Data = new byte[targetTestHelpers.SizeOfTypeInfo(MethodTableTypeInfo)] }
            ]);
        }

        internal static MockMemorySpace.Builder AddEEClass(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods, ushort numNonVirtualSlots)
        {
            MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"EEClass '{name}'", Address = eeClassPtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(EEClassTypeInfo)] };
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
            targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            return builder.AddHeapFragment(eeClassFragment);
        }

        internal static MockMemorySpace.Builder AddArrayClass(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods, ushort numNonVirtualSlots, byte rank)
        {
            int size = targetTestHelpers.SizeOfTypeInfo(EEClassTypeInfo) + targetTestHelpers.SizeOfTypeInfo(ArrayClassTypeInfo);
            MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"ArrayClass '{name}'", Address = eeClassPtr, Data = new byte[size] };
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
            targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(EEClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            targetTestHelpers.Write(dest.Slice(ArrayClassTypeInfo.Fields[nameof(Data.ArrayClass.Rank)].Offset), rank);
            return builder.AddHeapFragment(eeClassFragment);
        }

        internal static MockMemorySpace.Builder AddMethodTable(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer methodTablePtr, string name, TargetPointer eeClassOrCanonMT, uint mtflags, uint mtflags2, uint baseSize,
                                                            TargetPointer module, TargetPointer parentMethodTable, ushort numInterfaces, ushort numVirtuals)
        {
            MockMemorySpace.HeapFragment methodTableFragment = new() { Name = $"MethodTable '{name}'", Address = methodTablePtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(MethodTableTypeInfo)] };
            Span<byte> dest = methodTableFragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.EEClassOrCanonMT)].Offset), eeClassOrCanonMT);
            targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags)].Offset), mtflags);
            targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags2)].Offset), mtflags2);
            targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.BaseSize)].Offset), baseSize);
            targetTestHelpers.WritePointer(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.Module)].Offset), module);
            targetTestHelpers.WritePointer(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.ParentMethodTable)].Offset), parentMethodTable);
            targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.NumInterfaces)].Offset), numInterfaces);
            targetTestHelpers.Write(dest.Slice(MethodTableTypeInfo.Fields[nameof(Data.MethodTable.NumVirtuals)].Offset), numVirtuals);

            // TODO fill in the rest of the fields
            return builder.AddHeapFragment(methodTableFragment);
        }
    }

    public static class Object
    {
        private const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;
        private const ulong TestStringMethodTableAddress = 0x00000000_100000a8;
        internal const ulong TestArrayBoundsZeroGlobalAddress = 0x00000000_100000b0;

        private const ulong TestSyncTableEntriesGlobalAddress = 0x00000000_100000c0;
        private const ulong TestSyncTableEntriesAddress = 0x00000000_f0000000;

        internal const ulong TestObjectToMethodTableUnmask = 0x7;
        internal const ulong TestSyncBlockValueToObjectOffset = sizeof(uint);

        internal static Dictionary<DataType, Target.TypeInfo> Types(TargetTestHelpers helpers) => RuntimeTypeSystem.Types.Concat(
        new Dictionary<DataType, Target.TypeInfo>()
        {
            [DataType.Object] = ObjectTypeInfo,
            [DataType.String] = StringTypeInfo,
            [DataType.Array] = ArrayTypeInfo with { Size = helpers.ArrayBaseSize },
            [DataType.SyncTableEntry] = SyncTableEntryInfo with { Size = (uint)helpers.SizeOfTypeInfo(SyncTableEntryInfo) },
            [DataType.SyncBlock] = SyncBlockTypeInfo,
            [DataType.InteropSyncBlockInfo] = InteropSyncBlockTypeInfo,
        }).ToDictionary();

        internal static (string Name, ulong Value, string? Type)[] Globals(TargetTestHelpers helpers) => RuntimeTypeSystem.Globals.Concat(
        [
            (nameof(Constants.Globals.ObjectToMethodTableUnmask), TestObjectToMethodTableUnmask, "uint8"),
            (nameof(Constants.Globals.StringMethodTable), TestStringMethodTableGlobalAddress, null),
            (nameof(Constants.Globals.ArrayBoundsZero), TestArrayBoundsZeroGlobalAddress, null),
            (nameof(Constants.Globals.SyncTableEntries), TestSyncTableEntriesGlobalAddress, null),
            (nameof(Constants.Globals.ObjectHeaderSize), helpers.ObjHeaderSize, "uint32"),
            (nameof(Constants.Globals.SyncBlockValueToObjectOffset), TestSyncBlockValueToObjectOffset, "uint16"),
        ]).ToArray();

        internal static MockMemorySpace.Builder AddGlobalPointers(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
        {
            builder = RuntimeTypeSystem.AddGlobalPointers(targetTestHelpers, builder);
            builder = AddStringMethodTablePointer(targetTestHelpers, builder);
            builder = AddSyncTableEntriesPointer(targetTestHelpers, builder);
            return builder;
        }

        private static MockMemorySpace.Builder AddStringMethodTablePointer(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
        {
            MockMemorySpace.HeapFragment fragment = new() { Name = "Address of String Method Table", Address = TestStringMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(fragment.Data, TestStringMethodTableAddress);
            return builder.AddHeapFragments([
                fragment,
                new () { Name = "String Method Table", Address = TestStringMethodTableAddress, Data = new byte[targetTestHelpers.PointerSize] }
            ]);
        }

        private static MockMemorySpace.Builder AddSyncTableEntriesPointer(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder)
        {
            MockMemorySpace.HeapFragment fragment = new() { Name = "Address of Sync Table Entries", Address = TestSyncTableEntriesGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(fragment.Data, TestSyncTableEntriesAddress);
            return builder.AddHeapFragment(fragment);
        }

        internal static MockMemorySpace.Builder AddObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, TargetPointer methodTable)
        {
            MockMemorySpace.HeapFragment fragment = new() { Name = $"Object : MT = '{methodTable}'", Address = address, Data = new byte[targetTestHelpers.SizeOfTypeInfo(ObjectTypeInfo)] };
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(ObjectTypeInfo.Fields["m_pMethTab"].Offset), methodTable);
            return builder.AddHeapFragment(fragment);
        }

        internal static MockMemorySpace.Builder AddObjectWithSyncBlock(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, TargetPointer methodTable, uint syncBlockIndex, TargetPointer rcw, TargetPointer ccw)
        {
            const uint IsSyncBlockIndexBits = 0x08000000;
            const uint SyncBlockIndexMask = (1 << 26) - 1;
            if ((syncBlockIndex & SyncBlockIndexMask) != syncBlockIndex)
                throw new ArgumentOutOfRangeException(nameof(syncBlockIndex), "Invalid sync block index");

            builder = AddObject(targetTestHelpers, builder, address, methodTable);

            // Add the sync table value before the object
            uint syncTableValue = IsSyncBlockIndexBits | syncBlockIndex;
            TargetPointer syncTableValueAddr = address - TestSyncBlockValueToObjectOffset;
            MockMemorySpace.HeapFragment fragment = new() { Name = $"Sync Table Value : index = {syncBlockIndex}", Address = syncTableValueAddr, Data = new byte[sizeof(uint)] };
            targetTestHelpers.Write(fragment.Data, syncTableValue);
            builder = builder.AddHeapFragment(fragment);

            // Add the actual sync block and associated data
            return AddSyncBlock(targetTestHelpers, builder, syncBlockIndex, rcw, ccw);
        }

        private static MockMemorySpace.Builder AddSyncBlock(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, uint index, TargetPointer rcw, TargetPointer ccw)
        {
            // Tests write the sync blocks starting at TestSyncBlocksAddress
            const ulong TestSyncBlocksAddress = 0x00000000_e0000000;
            int syncBlockSize = targetTestHelpers.SizeOfTypeInfo(SyncBlockTypeInfo);
            int interopSyncBlockInfoSize = targetTestHelpers.SizeOfTypeInfo(InteropSyncBlockTypeInfo);
            ulong syncBlockAddr = TestSyncBlocksAddress + index * (ulong)(syncBlockSize + interopSyncBlockInfoSize);

            // Add the sync table entry - pointing at the sync block
            uint syncTableEntrySize = (uint)targetTestHelpers.SizeOfTypeInfo(SyncTableEntryInfo);
            ulong syncTableEntryAddr = TestSyncTableEntriesAddress + index * syncTableEntrySize;
            MockMemorySpace.HeapFragment syncTableEntry = new() { Name = $"SyncTableEntries[{index}]", Address = syncTableEntryAddr, Data = new byte[syncTableEntrySize] };
            Span<byte> syncTableEntryData = syncTableEntry.Data;
            targetTestHelpers.WritePointer(syncTableEntryData.Slice(SyncTableEntryInfo.Fields[nameof(Data.SyncTableEntry.SyncBlock)].Offset), syncBlockAddr);

            // Add the sync block - pointing at the interop sync block info
            ulong interopInfoAddr = syncBlockAddr + (ulong)syncBlockSize;
            MockMemorySpace.HeapFragment syncBlock = new() { Name = $"Sync Block", Address = syncBlockAddr, Data = new byte[syncBlockSize] };
            Span<byte> syncBlockData = syncBlock.Data;
            targetTestHelpers.WritePointer(syncBlockData.Slice(SyncBlockTypeInfo.Fields[nameof(Data.SyncBlock.InteropInfo)].Offset), interopInfoAddr);

            // Add the interop sync block info
            MockMemorySpace.HeapFragment interopInfo = new() { Name = $"Interop Sync Block Info", Address = interopInfoAddr, Data = new byte[interopSyncBlockInfoSize] };
            Span<byte> interopInfoData = interopInfo.Data;
            targetTestHelpers.WritePointer(interopInfoData.Slice(InteropSyncBlockTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.RCW)].Offset), rcw);
            targetTestHelpers.WritePointer(interopInfoData.Slice(InteropSyncBlockTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.CCW)].Offset), ccw);

            return builder.AddHeapFragments([syncTableEntry, syncBlock, interopInfo]);
        }

        internal static MockMemorySpace.Builder AddStringObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, string value)
        {
            int size = targetTestHelpers.SizeOfTypeInfo(ObjectTypeInfo) + targetTestHelpers.SizeOfTypeInfo(StringTypeInfo) + value.Length * sizeof(char);
            MockMemorySpace.HeapFragment fragment = new() { Name = $"String = '{value}'", Address = address, Data = new byte[size] };
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(ObjectTypeInfo.Fields["m_pMethTab"].Offset), TestStringMethodTableAddress);
            targetTestHelpers.Write(dest.Slice(StringTypeInfo.Fields["m_StringLength"].Offset), (uint)value.Length);
            MemoryMarshal.Cast<char, byte>(value).CopyTo(dest.Slice(StringTypeInfo.Fields["m_FirstChar"].Offset));
            return builder.AddHeapFragment(fragment);
        }

        internal static MockMemorySpace.Builder AddArrayObject(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, TargetPointer address, Array array)
        {
            bool isSingleDimensionZeroLowerBound = array.Rank == 1 && array.GetLowerBound(0) == 0;

            // Bounds are part of the array object for non-single dimension or non-zero lower bound arrays
            //   << fields that are part of the array type info >>
            //   int32_t bounds[rank];
            //   int32_t lowerBounds[rank];
            int size = targetTestHelpers.SizeOfTypeInfo(ObjectTypeInfo) + targetTestHelpers.SizeOfTypeInfo(ArrayTypeInfo);
            if (!isSingleDimensionZeroLowerBound)
                size += array.Rank * sizeof(int) * 2;

            ulong methodTableAddress = (address.Value + (ulong)size + (TestObjectToMethodTableUnmask - 1)) & ~(TestObjectToMethodTableUnmask - 1);
            ulong arrayClassAddress = methodTableAddress + (ulong)targetTestHelpers.SizeOfTypeInfo(RuntimeTypeSystem.Types[DataType.MethodTable]);

            uint flags = (uint)(RuntimeTypeSystem_1.WFLAGS_HIGH.HasComponentSize | RuntimeTypeSystem_1.WFLAGS_HIGH.Category_Array) | (uint)array.Length;
            if (isSingleDimensionZeroLowerBound)
                flags |= (uint)RuntimeTypeSystem_1.WFLAGS_HIGH.Category_IfArrayThenSzArray;

            string name = string.Join(',', array);

            builder = RuntimeTypeSystem.AddArrayClass(targetTestHelpers, builder, arrayClassAddress, name, methodTableAddress,
                attr: 0, numMethods: 0, numNonVirtualSlots: 0, rank: (byte)array.Rank);
            builder = RuntimeTypeSystem.AddMethodTable(targetTestHelpers, builder, methodTableAddress, name, arrayClassAddress,
                mtflags: flags, mtflags2: default, baseSize: targetTestHelpers.ArrayBaseBaseSize,
                module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: 0);

            MockMemorySpace.HeapFragment fragment = new() { Name = $"Array = '{string.Join(',', array)}'", Address = address, Data = new byte[size] };
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(ObjectTypeInfo.Fields["m_pMethTab"].Offset), methodTableAddress);
            targetTestHelpers.Write(dest.Slice(ArrayTypeInfo.Fields["m_NumComponents"].Offset), (uint)array.Length);
            return builder.AddHeapFragment(fragment);
        }
    }
}
