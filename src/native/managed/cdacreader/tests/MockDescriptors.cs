// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.RuntimeTypeSystemHelpers;

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
        (nameof(Data.MethodTable.AuxiliaryData), DataType.pointer),
    };

    private static readonly (string Name, DataType Type)[] EEClassFields = new[]
    {
        (nameof(Data.EEClass.MethodTable), DataType.pointer),
        (nameof(Data.EEClass.CorTypeAttr), DataType.uint32),
        (nameof(Data.EEClass.NumMethods), DataType.uint16),
        (nameof(Data.EEClass.InternalCorElementType), DataType.uint8),
        (nameof(Data.EEClass.NumNonVirtualSlots), DataType.uint16),
    };

    private static readonly (string Name, DataType Type)[] MethodTableAuxiliaryDataFields = new[]
    {
        (nameof(Data.MethodTableAuxiliaryData.LoaderModule), DataType.pointer),
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

    private static readonly (string, DataType)[] AssemblyFields =
    [
        (nameof(Data.Assembly.IsCollectible), DataType.uint8),
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

    public class RuntimeTypeSystem
    {

        internal const ulong TestFreeObjectMethodTableGlobalAddress = 0x00000000_7a0000a0;

        private Dictionary<DataType, Target.TypeInfo>  GetTypes()
        {
            var targetTestHelpers = Builder.TargetTestHelpers;
            Dictionary<DataType, Target.TypeInfo> types = new ();
            var layout = targetTestHelpers.LayoutFields(MethodTableFields);
            types[DataType.MethodTable] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            var eeClassLayout = targetTestHelpers.LayoutFields(EEClassFields);
            layout = eeClassLayout;
            types[DataType.EEClass] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = targetTestHelpers.ExtendLayout(ArrayClassFields, eeClassLayout);
            types[DataType.ArrayClass] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = targetTestHelpers.LayoutFields(MethodTableAuxiliaryDataFields);
            types[DataType.MethodTableAuxiliaryData] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            return types;
        }

        internal static uint GetMethodDescAlignment(TargetTestHelpers helpers) => helpers.Arch.Is64Bit ? 8u : 4u;

        internal static (string Name, ulong Value, string? Type)[] GetGlobals(TargetTestHelpers helpers) =>
        [
            (nameof(Constants.Globals.FreeObjectMethodTable), TestFreeObjectMethodTableGlobalAddress, null),
            (nameof(Constants.Globals.MethodDescAlignment), GetMethodDescAlignment(helpers), nameof(DataType.uint64)),
        ];

        internal readonly MockMemorySpace.Builder Builder;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; init; }

        internal MockMemorySpace.BumpAllocator TypeSystemAllocator { get; set; }

        internal TargetPointer FreeObjectMethodTableAddress { get; private set; }

        internal RuntimeTypeSystem(MockMemorySpace.Builder builder)
        {
            Builder = builder;
            Types = GetTypes();;
        }

        internal void AddGlobalPointers()
        {
            AddFreeObjectMethodTable();
        }

        private void AddFreeObjectMethodTable()
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            MockMemorySpace.HeapFragment freeObjectMethodTableFragment = TypeSystemAllocator.Allocate(methodTableTypeInfo.Size.Value, "Free Object Method Table");
            Builder.AddHeapFragment(freeObjectMethodTableFragment);
            FreeObjectMethodTableAddress = freeObjectMethodTableFragment.Address;

            TargetTestHelpers targetTestHelpers = Builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of Free Object Method Table", Address = TestFreeObjectMethodTableGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
            targetTestHelpers.WritePointer(globalAddr.Data, FreeObjectMethodTableAddress);
            Builder.AddHeapFragment(globalAddr);
        }

        // set the eeClass MethodTable pointer to the canonMT and the canonMT's EEClass pointer to the eeClass
        internal void SetEEClassAndCanonMTRefs(TargetPointer eeClass, TargetPointer canonMT)
        {
            // make eeClass point at the canonMT
            Target.TypeInfo eeClassTypeInfo = Types[DataType.EEClass];
            Span<byte> eeClassBytes = Builder.BorrowAddressRange(eeClass, (int)eeClassTypeInfo.Size.Value);
            Builder.TargetTestHelpers.WritePointer(eeClassBytes.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.MethodTable)].Offset, Builder.TargetTestHelpers.PointerSize), canonMT);

            // and make the canonMT point at the eeClass
            SetMethodTableEEClassOrCanonMTRaw(canonMT, eeClass);
        }


        // for cases when a methodTable needs to point at a canonical method table
        internal void SetMethodTableCanonMT(TargetPointer methodTable, TargetPointer canonMT) => SetMethodTableEEClassOrCanonMTRaw(methodTable, canonMT.Value | 1);

        // NOTE: don't use directly unless you want to write a bogus value into the canonMT field
        internal void SetMethodTableEEClassOrCanonMTRaw(TargetPointer methodTable, TargetPointer eeClassOrCanonMT)
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            Span<byte> methodTableBytes = Builder.BorrowAddressRange(methodTable, (int)methodTableTypeInfo.Size.Value);
            Builder.TargetTestHelpers.WritePointer(methodTableBytes.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.EEClassOrCanonMT)].Offset, Builder.TargetTestHelpers.PointerSize), eeClassOrCanonMT);
        }

        // call SetEEClassAndCanonMTRefs after the EEClass and the MethodTable have been added
        internal TargetPointer AddEEClass(string name, uint attr, ushort numMethods, ushort numNonVirtualSlots)
        {
            Target.TypeInfo eeClassTypeInfo  = Types[DataType.EEClass];
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;

            MockMemorySpace.HeapFragment eeClassFragment = TypeSystemAllocator.Allocate(eeClassTypeInfo.Size.Value, $"EEClass '{name}'");
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            builder.AddHeapFragment(eeClassFragment);
            return eeClassFragment.Address;
        }

        internal TargetPointer AddArrayClass(string name, uint attr, ushort numMethods, ushort numNonVirtualSlots, byte rank)
        {
            Dictionary<DataType, Target.TypeInfo> types = Types;
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            Target.TypeInfo eeClassTypeInfo = types[DataType.EEClass];
            Target.TypeInfo arrayClassTypeInfo = types[DataType.ArrayClass];
            MockMemorySpace.HeapFragment eeClassFragment = TypeSystemAllocator.Allocate (arrayClassTypeInfo.Size.Value, $"ArrayClass '{name}'");
            Span<byte> dest = eeClassFragment.Data;
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.CorTypeAttr)].Offset), attr);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumMethods)].Offset), numMethods);
            targetTestHelpers.Write(dest.Slice(eeClassTypeInfo.Fields[nameof(Data.EEClass.NumNonVirtualSlots)].Offset), numNonVirtualSlots);
            targetTestHelpers.Write(dest.Slice(arrayClassTypeInfo.Fields[nameof(Data.ArrayClass.Rank)].Offset), rank);
            builder.AddHeapFragment(eeClassFragment);
            return eeClassFragment.Address;
        }

        internal TargetPointer AddMethodTable(string name, uint mtflags, uint mtflags2, uint baseSize,
                                                            TargetPointer module, TargetPointer parentMethodTable, ushort numInterfaces, ushort numVirtuals)
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            MockMemorySpace.Builder builder = Builder;
            TargetTestHelpers targetTestHelpers = builder.TargetTestHelpers;
            MockMemorySpace.HeapFragment methodTableFragment = TypeSystemAllocator.Allocate(methodTableTypeInfo.Size.Value,  $"MethodTable '{name}'");
            Span<byte> dest = methodTableFragment.Data;
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags)].Offset), mtflags);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.MTFlags2)].Offset), mtflags2);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.BaseSize)].Offset), baseSize);
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.Module)].Offset), module);
            targetTestHelpers.WritePointer(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.ParentMethodTable)].Offset), parentMethodTable);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.NumInterfaces)].Offset), numInterfaces);
            targetTestHelpers.Write(dest.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.NumVirtuals)].Offset), numVirtuals);

            // TODO fill in the rest of the fields
            builder.AddHeapFragment(methodTableFragment);
            return methodTableFragment.Address;
        }

        internal void SetMethodTableAuxData(TargetPointer methodTablePointer, TargetPointer loaderModule)
        {
            Target.TypeInfo methodTableTypeInfo = Types[DataType.MethodTable];
            Target.TypeInfo auxDataTypeInfo = Types[DataType.MethodTableAuxiliaryData];
            MockMemorySpace.HeapFragment auxDataFragment = TypeSystemAllocator.Allocate(auxDataTypeInfo.Size.Value, "MethodTableAuxiliaryData");
            Span<byte> dest = auxDataFragment.Data;
            Builder.TargetTestHelpers.WritePointer(dest.Slice(auxDataTypeInfo.Fields[nameof(Data.MethodTableAuxiliaryData.LoaderModule)].Offset), loaderModule);
            Builder.AddHeapFragment(auxDataFragment);

            Span<byte> methodTable = Builder.BorrowAddressRange(methodTablePointer, (int)methodTableTypeInfo.Size.Value);
            Builder.TargetTestHelpers.WritePointer(methodTable.Slice(methodTableTypeInfo.Fields[nameof(Data.MethodTable.AuxiliaryData)].Offset), auxDataFragment.Address);
        }
    }

    public class Object
    {
        private const ulong TestStringMethodTableGlobalAddress = 0x00000000_100000a0;

        internal const ulong TestArrayBoundsZeroGlobalAddress = 0x00000000_100000b0;

        private const ulong TestSyncTableEntriesGlobalAddress = 0x00000000_100000c0;
        // The sync table entries address range is manually managed in AddObjectWithSyncBlock
        private const ulong TestSyncTableEntriesAddress = 0x00000000_f0000000;

        internal const ulong TestObjectToMethodTableUnmask = 0x7;
        internal const ulong TestSyncBlockValueToObjectOffset = sizeof(uint);

        internal readonly RuntimeTypeSystem RTSBuilder;
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;

        internal MockMemorySpace.BumpAllocator ManagedObjectAllocator { get; set; }

        internal MockMemorySpace.BumpAllocator SyncBlockAllocator { get; private set; }

        internal TargetPointer TestStringMethodTableAddress { get; private set; }

        internal Dictionary<DataType, Target.TypeInfo> Types { get; init; }

        internal Object(RuntimeTypeSystem rtsBuilder)
        {
            RTSBuilder = rtsBuilder;

            const ulong TestSyncBlocksAddress = 0x00000000_e0000000;
            SyncBlockAllocator = Builder.CreateAllocator(start: TestSyncBlocksAddress, end: TestSyncBlocksAddress + 0x1000);

            Types = GetTypes();
        }

        private Dictionary<DataType, Target.TypeInfo> GetTypes()
        {
            var helpers = Builder.TargetTestHelpers;
            Dictionary<DataType, Target.TypeInfo> types = RTSBuilder.Types;
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
            return types;
        }

        internal static (string Name, ulong Value, string? Type)[] Globals(TargetTestHelpers helpers) => RuntimeTypeSystem.GetGlobals(helpers).Concat(
        [
            (nameof(Constants.Globals.ObjectToMethodTableUnmask), TestObjectToMethodTableUnmask, "uint8"),
            (nameof(Constants.Globals.StringMethodTable), TestStringMethodTableGlobalAddress, null),
            (nameof(Constants.Globals.ArrayBoundsZero), TestArrayBoundsZeroGlobalAddress, null),
            (nameof(Constants.Globals.SyncTableEntries), TestSyncTableEntriesGlobalAddress, null),
            (nameof(Constants.Globals.ObjectHeaderSize), helpers.ObjHeaderSize, "uint32"),
            (nameof(Constants.Globals.SyncBlockValueToObjectOffset), TestSyncBlockValueToObjectOffset, "uint16"),
        ]).ToArray();

        internal void AddGlobalPointers()
        {
            RTSBuilder.AddGlobalPointers();
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
            Dictionary<DataType, Target.TypeInfo> types = new();
            AddTypes(helpers, types);
            return types;
        }

        internal static void AddTypes(TargetTestHelpers helpers, Dictionary<DataType, Target.TypeInfo> types)
        {
            TargetTestHelpers.LayoutResult layout = helpers.LayoutFields(ModuleFields);
            types[DataType.Module] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout= helpers.LayoutFields(AssemblyFields);
            types[DataType.Assembly] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
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

            // add an assembly without any fields set (ie: not collectible)
            MockMemorySpace.HeapFragment assembly = _allocator.Allocate((ulong)helpers.SizeOfTypeInfo(Types(helpers)[DataType.Assembly]), "Assembly");
            _builder.AddHeapFragment(assembly);
            helpers.WritePointer(module.Data.AsSpan().Slice(typeInfo.Fields[nameof(Data.Module.Assembly)].Offset, helpers.PointerSize), assembly.Address);

            return module.Address;
        }
    }

    public class Thread
    {
        const bool UseFunclets = false;
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
                (nameof(Constants.Globals.FeatureEHFunclets), UseFunclets ? 1 : 0, null),
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
            if (UseFunclets)
                throw new NotImplementedException("todo for funclets: allocate the ExceptionInfo separately");
            ulong allocSize = typeInfo.Size.Value + (UseFunclets ? 0 : Types[DataType.ExceptionInfo].Size.Value);
            MockMemorySpace.HeapFragment thread = _allocator.Allocate(allocSize, UseFunclets ? "Thread" : "Thread and ExceptionInfo");
            Span<byte> data = thread.Data.AsSpan();
            helpers.Write(
                data.Slice(typeInfo.Fields[nameof(Data.Thread.Id)].Offset),
                id);
            helpers.WriteNUInt(
                data.Slice(typeInfo.Fields[nameof(Data.Thread.OSId)].Offset),
                osId);
            _builder.AddHeapFragment(thread);

            // Add exception info for the thread
            // Add exception info for the thread
            // TODO: [cdac] Handle when UseFunclets is true - see NotImplementedException thrown above
            TargetPointer exceptionInfoAddress = thread.Address + Types[DataType.ExceptionInfo].Size.Value;
            helpers.WritePointer(
                data.Slice(typeInfo.Fields[nameof(Data.Thread.ExceptionTracker)].Offset),
                exceptionInfoAddress);

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

    internal class MethodDescriptors
    {
        internal const uint TokenRemainderBitCount = 12u; /* see METHOD_TOKEN_REMAINDER_BIT_COUNT*/

        private static readonly (string Name, DataType Type)[] MethodDescFields = new[]
        {
            (nameof(Data.MethodDesc.ChunkIndex), DataType.uint8),
            (nameof(Data.MethodDesc.Slot), DataType.uint16),
            (nameof(Data.MethodDesc.Flags), DataType.uint16),
            (nameof(Data.MethodDesc.Flags3AndTokenRemainder), DataType.uint16),
            (nameof(Data.MethodDesc.EntryPointFlags), DataType.uint8),
            (nameof(Data.MethodDesc.CodeData), DataType.pointer),
        };

        private static readonly (string Name, DataType Type)[] MethodDescChunkFields = new[]
        {
                (nameof(Data.MethodDescChunk.MethodTable), DataType.pointer),
                (nameof(Data.MethodDescChunk.Next), DataType.pointer),
                (nameof(Data.MethodDescChunk.Size), DataType.uint8),
                (nameof(Data.MethodDescChunk.Count), DataType.uint8),
                (nameof(Data.MethodDescChunk.FlagsAndTokenRange), DataType.uint16)
        };

        internal readonly RuntimeTypeSystem RTSBuilder;
        internal readonly Loader LoaderBuilder;

        internal MockMemorySpace.BumpAllocator MethodDescChunkAllocator { get; set; }

        internal TargetTestHelpers TargetTestHelpers => RTSBuilder.Builder.TargetTestHelpers;
        internal Dictionary<DataType, Target.TypeInfo> Types => RTSBuilder.Types;
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;
        internal uint MethodDescAlignment => RuntimeTypeSystem.GetMethodDescAlignment(TargetTestHelpers);

        internal MethodDescriptors(RuntimeTypeSystem rtsBuilder, Loader loaderBuilder)
        {
            RTSBuilder = rtsBuilder;
            LoaderBuilder = loaderBuilder;
            AddTypes();
        }

        private void AddTypes()
        {
            Dictionary<DataType, Target.TypeInfo> types = RTSBuilder.Types;
            TargetTestHelpers targetTestHelpers = Builder.TargetTestHelpers;
            Loader.AddTypes(targetTestHelpers, types);
            var layout = targetTestHelpers.LayoutFields(MethodDescFields);
            types[DataType.MethodDesc] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
            layout = targetTestHelpers.LayoutFields(MethodDescChunkFields);
            types[DataType.MethodDescChunk] = new Target.TypeInfo() { Fields = layout.Fields, Size = layout.Stride };
        }

        internal static (string Name, ulong Value, string? Type)[] Globals(TargetTestHelpers targetTestHelpers) => RuntimeTypeSystem.GetGlobals(targetTestHelpers).Concat([
            (nameof(Constants.Globals.MethodDescTokenRemainderBitCount), TokenRemainderBitCount, "uint8"),
        ]).ToArray();

        internal void AddGlobalPointers()
        {
            RTSBuilder.AddGlobalPointers();

        }

        internal TargetPointer AddMethodDescChunk(TargetPointer methodTable, string name, byte count, byte size, uint tokenRange)
        {
            uint totalAllocSize = Types[DataType.MethodDescChunk].Size.Value;
            totalAllocSize += (uint)(size * MethodDescAlignment);

            MockMemorySpace.HeapFragment methodDescChunk = MethodDescChunkAllocator.Allocate(totalAllocSize, $"MethodDescChunk {name}");
            Span<byte> dest = methodDescChunk.Data;
            TargetTestHelpers.WritePointer(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.MethodTable)].Offset), methodTable);
            TargetTestHelpers.Write(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.Size)].Offset), size);
            TargetTestHelpers.Write(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.Count)].Offset), count);
            TargetTestHelpers.Write(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.FlagsAndTokenRange)].Offset), (ushort)(tokenRange >> (int)TokenRemainderBitCount));
            Builder.AddHeapFragment(methodDescChunk);
            return methodDescChunk.Address;
        }

        internal TargetPointer GetMethodDescAddress(TargetPointer chunkAddress, byte index)
        {
            Target.TypeInfo methodDescChunkTypeInfo = Types[DataType.MethodDescChunk];
            return chunkAddress + methodDescChunkTypeInfo.Size.Value + index * MethodDescAlignment;
        }
        internal Span<byte> BorrowMethodDesc(TargetPointer methodDescChunk, byte index)
        {
            TargetPointer methodDescAddress = GetMethodDescAddress(methodDescChunk, index);
            Target.TypeInfo methodDescTypeInfo = Types[DataType.MethodDesc];
            return Builder.BorrowAddressRange(methodDescAddress, (int)methodDescTypeInfo.Size.Value);
        }

        internal void SetMethodDesc(scoped Span<byte> dest, byte index, ushort slotNum, ushort tokenRemainder)
        {
            Target.TypeInfo methodDescTypeInfo = Types[DataType.MethodDesc];
            TargetTestHelpers.Write(dest.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.ChunkIndex)].Offset), (byte)index);
            TargetTestHelpers.Write(dest.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.Flags3AndTokenRemainder)].Offset), tokenRemainder);
            TargetTestHelpers.Write(dest.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.Slot)].Offset), slotNum);
            // TODO: write more fields
        }

    }
}
