// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

internal class MockDescriptors
{
    private static readonly (string Name, DataType Type)[] MethodTableFields = new[]
    {
        (nameof(Data.MethodTable.MTFlags), DataType.uint32),
        (nameof(Data.MethodTable.BaseSize), DataType.uint32),
        (nameof(Data.MethodTable.MTFlags2), DataType.uint32),
        (nameof(Data.MethodTable.EEClassOrCanonMT), DataType.nuint),
        (nameof(Data.MethodTable.Module), DataType.pointer),
        (nameof(Data.MethodTable.ParentMethodTable), DataType.pointer),
        (nameof(Data.MethodTable.NumInterfaces), DataType.uint16),
        (nameof(Data.MethodTable.NumVirtuals), DataType.uint16),
        (nameof(Data.MethodTable.PerInstInfo), DataType.pointer),
    };

    private static readonly (string Name, DataType Type)[] EEClassFields = new[]
    {
        (nameof(Data.EEClass.MethodTable), DataType.pointer),
        (nameof(Data.EEClass.CorTypeAttr), DataType.uint32),
        (nameof(Data.EEClass.NumMethods), DataType.uint16),
        (nameof(Data.EEClass.InternalCorElementType), DataType.uint8),
        (nameof(Data.EEClass.NumNonVirtualSlots), DataType.uint16),
    };

    private static readonly (string Name, DataType Type)[] ArrayClassFields = new[]
    {
        (nameof(Data.ArrayClass.Rank), DataType.uint8),
    };

    private static readonly (string Name, DataType Type)[] ObjectFields = new[]
    {
        ("m_pMethTab", DataType.pointer),
    };

    private static readonly (string Name, DataType Type)[] StringFields = new[]
    {
        ("m_StringLength", DataType.uint32),
        ("m_FirstChar", DataType.uint16),
    };

    private static readonly (string Name, DataType Type)[] ArrayFields = new[]
    {
        ("m_NumComponents", DataType.uint32),
    };

    private static readonly (string Name, DataType Type)[] SyncTableEntryFields = new[]
    {
        (nameof(Data.SyncTableEntry.SyncBlock), DataType.pointer),
    };

    private static readonly (string Name, DataType Type)[] SyncBlockFields = new[]
    {
        (nameof(Data.SyncBlock.InteropInfo), DataType.pointer),
    };

    private static readonly (string Name, DataType Type)[] InteropSyncBlockFields = new[]
    {
        (nameof(Data.InteropSyncBlockInfo.RCW), DataType.pointer),
        (nameof(Data.InteropSyncBlockInfo.CCW), DataType.pointer),
    };

    private static readonly (string, DataType)[] ModuleFields =
    [
        (nameof(Data.Module.Assembly), DataType.pointer),
        (nameof(Data.Module.Flags), DataType.uint32),
        (nameof(Data.Module.Base), DataType.pointer),
        (nameof(Data.Module.LoaderAllocator), DataType.pointer),
        (nameof(Data.Module.ThunkHeap), DataType.pointer),
        (nameof(Data.Module.DynamicMetadata), DataType.pointer),
        (nameof(Data.Module.Path), DataType.pointer),
        (nameof(Data.Module.FileName), DataType.pointer),
        (nameof(Data.Module.FieldDefToDescMap), DataType.pointer),
        (nameof(Data.Module.ManifestModuleReferencesMap), DataType.pointer),
        (nameof(Data.Module.MemberRefToDescMap), DataType.pointer),
        (nameof(Data.Module.MethodDefToDescMap), DataType.pointer),
        (nameof(Data.Module.TypeDefToMethodTableMap), DataType.pointer),
        (nameof(Data.Module.TypeRefToMethodTableMap), DataType.pointer),
        (nameof(Data.Module.MethodDefToILCodeVersioningStateMap), DataType.pointer),
    ];

    private static readonly (string, DataType)[] ExceptionInfoFields =
    [
        (nameof(Data.ExceptionInfo.PreviousNestedInfo), DataType.pointer),
        (nameof(Data.ExceptionInfo.ThrownObject), DataType.pointer),
    ];

