// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

internal partial class MockDescriptors
{
    public class MethodDescriptors
    {
        internal const byte TokenRemainderBitCount = 12; /* see METHOD_TOKEN_REMAINDER_BIT_COUNT*/

        private static readonly TypeFields MethodDescFields = new TypeFields()
        {
            DataType = DataType.MethodDesc,
            Fields =
            [
                new(nameof(Data.MethodDesc.ChunkIndex), DataType.uint8),
                new(nameof(Data.MethodDesc.Slot), DataType.uint16),
                new(nameof(Data.MethodDesc.Flags), DataType.uint16),
                new(nameof(Data.MethodDesc.Flags3AndTokenRemainder), DataType.uint16),
                new(nameof(Data.MethodDesc.EntryPointFlags), DataType.uint8),
                new(nameof(Data.MethodDesc.CodeData), DataType.pointer),
                new(nameof(Data.MethodDesc.GCCoverageInfo), DataType.pointer),
            ]
        };

        private static readonly TypeFields MethodDescChunkFields = new TypeFields()
        {
            DataType = DataType.MethodDescChunk,
            Fields =
            [
                new(nameof(Data.MethodDescChunk.MethodTable), DataType.pointer),
                new(nameof(Data.MethodDescChunk.Next), DataType.pointer),
                new(nameof(Data.MethodDescChunk.Size), DataType.uint8),
                new(nameof(Data.MethodDescChunk.Count), DataType.uint8),
                new(nameof(Data.MethodDescChunk.FlagsAndTokenRange), DataType.uint16)
            ]
        };

        private static readonly TypeFields InstantiatedMethodDescFields = new TypeFields()
        {
            DataType = DataType.InstantiatedMethodDesc,
            Fields =
            [
                new(nameof(Data.InstantiatedMethodDesc.PerInstInfo), DataType.pointer),
                new(nameof(Data.InstantiatedMethodDesc.NumGenericArgs), DataType.uint16),
                new(nameof(Data.InstantiatedMethodDesc.Flags2), DataType.uint16),
            ],
            BaseTypeFields = MethodDescFields
        };

        private static readonly TypeFields StoredSigMethodDescFields = new TypeFields()
        {
            DataType = DataType.StoredSigMethodDesc,
            Fields =
            [
                new(nameof(Data.StoredSigMethodDesc.Sig), DataType.pointer),
                new(nameof(Data.StoredSigMethodDesc.cSig), DataType.uint32),
                new(nameof(Data.StoredSigMethodDesc.ExtendedFlags), DataType.uint32),
            ],
            BaseTypeFields = MethodDescFields
        };

        private static readonly TypeFields DynamicMethodDescFields = new TypeFields()
        {
            DataType = DataType.DynamicMethodDesc,
            Fields =
            [
                new(nameof(Data.DynamicMethodDesc.MethodName), DataType.pointer),
            ],
            BaseTypeFields = StoredSigMethodDescFields
        };

        private const ulong DefaultAllocationRangeStart = 0x2000_2000;
        private const ulong DefaultAllocationRangeEnd = 0x2000_3000;

        internal readonly RuntimeTypeSystem RTSBuilder;
        internal readonly Loader LoaderBuilder;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        private readonly MockMemorySpace.BumpAllocator _allocator;

        internal TargetTestHelpers TargetTestHelpers => RTSBuilder.Builder.TargetTestHelpers;
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;
        internal uint MethodDescAlignment => RuntimeTypeSystem.GetMethodDescAlignment(TargetTestHelpers);

        internal MethodDescriptors(RuntimeTypeSystem rtsBuilder, Loader loaderBuilder)
            : this(rtsBuilder, loaderBuilder, (DefaultAllocationRangeStart, DefaultAllocationRangeEnd))
        { }

        internal MethodDescriptors(RuntimeTypeSystem rtsBuilder, Loader loaderBuilder, (ulong Start, ulong End) allocationRange)
        {
            RTSBuilder = rtsBuilder;
            LoaderBuilder = loaderBuilder;
            _allocator = Builder.CreateAllocator(allocationRange.Start, allocationRange.End);
            Types = GetTypes();

            // Add dummy MethodDescSizeTable. Sizes will be incorrect, but we don't use it in tests.
            MockMemorySpace.HeapFragment methodDescSizeTable = _allocator.Allocate(0x100, "MethodDescSizeTable");
            Builder.AddHeapFragment(methodDescSizeTable);

            Globals = rtsBuilder.Globals.Concat(
            [
                new(nameof(Constants.Globals.MethodDescTokenRemainderBitCount), TokenRemainderBitCount),
                new(nameof(Constants.Globals.MethodDescSizeTable), methodDescSizeTable.Address),
            ]).ToArray();
        }

