// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.UnitTests;

public class DacStreamsTests
{
    private delegate MockMemorySpace.Builder ConfigureContextBuilder(MockMemorySpace.Builder builder);

    const ulong TestMiniMetaDataBuffGlobalAddress = 0x00000000_200000a0;
    const ulong TestMiniMetaDataBuffGlobalMaxSize = 0x00000000_20000000;
    const ulong TestMiniMetaDataBuffAddress = 0x00000000_100000a0;

    const uint MiniMetadataSignature = 0x6d727473;
    const uint EENameStreamSignature = 0x614e4545;

    const uint MiniMetaDataStreamsHeaderSize = 12;

    private static readonly Dictionary<DataType, Target.TypeInfo> DacStreamsTypes =
    [
    ];

    private static readonly (string Name, ulong Value, string? Type)[] DacStreamsGlobals =
    [
        (nameof(Constants.Globals.MiniMetaDataBuffAddress), TestMiniMetaDataBuffGlobalAddress, null),
        (nameof(Constants.Globals.MiniMetaDataBuffMaxSize), TestMiniMetaDataBuffGlobalMaxSize, null),
    ];

    private static unsafe void DacStreamsContractHelper(MockTarget.Architecture arch, ConfigureContextBuilder configure, Action<Target> testCase)
    {
        TargetTestHelpers targetTestHelpers = new(arch);
        string metadataTypesJson = TargetTestHelpers.MakeTypesJson(DacStreamsTypes);
        string metadataGlobalsJson = TargetTestHelpers.MakeGlobalsJson(DacStreamsGlobals);
        byte[] json = Encoding.UTF8.GetBytes($$"""
        {
            "version": 0,
            "baseline": "empty",
            "contracts": {
                "{{nameof(Contracts.DacStreams)}}": 1
            },
            "types": { {{metadataTypesJson}} },
            "globals": { {{metadataGlobalsJson}} }
        }
        """);
        Span<byte> descriptor = stackalloc byte[targetTestHelpers.ContractDescriptorSize];
        targetTestHelpers.ContractDescriptorFill(descriptor, json.Length, DacStreamsGlobals.Length);

        int pointerSize = targetTestHelpers.PointerSize;
        Span<byte> pointerData = stackalloc byte[DacStreamsGlobals.Length * pointerSize];
        for (int i = 0; i < DacStreamsGlobals.Length; i++)
        {
            var (_, value, _) = DacStreamsGlobals[i];
            targetTestHelpers.WritePointer(pointerData.Slice(i * pointerSize), value);
        }

        fixed (byte* jsonPtr = json)
        {
            MockMemorySpace.Builder builder = new();

            builder = builder.SetDescriptor(descriptor)
                    .SetJson(json)
                    .SetPointerData(pointerData);

            if (configure != null)
            {
                builder = configure(builder);
            }

            using MockMemorySpace.ReadContext context = builder.Create();

            bool success = MockMemorySpace.TryCreateTarget(&context, out Target? target);
            Assert.True(success);

            testCase(target);
        }
        GC.KeepAlive(json);
    }