    private static readonly (string, DataType)[] ThreadFields =
    [
        (nameof(Data.Thread.Id), DataType.uint32),
        (nameof(Data.Thread.OSId), DataType.nuint),
        (nameof(Data.Thread.State), DataType.uint32),
        (nameof(Data.Thread.PreemptiveGCDisabled), DataType.uint32),
        (nameof(Data.Thread.RuntimeThreadLocals), DataType.pointer),
        (nameof(Data.Thread.Frame), DataType.pointer),
        (nameof(Data.Thread.TEB), DataType.pointer),
        (nameof(Data.Thread.LastThrownObject), DataType.pointer),
        (nameof(Data.Thread.LinkNext), DataType.pointer),
        (nameof(Data.Thread.ExceptionTracker), DataType.pointer),
    ];

    private static readonly (string, DataType)[] ThreadStoreFields =
    [
        (nameof(Data.ThreadStore.ThreadCount), DataType.uint32),
        (nameof(Data.ThreadStore.FirstThreadLink), DataType.pointer),
        (nameof(Data.ThreadStore.UnstartedCount), DataType.uint32),
        (nameof(Data.ThreadStore.BackgroundCount), DataType.uint32),
        (nameof(Data.ThreadStore.PendingCount), DataType.uint32),
        (nameof(Data.ThreadStore.DeadCount), DataType.uint32),
    ];

    public static class RuntimeTypeSystem
    {
        internal const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;
        internal const ulong TestFreeObjectMethodTableAddress = 0x00000000_7a0000a8;

        internal static void AddTypes(TargetTestHelpers targetTestHelpers, Dictionary<DataType, Target.TypeInfo> types)
        {
            var layout = targetTestHelpers.LayoutFields(MethodTableFields);
            types[DataType.MethodTable] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            var eeClassLayout = targetTestHelpers.LayoutFields(EEClassFields);
            layout = eeClassLayout;
            types[DataType.EEClass] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = targetTestHelpers.ExtendLayout(ArrayClassFields, eeClassLayout);
            types[DataType.ArrayClass] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
        }

        internal static readonly (string Name, ulong Value, string? Type)[] Globals =
        [
            (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
            (nameof(Constants.Globals.MethodDescAlignment), 8, nameof(DataType.uint64)),
        ];

        internal static MockMemorySpace.Builder AddGlobalPointers(Target.TypeInfo methodTableTypeInfo, MockMemorySpace.Builder builder)
        {
            return AddFreeObjectMethodTable(methodTableTypeInfo, builder);
        }

        private static MockMemorySpace.Builder AddFreeObjectMethodTable(Target.TypeInfo methodTableTypeInfo, MockMemorySpace.Builder builder)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(globalAddr.Data, TestFreeObjectMethodTableAddress);
            return builder.AddHeapFragments([
                globalAddr,
                new () { Name = "Free Object Method Table", Address = TestFreeObjectMethodTableAddress, Data = new byte[targetTestHelpers.SizeOfTypeInfo(methodTableTypeInfo)] }
            ]);
        }

