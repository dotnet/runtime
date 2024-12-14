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
        internal const uint TokenRemainderBitCount = 12u; /* see METHOD_TOKEN_REMAINDER_BIT_COUNT*/

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

        internal readonly RuntimeTypeSystem RTSBuilder;
        internal readonly Loader LoaderBuilder;

        internal Dictionary<DataType, Target.TypeInfo> Types { get; }
        internal (string Name, ulong Value)[] Globals { get; }

        internal MockMemorySpace.BumpAllocator MethodDescChunkAllocator { get; set; }

        internal TargetTestHelpers TargetTestHelpers => RTSBuilder.Builder.TargetTestHelpers;
        internal MockMemorySpace.Builder Builder => RTSBuilder.Builder;
        internal uint MethodDescAlignment => RuntimeTypeSystem.GetMethodDescAlignment(TargetTestHelpers);

        internal MethodDescriptors(RuntimeTypeSystem rtsBuilder, Loader loaderBuilder)
        {
            RTSBuilder = rtsBuilder;
            LoaderBuilder = loaderBuilder;
            Types = GetTypes();
            Globals = rtsBuilder.Globals.Concat(
            [
                new(nameof(Constants.Globals.MethodDescTokenRemainderBitCount), TokenRemainderBitCount),
            ]).ToArray();
        }

        private Dictionary<DataType, Target.TypeInfo> GetTypes()
        {
            Dictionary<DataType, Target.TypeInfo> types = GetTypesForTypeFields(
                TargetTestHelpers,
                [
                    MethodDescFields,
                    MethodDescChunkFields,
                ]);
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