    MockMemorySpace.Builder AddMiniMetaDataBuffMaxSize(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, uint maxSize)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of MiniMetaDataBuffMaxSize", Address = TestMiniMetaDataBuffGlobalMaxSize, Data = new byte[4] };
        targetTestHelpers.Write(globalAddr.Data, maxSize);
        return builder.AddHeapFragment(globalAddr);
    }

    MockMemorySpace.Builder AddMiniMetaDataBuffAddress(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, ulong pointer)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "Address of MiniMetaDataBuffAddress", Address = TestMiniMetaDataBuffGlobalAddress, Data = new byte[targetTestHelpers.PointerSize] };
        targetTestHelpers.WritePointer(globalAddr.Data, pointer);
        return builder.AddHeapFragment(globalAddr);
    }

    private class CurrentPointer
    {
        public ulong Pointer;
    }

    MockMemorySpace.Builder AddMiniMetaDataStreamsHeader(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, uint totalSizeOtherThanStreamsHeader, uint countStreams, CurrentPointer currentPointer)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "MiniMetaDataStreamsHeader", Address = currentPointer.Pointer, Data = new byte[MiniMetaDataStreamsHeaderSize] };
        targetTestHelpers.Write(globalAddr.Data.AsSpan().Slice(0, 4), MiniMetadataSignature);
        targetTestHelpers.Write(globalAddr.Data.AsSpan().Slice(4, 4), totalSizeOtherThanStreamsHeader + MiniMetaDataStreamsHeaderSize);
        targetTestHelpers.Write(globalAddr.Data.AsSpan().Slice(8, 4), countStreams);
        currentPointer.Pointer += 12;
        return builder.AddHeapFragment(globalAddr);
    }

    MockMemorySpace.Builder AddEENameStreamHeader(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, uint countEntries, CurrentPointer currentPointer)
    {
        MockMemorySpace.HeapFragment globalAddr = new() { Name = "EEStreamHeader", Address = currentPointer.Pointer, Data = new byte[8] };
        targetTestHelpers.Write(globalAddr.Data.AsSpan().Slice(0, 4), EENameStreamSignature);
        targetTestHelpers.Write(globalAddr.Data.AsSpan().Slice(4, 4), countEntries);
        currentPointer.Pointer += 8;
        return builder.AddHeapFragment(globalAddr);
    }


    MockMemorySpace.Builder AddEENameStream(TargetTestHelpers targetTestHelpers, MockMemorySpace.Builder builder, List<(ulong Pointer, string Name)> names, CurrentPointer currentPointer)
    {
        builder = AddEENameStreamHeader(targetTestHelpers, builder, checked((uint)names.Count), currentPointer);

        for (int i = 0; i < names.Count; i++)
        {
            int byteCountWithoutNullTerminator = Encoding.UTF8.GetByteCount(names[i].Name);
            int entrySize = byteCountWithoutNullTerminator + 1 + targetTestHelpers.PointerSize;
            MockMemorySpace.HeapFragment entryAddr = new()
            {
                Name = $"EEStreamEntry{i}", Address = currentPointer.Pointer, Data = new byte[byteCountWithoutNullTerminator + 1 + targetTestHelpers.PointerSize]
            };
            targetTestHelpers.WritePointer(entryAddr.Data.AsSpan().Slice(0, targetTestHelpers.PointerSize), names[i].Pointer);
            Encoding.UTF8.TryGetBytes(names[i].Name.AsSpan(), entryAddr.Data.AsSpan().Slice(targetTestHelpers.PointerSize, byteCountWithoutNullTerminator), out _);
            targetTestHelpers.Write(entryAddr.Data.AsSpan().Slice(byteCountWithoutNullTerminator + targetTestHelpers.PointerSize, 1), (byte)0);
            currentPointer.Pointer += (ulong)entrySize;
            builder = builder.AddHeapFragment(entryAddr);
        }
        return builder;
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void DacStreamValues(MockTarget.Architecture arch)
    {
        TargetTestHelpers targetTestHelpers = new(arch);

        DacStreamsContractHelper(arch,
        (builder) =>
        {
            // Test normal non-error behavior

            List<(ulong Pointer, string Name)> values = [((ulong)0x1234, "Type1"), ((ulong)0x1238, "Type2")];
            builder = AddMiniMetaDataBuffAddress(targetTestHelpers, builder, TestMiniMetaDataBuffAddress);
            builder = AddMiniMetaDataBuffMaxSize(targetTestHelpers, builder, 0x10000);
            CurrentPointer currentPointer = new();
            ulong eeNameStreamStart = TestMiniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            currentPointer.Pointer = TestMiniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            builder = AddEENameStream(targetTestHelpers, builder, values, currentPointer);
            uint eeNameStreamSize = checked((uint)(currentPointer.Pointer - eeNameStreamStart));

            currentPointer.Pointer = TestMiniMetaDataBuffAddress;
            builder = AddMiniMetaDataStreamsHeader(targetTestHelpers, builder, eeNameStreamSize, 1, currentPointer);
            return builder;
        },
        (target) =>
        {
            Contracts.IDacStreams dacStreamsContract = target.Contracts.DacStreams;
            Assert.NotNull(dacStreamsContract);
            Assert.Null(dacStreamsContract.StringFromEEAddress(0));
            Assert.Equal("Type1", dacStreamsContract.StringFromEEAddress(0x1234));
            Assert.Equal("Type2", dacStreamsContract.StringFromEEAddress(0x1238));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void DacStreamValues_TruncatedTotalSize(MockTarget.Architecture arch)
    {
        // Test behavior if TotalSize isn't big enough to hold the last entry

        TargetTestHelpers targetTestHelpers = new(arch);

        DacStreamsContractHelper(arch,
        (builder) =>
        {
            List<(ulong Pointer, string Name)> values = [((ulong)0x1234, "Type1"), ((ulong)0x1238, "Type2")];
            builder = AddMiniMetaDataBuffAddress(targetTestHelpers, builder, TestMiniMetaDataBuffAddress);
            builder = AddMiniMetaDataBuffMaxSize(targetTestHelpers, builder, 0x10000);
            CurrentPointer currentPointer = new();
            ulong eeNameStreamStart = TestMiniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            currentPointer.Pointer = TestMiniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            builder = AddEENameStream(targetTestHelpers, builder, values, currentPointer);
            uint eeNameStreamSize = checked((uint)(currentPointer.Pointer - eeNameStreamStart));

            currentPointer.Pointer = TestMiniMetaDataBuffAddress;
            builder = AddMiniMetaDataStreamsHeader(targetTestHelpers, builder, eeNameStreamSize - 2, 1, currentPointer);
            return builder;
        },
        (target) =>
        {
            Contracts.IDacStreams dacStreamsContract = target.Contracts.DacStreams;
            Assert.NotNull(dacStreamsContract);
            Assert.Null(dacStreamsContract.StringFromEEAddress(0));
            Assert.Equal("Type1", dacStreamsContract.StringFromEEAddress(0x1234));
            Assert.Null(dacStreamsContract.StringFromEEAddress(0x1238));
        });
    }

    [Theory]
    [ClassData(typeof(MockTarget.StdArch))]
    public void DacStreamValues_TruncatedBuffMaxSize(MockTarget.Architecture arch)
    {
        // Test behavior if MaxSize global is smaller than TotalSize
        TargetTestHelpers targetTestHelpers = new(arch);

        DacStreamsContractHelper(arch,
        (builder) =>
        {
            List<(ulong Pointer, string Name)> values = [((ulong)0x1234, "Type1"), ((ulong)0x1238, "Type2")];
            builder = AddMiniMetaDataBuffAddress(targetTestHelpers, builder, TestMiniMetaDataBuffAddress);
            builder = AddMiniMetaDataBuffMaxSize(targetTestHelpers, builder, (uint)(0x20 + targetTestHelpers.PointerSize * 2 - 1));
            CurrentPointer currentPointer = new();
            ulong eeNameStreamStart = TestMiniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            currentPointer.Pointer = TestMiniMetaDataBuffAddress + MiniMetaDataStreamsHeaderSize;
            builder = AddEENameStream(targetTestHelpers, builder, values, currentPointer);
            uint eeNameStreamSize = checked((uint)(currentPointer.Pointer - eeNameStreamStart));

            currentPointer.Pointer = TestMiniMetaDataBuffAddress;
            builder = AddMiniMetaDataStreamsHeader(targetTestHelpers, builder, eeNameStreamSize, 1, currentPointer);
            return builder;
        },
        (target) =>
        {
            Contracts.IDacStreams dacStreamsContract = target.Contracts.DacStreams;
            Assert.NotNull(dacStreamsContract);
            Assert.Null(dacStreamsContract.StringFromEEAddress(0));
            Assert.Null(dacStreamsContract.StringFromEEAddress(0x1234));
            Assert.Null(dacStreamsContract.StringFromEEAddress(0x1238));
        });
    }
}