        private Dictionary<DataType, Target.TypeInfo> GetTypes()
        {
            Dictionary<DataType, Target.TypeInfo> types = GetTypesForTypeFields(
                TargetTestHelpers,
                [
                    MethodDescFields,
                    MethodDescChunkFields,
                    InstantiatedMethodDescFields,
                    StoredSigMethodDescFields,
                    DynamicMethodDescFields,
                ]);
            types[DataType.NonVtableSlot] = new Target.TypeInfo() { Size = (uint)TargetTestHelpers.PointerSize };
            types[DataType.MethodImpl] = new Target.TypeInfo() { Size = (uint)TargetTestHelpers.PointerSize * 2 };
            types[DataType.NativeCodeSlot] = new Target.TypeInfo() { Size = (uint)TargetTestHelpers.PointerSize };
            types[DataType.ArrayMethodDesc] = new Target.TypeInfo() { Size = types[DataType.StoredSigMethodDesc].Size.Value };
            types[DataType.FCallMethodDesc] = new Target.TypeInfo() { Size = types[DataType.MethodDesc].Size.Value + (uint)TargetTestHelpers.PointerSize };
            types[DataType.PInvokeMethodDesc] = new Target.TypeInfo() { Size = types[DataType.MethodDesc].Size.Value + (uint)TargetTestHelpers.PointerSize };
            types[DataType.EEImplMethodDesc] = new Target.TypeInfo() { Size = types[DataType.StoredSigMethodDesc].Size.Value };
            types[DataType.CLRToCOMCallMethodDesc] = new Target.TypeInfo() { Size = types[DataType.MethodDesc].Size.Value + (uint)TargetTestHelpers.PointerSize };
            types = types
                .Concat(RTSBuilder.Types)
                .Concat(LoaderBuilder.Types)
                .ToDictionary();
            return types;
        }

        internal TargetPointer AddMethodDescChunk(TargetPointer methodTable, string name, byte count, byte size, uint tokenRange)
        {
            uint totalAllocSize = Types[DataType.MethodDescChunk].Size.Value;
            totalAllocSize += (uint)(size * MethodDescAlignment);

            MockMemorySpace.HeapFragment methodDescChunk = _allocator.Allocate(totalAllocSize, $"MethodDescChunk {name}");
            Span<byte> dest = methodDescChunk.Data;
            TargetTestHelpers.WritePointer(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.MethodTable)].Offset), methodTable);
            TargetTestHelpers.Write(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.Size)].Offset), size);
            TargetTestHelpers.Write(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.Count)].Offset), count);
            TargetTestHelpers.Write(dest.Slice(Types[DataType.MethodDescChunk].Fields[nameof(Data.MethodDescChunk.FlagsAndTokenRange)].Offset), (ushort)(tokenRange >> (int)TokenRemainderBitCount));
            Builder.AddHeapFragment(methodDescChunk);
            return methodDescChunk.Address;
        }

        private TargetPointer GetMethodDescAddress(TargetPointer chunkAddress, byte index)
        {
            Target.TypeInfo methodDescChunkTypeInfo = Types[DataType.MethodDescChunk];
            return chunkAddress + methodDescChunkTypeInfo.Size.Value + index * MethodDescAlignment;
        }

        internal TargetPointer SetMethodDesc(TargetPointer methodDescChunk, byte index, ushort slotNum, ushort flags, ushort tokenRemainder, ushort flags3 = 0)
        {
            TargetPointer methodDesc = GetMethodDescAddress(methodDescChunk, index);
            Target.TypeInfo methodDescTypeInfo = Types[DataType.MethodDesc];
            Span<byte> data = Builder.BorrowAddressRange(methodDesc, (int)methodDescTypeInfo.Size.Value);
            TargetTestHelpers.Write(data.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.ChunkIndex)].Offset), index);
            TargetTestHelpers.Write(data.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.Flags)].Offset), flags);
            TargetTestHelpers.Write(data.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.Flags3AndTokenRemainder)].Offset), (ushort)(tokenRemainder | flags3));
            TargetTestHelpers.Write(data.Slice(methodDescTypeInfo.Fields[nameof(Data.MethodDesc.Slot)].Offset), slotNum);
            return methodDesc;
        }

        internal void SetDynamicMethodDesc(TargetPointer methodDesc, uint extendedFlags)
        {
            Target.TypeInfo storedSigInfo = Types[DataType.StoredSigMethodDesc];
            Span<byte> data = Builder.BorrowAddressRange(methodDesc, (int)storedSigInfo.Size.Value);
            TargetTestHelpers.Write(data.Slice(storedSigInfo.Fields[nameof(Data.StoredSigMethodDesc.ExtendedFlags)].Offset), extendedFlags);
        }

        internal void SetInstantiatedMethodDesc(TargetPointer methodDesc, ushort flags, TargetPointer[] typesArgs)
        {
            Target.TypeInfo typeInfo = Types[DataType.InstantiatedMethodDesc];
            Span<byte> data = Builder.BorrowAddressRange(methodDesc, (int)typeInfo.Size.Value);
            TargetTestHelpers.Write(data.Slice(typeInfo.Fields[nameof(Data.InstantiatedMethodDesc.Flags2)].Offset), flags);
            TargetTestHelpers.Write(data.Slice(typeInfo.Fields[nameof(Data.InstantiatedMethodDesc.NumGenericArgs)].Offset), (ushort)typesArgs.Length);
            if (typesArgs.Length > 0)
            {
                MockMemorySpace.HeapFragment fragment = _allocator.Allocate((ulong)(typesArgs.Length * TargetTestHelpers.PointerSize), "InstantiatedMethodDesc type args");
                Builder.AddHeapFragment(fragment);
                TargetTestHelpers.WritePointer(data.Slice(typeInfo.Fields[nameof(Data.InstantiatedMethodDesc.PerInstInfo)].Offset), fragment.Address);
                for (int i = 0; i < typesArgs.Length; i++)
                {
                    Span<byte> span = fragment.Data.AsSpan().Slice(i * TargetTestHelpers.PointerSize);
                    TargetTestHelpers.WritePointer(span, typesArgs[i]);
                }
            }
        }
    }
}
