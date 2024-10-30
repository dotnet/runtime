// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

internal class MockDescriptors
{
    private static readonly Target.TypeInfo MethodTableTypeInfo = new()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
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
        Fields = new Dictionary<string, Target.FieldInfo> {
            { nameof (Data.EEClass.MethodTable), new () { Offset = 8, Type = DataType.pointer}},
            { nameof (Data.EEClass.CorTypeAttr), new () { Offset = 16, Type = DataType.uint32}},
            { nameof (Data.EEClass.NumMethods), new () { Offset = 20, Type = DataType.uint16}},
            { nameof (Data.EEClass.InternalCorElementType), new () { Offset = 22, Type = DataType.uint8}},
            { nameof (Data.EEClass.NumNonVirtualSlots), new () { Offset = 24, Type = DataType.uint16}},
        }
    };

    private static readonly Target.TypeInfo ArrayClassTypeInfo = new Target.TypeInfo()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { nameof (Data.ArrayClass.Rank), new () { Offset = 0x70, Type = DataType.uint8}},
        }
    };

    private static readonly Target.TypeInfo ObjectTypeInfo = new()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { "m_pMethTab", new() { Offset = 0, Type = DataType.pointer} },
        }
    };

    private static readonly Target.TypeInfo StringTypeInfo = new Target.TypeInfo()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { "m_StringLength", new() { Offset = 0x8, Type = DataType.uint32} },
            { "m_FirstChar", new() { Offset = 0xc, Type = DataType.uint16} },
        }
    };

    private static readonly Target.TypeInfo ArrayTypeInfo = new Target.TypeInfo()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { "m_NumComponents", new() { Offset = 0x8, Type = DataType.uint32} },
        },
    };

    private static readonly Target.TypeInfo SyncTableEntryInfo = new Target.TypeInfo()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { nameof(Data.SyncTableEntry.SyncBlock), new() { Offset = 0, Type = DataType.pointer} },
        },
    };

    private static readonly Target.TypeInfo SyncBlockTypeInfo = new Target.TypeInfo()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { nameof(Data.SyncBlock.InteropInfo), new() { Offset = 0, Type = DataType.pointer} },
        },
    };

    private static readonly Target.TypeInfo InteropSyncBlockTypeInfo = new Target.TypeInfo()
    {
        Fields = new Dictionary<string, Target.FieldInfo> {
            { nameof(Data.InteropSyncBlockInfo.RCW), new() { Offset = 0, Type = DataType.pointer} },
            { nameof(Data.InteropSyncBlockInfo.CCW), new() { Offset = 0x8, Type = DataType.pointer} },
        },
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