        internal static MockMemorySpace.Builder AddEEClass(Target.TypeInfo eeClassTypeInfo, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods, ushort numNonVirtualSlots)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"EEClass '{name}'", Address = eeClassPtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(eeClassTypeInfo)] };
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            return builder.AddHeapFragment(eeClassFragment);
        }

        internal static MockMemorySpace.Builder AddArrayClass(Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder, TargetPointer eeClassPtr, string name, TargetPointer canonMTPtr, uint attr, ushort numMethods, ushort numNonVirtualSlots, byte rank)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo eeClassTypeInfo = types[DataType.EEClass];
            Target.TypeInfo arrayClassTypeInfo = types[DataType.ArrayClass];
            int size = (int)arrayClassTypeInfo.Size.Value;
            MockMemorySpace.HeapFragment eeClassFragment = new() { Name = $"ArrayClass '{name}'", Address = eeClassPtr, Data = new byte[size] };
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset), canonMTPtr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            targetTestHelpers.Write(dest.Slice(arrayClassTypeInfo.Fields[nameof(Data.ArrayClass.Rank)].Offset), rank);
            return builder.AddHeapFragment(eeClassFragment);
        }

        internal static MockMemorySpace.Builder AddMethodTable(Target.TypeInfo methodTableTypeInfo, MockMemorySpace.Builder builder, TargetPointer methodTablePtr, string name, TargetPointer eeClassOrCanonMT, uint mtflags, uint mtflags2, uint baseSize,
                                                            TargetPointer module, TargetPointer parentMethodTable, ushort numInterfaces, ushort numVirtuals)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment methodTableFragment = new() { Name = $"MethodTable '{name}'", Address = methodTablePtr, Data = new byte[targetTestHelpers.SizeOfTypeInfo(methodTableTypeInfo)] };
            Span<byte> dest = methodTableFragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.EEClassOrCanonMT)].Offset), eeClassOrCanonMT);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags)].Offset), mtflags);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags2)].Offset), mtflags2);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.BaseSize)].Offset), baseSize);
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.Module)].Offset), module);
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.ParentMethodTable)].Offset), parentMethodTable);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.NumInterfaces)].Offset), numInterfaces);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.NumVirtuals)].Offset), numVirtuals);

            // TODO fill in the rest of the fields
            return builder.AddHeapFragment(methodTableFragment);
        }
    }

    public class Object
    {
        private const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;
        private const ulong TestStringMethodTableAddress = 0x00000000_100000a8;
        internal const ulong TestArrayBoundsZeroGlobalAddress = 0x00000000_100000b0;

        private const ulong TestSyncTableEntriesGlobalAddress = 0x00000000_100000c0;
        private const ulong TestSyncTableEntriesAddress = 0x00000000_f0000000;

        internal const ulong TestObjectToMethodTableUnmask = 0x7;
        internal const ulong TestSyncBlockValueToObjectOffset = sizeof(uint);

        internal readonly Dictionary<DataType, Target.TypeInfo> Types;
        internal readonly MockMemorySpace.Builder Builder;

        internal Object(Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder)
        {
            Types = types;
            Builder = builder;
        }

        internal static void AddTypes(Dictionary<DataType, Target.TypeInfo> types, TargetTestHelpers helpers)
        {
            RuntimeTypeSystem.AddTypes(helpers, types);
            var objectLayout = helpers.LayoutFields(ObjectFields);
            types[DataType.Object] = new Target.TypeInfo() { Fields = objectLayout.Fields, Size = objectLayout.Stride };
            var layout = helpers.ExtendLayout(StringFields, objectLayout);
            types[DataType.String] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = helpers.ExtendLayout(ArrayFields, objectLayout);
            types[DataType.Array] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            Debug.Assert(types[DataType.Array].Size == helpers.ArrayBaseSize);
            layout = helpers.LayoutFields(SyncTableEntryFields);
            types[DataType.SyncTableEntry] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = helpers.LayoutFields(SyncBlockFields);
            types[DataType.SyncBlock] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = helpers.LayoutFields(InteropSyncBlockFields);
            types[DataType.InteropSyncBlockInfo] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
        }

        internal static (string Name, ulong Value, string? Type)[] Globals(TargetTestHelpers helpers) => RuntimeTypeSystem.Globals.Concat(
        [
            (nameof(Constants.Globals.ObjectToMethodTableUnmask), TestObjectToMethodTableUnmask, "uint8"),
            (nameof(Constants.Globals.StringMethodTable), TestStringMethodTableGlobalAddress, null),
            (nameof(Constants.Globals.ArrayBoundsZero), TestArrayBoundsZeroGlobalAddress, null),
            (nameof(Constants.Globals.SyncTableEntries), TestSyncTableEntriesGlobalAddress, null),
            (nameof(Constants.Globals.ObjectHeaderSize), helpers.ObjHeaderSize, "uint32"),
            (nameof(Constants.Globals.SyncBlockValueToObjectOffset), TestSyncBlockValueToObjectOffset, "uint16"),
        ]).ToArray();

        internal static void AddGlobalPointers(Target.TypeInfo methodTableTypeInfo, MockMemorySpace.Builder builder)
        {
            RuntimeTypeSystem.AddGlobalPointers(methodTableTypeInfo, builder);
            AddStringMethodTablePointer(builder);
            AddSyncTableEntriesPointer(builder);

        }

        private static void AddStringMethodTablePointer(MockMemorySpace.Builder builder)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment fragment = new() { Name = "Address of String Method Table", Address = TestStringMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(fragment.Data, TestStringMethodTableAddress);
            builder.AddHeapFragments([
                fragment,
                new () { Name = "String Method Table", Address = TestStringMethodTableAddress, Data = new byte[targetTestHelpers.PointerSize] }
            ]);
        }

        private static MockMemorySpace.Builder AddSyncTableEntriesPointer(MockMemorySpace.Builder builder)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment fragment = new() { Name = "Address of Sync Table Entries", Address = TestSyncTableEntriesGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(fragment.Data, TestSyncTableEntriesAddress);
            return builder.AddHeapFragment(fragment);
        }

        internal static void AddObject(Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder, TargetPointer address, TargetPointer methodTable)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo objectTypeInfo = types[DataType.Object];
            MockMemorySpace.HeapFragment fragment = new() { Name = $"Object : MT = '{methodTable}'", Address = address, Data = new byte[targetTestHelpers.SizeOfTypeInfo(objectTypeInfo)] };
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(objectTypeInfo.Fields["m_pMethTab"].Offset), methodTable);
            builder.AddHeapFragment(fragment);
        }

        internal static void AddObjectWithSyncBlock(Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder, TargetPointer address, TargetPointer methodTable, uint syncBlockIndex, TargetPointer rcw, TargetPointer ccw)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            const uint IsSyncBlockIndexBits = 0x08000000;
            const uint SyncBlockIndexMask = (1 << 26) - 1;
            if ((syncBlockIndex & SyncBlockIndexMask) != syncBlockIndex)
                throw new ArgumentOutOfRangeException(nameof(syncBlockIndex), "Invalid sync block index");

            AddObject(types, builder, address, methodTable);

            // Add the sync table value before the object
            uint syncTableValue = IsSyncBlockIndexBits | syncBlockIndex;
            TargetPointer syncTableValueAddr = address - TestSyncBlockValueToObjectOffset;
            MockMemorySpace.HeapFragment fragment = new() { Name = $"Sync Table Value : index = {syncBlockIndex}", Address = syncTableValueAddr, Data = new byte[sizeof(uint)] };
            targetTestHelpers.Write(fragment.Data, syncTableValue);
            builder.AddHeapFragment(fragment);

            // Add the actual sync block and associated data
            AddSyncBlock(types, builder, syncBlockIndex, rcw, ccw);
        }

        private static void AddSyncBlock(Dictionary<DataType, Target.TypeInfo> types, MockMemorySpace.Builder builder, uint index, TargetPointer rcw, TargetPointer ccw)
        {
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            // Tests write the sync blocks starting at TestSyncBlocksAddress
            const ulong TestSyncBlocksAddress = 0x00000000_e0000000;
            Target.TypeInfo syncBlockTypeInfo = types[DataType.SyncBlock];
            Target.TypeInfo interopSyncBlockTypeInfo = types[DataType.InteropSyncBlockInfo];
            int syncBlockSize = targetTestHelpers.SizeOfTypeInfo(syncBlockTypeInfo);
            int interopSyncBlockInfoSize = targetTestHelpers.SizeOfTypeInfo(interopSyncBlockTypeInfo);
            ulong syncBlockAddr = TestSyncBlocksAddress + index * (ulong)(syncBlockSize + interopSyncBlockInfoSize);

            // Add the sync table entry - pointing at the sync block
            Target.TypeInfo syncTableEntryInfo = types[DataType.SyncTableEntry];
            uint syncTableEntrySize = (uint)targetTestHelpers.SizeOfTypeInfo(syncTableEntryInfo);
            ulong syncTableEntryAddr = TestSyncTableEntriesAddress + index * syncTableEntrySize;
            MockMemorySpace.HeapFragment syncTableEntry = new() { Name = $"SyncTableEntries[{index}]", Address = syncTableEntryAddr, Data = new byte[syncTableEntrySize] };
            Span<byte> syncTableEntryData = syncTableEntry.Data;
            targetTestHelpers.WritePointer(syncTableEntryData.Slice(syncTableEntryInfo.Fields[nameof(Data.SyncTableEntry.SyncBlock)].Offset), syncBlockAddr);

            // Add the sync block - pointing at the interop sync block info
            ulong interopInfoAddr = syncBlockAddr + (ulong)syncBlockSize;
            MockMemorySpace.HeapFragment syncBlock = new() { Name = $"Sync Block", Address = syncBlockAddr, Data = new byte[syncBlockSize] };
            Span<byte> syncBlockData = syncBlock.Data;
            targetTestHelpers.WritePointer(syncBlockData.Slice(syncBlockTypeInfo.Fields[nameof(Data.SyncBlock.InteropInfo)].Offset), interopInfoAddr);

            // Add the interop sync block info
            MockMemorySpace.HeapFragment interopInfo = new() { Name = $"Interop Sync Block Info", Address = interopInfoAddr, Data = new byte[interopSyncBlockInfoSize] };
            Span<byte> interopInfoData = interopInfo.Data;
            targetTestHelpers.WritePointer(interopInfoData.Slice(interopSyncBlockTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.RCW)].Offset), rcw);
            targetTestHelpers.WritePointer(interopInfoData.Slice(interopSyncBlockTypeInfo.Fields[nameof(Data.InteropSyncBlockInfo.CCW)].Offset), ccw);

            builder.AddHeapFragments([syncTableEntry, syncBlock, interopInfo]);
        }

        internal void AddStringObject(TargetPointer address, string value)
        {
            MockMemorySpace.Builder builder = Builder;
            Dictionary<DataType, Target.TypeInfo> types = Types;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo objectTypeInfo = types[DataType.Object];
            Target.TypeInfo stringTypeInfo = types[DataType.String];
            int size = (int)stringTypeInfo.Size.Value + value.Length * sizeof(char);
            MockMemorySpace.HeapFragment fragment = new() { Name = $"String = '{value}'", Address = address, Data = new byte[size] };
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(objectTypeInfo.Fields["m_pMethTab"].Offset), TestStringMethodTableAddress);
            targetTestHelpers.Write(dest.Slice(stringTypeInfo.Fields["m_StringLength"].Offset), (uint)value.Length);
            MemoryMarshal.Cast<char, byte>(value).CopyTo(dest.Slice(stringTypeInfo.Fields["m_FirstChar"].Offset));
            builder.AddHeapFragment(fragment);
        }

        internal void AddArrayObject(TargetPointer address, Array array)
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

            ulong methodTableAddress = (address.Value + (ulong)size + (TestObjectToMethodTableUnmask - 1)) & ~(TestObjectToMethodTableUnmask - 1);
            ulong arrayClassAddress = methodTableAddress + (ulong)targetTestHelpers.SizeOfTypeInfo(types[DataType.MethodTable]);

            uint flags = (uint)(RuntimeTypeSystem_1.WFLAGS_HIGH.HasComponentSize | RuntimeTypeSystem_1.WFLAGS_HIGH.Category_Array) | (uint)array.Length;
            if (isSingleDimensionZeroLowerBound)
                flags |= (uint)RuntimeTypeSystem_1.WFLAGS_HIGH.Category_IfArrayThenSzArray;

            string name = string.Join(',', array);

            RuntimeTypeSystem.AddArrayClass(types, builder, arrayClassAddress, name, methodTableAddress,
                attr: 0, numMethods: 0, numNonVirtualSlots: 0, rank: (byte)array.Rank);
            RuntimeTypeSystem.AddMethodTable(types[DataType.MethodTable], builder, methodTableAddress, name, arrayClassAddress,
                mtflags: flags, mtflags2: default, baseSize: targetTestHelpers.ArrayBaseBaseSize,
                module: TargetPointer.Null, parentMethodTable: TargetPointer.Null, numInterfaces: 0, numVirtuals: 0);

            MockMemorySpace.HeapFragment fragment = new() { Name = $"Array = '{string.Join(',', array)}'", Address = address, Data = new byte[size] };
            Span<byte> dest = fragment.Data;
            targetTestHelpers.WritePointer(dest.Slice(objectTypeInfo.Fields["m_pMethTab"].Offset), methodTableAddress);
            targetTestHelpers.Write(dest.Slice(arrayTypeInfo.Fields["m_NumComponents"].Offset), (uint)array.Length);
            builder.AddHeapFragment(fragment);
        }
    }

    public class Loader
    {
        private const ulong DefaultAllocationRangeStart = 0x0001_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0002_0000;

        private readonly MockMemorySpace.Builder _builder;
        private readonly MockMemorySpace.BumpAllocator _allocator;

        public Loader(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public Loader(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            _builder = builder;
            _allocator = _builder.CreateAllocator(allocationRange.Start, allocationRange.End);
        }

        internal static Dictionary<DataType, Target.TypeInfo> Types(TargetTestHelpers helpers)
        {
            TargetTestHelpers.LayoutResult layout = helpers.LayoutFields(ModuleFields);
            return new()
            {
                [DataType.Module] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride },
            };
        }

        internal TargetPointer AddModule(string? path = null, string? fileName = null)
        {
            TargetTestHelpers helpers = _builder.TargetTestHelpers;
            Target.TypeInfo typeInfo = Types(helpers)[DataType.Module];
            uint size = typeInfo.Size.Value;
            MockMemorySpace.HeapFragment module = _allocator.Allocate(size, "Module");
            _builder.AddHeapFragment(module);

            if (path != null)
            {
                // Path data
                Encoding encoding = helpers.Arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
                ulong pathSize = (ulong)encoding.GetByteCount(path) + sizeof(char);
                MockMemorySpace.HeapFragment pathFragment = _allocator.Allocate(pathSize, $"Module path = {path}");
                helpers.WriteUtf16String(pathFragment.Data, path);
                _builder.AddHeapFragment(pathFragment);

                // Pointer to path
                helpers.WritePointer(
                    module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.Path)].Offset, helpers.PointerSize),
                    pathFragment.Address);
            }

            if (fileName != null)
            {
                // File name data
                Encoding encoding = helpers.Arch.IsLittleEndian ? Encoding.Unicode : Encoding.BigEndianUnicode;
                ulong fileNameSize = (ulong)encoding.GetByteCount(fileName) + sizeof(char);
                MockMemorySpace.HeapFragment fileNameFragment = _allocator.Allocate(fileNameSize, $"Module file name = {fileName}");
                helpers.WriteUtf16String(fileNameFragment.Data, fileName);
                _builder.AddHeapFragment(fileNameFragment);

                // Pointer to file name
                helpers.WritePointer(
                    module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.FileName)].Offset, helpers.PointerSize),
                    fileNameFragment.Address);
            }

            return module.Address;
        }
    }

    public class Thread
    {
        private const ulong DefaultAllocationRangeStart = 0x0003_0000;
        private const ulong DefaultAllocationRangeEnd = 0x0004_0000;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; init; }
        internal (string Name, ulong Value, string? Type)[] Globals { get; init; }

        internal TargetPointer FinalizerThreadAddress { get; init; }
        internal TargetPointer GCThreadAddress { get; init; }

        private readonly MockMemorySpace.Builder _builder;
        private readonly MockMemorySpace.BumpAllocator _allocator;

        private readonly TargetPointer _threadStoreAddress;

        // Most recently added thread. We update its link to the next thread if another thread is added.
        private TargetPointer _previousThread = TargetPointer.Null;

        public Thread(MockMemorySpace.Builder builder)
            : this(builder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        public Thread(MockMemorySpace.Builder builder, (ulong Start, ulong End) allocationRange)
        {
            _builder = builder;
            _allocator = _builder.CreateAllocator(allocationRange.Start, allocationRange.End);

            TargetTestHelpers helpers = builder.TargetTestHelpers;

            Types = GetTypes(helpers);

            // Add thread store and set global to point at it
            MockMemorySpace.HeapFragment threadStoreGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] ThreadStore");
            MockMemorySpace.HeapFragment threadStore = _allocator.Allocate(Types[DataType.ThreadStore].Size.Value, "ThreadStore");
            helpers.WritePointer(threadStoreGlobal.Data, threadStore.Address);
            _builder.AddHeapFragments([threadStoreGlobal, threadStore]);
            _threadStoreAddress = threadStore.Address;

            // Add finalizer thread and set global to point at it
            MockMemorySpace.HeapFragment finalizerThreadGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] Finalizer thread");
            MockMemorySpace.HeapFragment finalizerThread = _allocator.Allocate(Types[DataType.Thread].Size.Value, "Finalizer thread");
            helpers.WritePointer(finalizerThreadGlobal.Data, finalizerThread.Address);
            _builder.AddHeapFragments([finalizerThreadGlobal, finalizerThread]);
            FinalizerThreadAddress = finalizerThread.Address;

            // Add GC thread and set global to point at it
            MockMemorySpace.HeapFragment gcThreadGlobal = _allocator.Allocate((ulong)helpers.PointerSize, "[global pointer] GC thread");
            MockMemorySpace.HeapFragment gcThread = _allocator.Allocate(Types[DataType.Thread].Size.Value, "GC thread");
            helpers.WritePointer(gcThreadGlobal.Data, gcThread.Address);
            _builder.AddHeapFragments([gcThreadGlobal, gcThread]);
            GCThreadAddress = gcThread.Address;

            Globals =
            [
                (nameof(Constants.Globals.ThreadStore), threadStoreGlobal.Address, null),
                (nameof(Constants.Globals.FinalizerThread), finalizerThreadGlobal.Address, null),
                (nameof(Constants.Globals.GCThread), gcThreadGlobal.Address, null),
                (nameof(Constants.Globals.FeatureEHFunclets), 0, null),
            ];
        }

        private static Dictionary<DataType, Target.TypeInfo> GetTypes(TargetTestHelpers helpers)
        {
            TargetTestHelpers.LayoutResult exceptionInfoLayout = helpers.LayoutFields(ExceptionInfoFields);
            TargetTestHelpers.LayoutResult threadLayout = helpers.LayoutFields(ThreadFields);
            TargetTestHelpers.LayoutResult threadStoreLayout = helpers.LayoutFields(ThreadStoreFields);
            return new()
            {
                [DataType.ExceptionInfo] = new Target.TypeInfo() { Fields = exceptionInfoLayout.Fields, Size = exceptionInfoLayout.Stride },
                [DataType.Thread] = new Target.TypeInfo() { Fields = threadLayout.Fields, Size = threadLayout.Stride },
                [DataType.ThreadStore] = new Target.TypeInfo() { Fields = threadStoreLayout.Fields, Size = threadStoreLayout.Stride },
            };
        }

        internal void SetThreadCounts(int threadCount, int unstartedCount, int backgroundCount, int pendingCount, int deadCount)
        {
            TargetTestHelpers helpers = _builder.TargetTestHelpers;
            Target.TypeInfo typeInfo = Types[DataType.ThreadStore];
            Span<byte> data = _builder.BorrowAddressRange(_threadStoreAddress, (int)typeInfo.Size.Value);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.ThreadCount)].Offset),
                threadCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.UnstartedCount)].Offset),
                unstartedCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.BackgroundCount)].Offset),
                backgroundCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.PendingCount)].Offset),
                pendingCount);
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.ThreadStore.DeadCount)].Offset),
                deadCount);
        }

        internal TargetPointer AddThread(uint id, TargetNUInt osId)
        {
            TargetTestHelpers helpers = _builder.TargetTestHelpers;
            Target.TypeInfo typeInfo = Types[DataType.Thread];
            MockMemorySpace.HeapFragment thread = _allocator.Allocate(typeInfo.Size.Value, "Thread");
            Span<byte> data = thread.Data.AsSpan();
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.Thread.Id)].Offset),
                id);
            helpers.WriteNUInt(
                data.Slice(typeInfo.Fields[nameof(Data.Thread.OSId)].Offset),
                osId);
            _builder.AddHeapFragment(thread);

            // Add exception info for the thread
            MockMemorySpace.HeapFragment exceptionInfo = _allocator.Allocate(Types[DataType.ExceptionInfo].Size.Value, "ExceptionInfo");
            _builder.AddHeapFragment(exceptionInfo);
            helpers.WritePointer(
                data.Slice(typeInfo.Fields[nameof(Data.Thread.ExceptionTracker)].Offset),
                exceptionInfo.Address);

            ulong threadLinkOffset = (ulong)typeInfo.Fields[nameof(Data.Thread.LinkNext)].Offset;
            if (_previousThread != TargetPointer.Null)
            {
                // Set the next link for the previously added thread to the newly added one
                helpers.WritePointer(
                    _builder.BorrowAddressRange(_previousThread + threadLinkOffset, helpers.PointerSize),
                    thread.Address + threadLinkOffset);
            }
            else
            {
                // Set the first thread link in the thread store
                ulong firstThreadLinkAddr = _threadStoreAddress + (ulong)Types[DataType.ThreadStore].Fields[nameof(Data.ThreadStore.FirstThreadLink)].Offset;
                helpers.WritePointer(
                    _builder.BorrowAddressRange(firstThreadLinkAddr, helpers.PointerSize),
                    thread.Address + threadLinkOffset);
            }

            _previousThread = thread.Address;
            return thread.Address;
        }
    }
}
