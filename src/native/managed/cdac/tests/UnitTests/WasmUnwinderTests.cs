// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Xunit;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

public class WasmUnwinderTests
{
    // WASM is a 32-bit little-endian target.
    private static readonly MockTarget.Architecture WasmArch = new() { IsLittleEndian = true, Is64Bit = false };

    private const ulong FramesBase = 0x10000;
    private const ulong BlobsBase = 0x20000;
    private const ulong VirtualIpBase = 0x50000;

    // Function table indices 0 and 1 are reserved for the STACK_WALK_INDIRECT_TO_FRAMEPOINTER
    // and TERMINATE_R2R_STACK_WALK sentinels, so real indices start at 2.
    private const uint FuncIndexLeaf = 10;
    private const uint FuncIndexCaller = 11;

    private sealed class FakeWasmR2RInfo : IWasmR2RInfo
    {
        public Dictionary<uint, ulong> VirtualIpBases { get; } = new();
        public Dictionary<uint, ulong> UnwindData { get; } = new();
        public HashSet<uint> Funclets { get; } = new();

        public bool TryGetVirtualIPBase(uint functionTableIndex, out ulong baseVirtualIP)
            => VirtualIpBases.TryGetValue(functionTableIndex, out baseVirtualIP);

        public bool IsFuncletFunctionIndex(uint functionTableIndex)
            => Funclets.Contains(functionTableIndex);

        public bool TryGetUnwindData(uint functionTableIndex, out TargetPointer unwindDataAddress)
        {
            if (UnwindData.TryGetValue(functionTableIndex, out ulong addr))
            {
                unwindDataAddress = new TargetPointer(addr);
                return true;
            }
            unwindDataAddress = TargetPointer.Null;
            return false;
        }
    }

    private static TestPlaceholderTarget CreateTarget(MockMemorySpace.HeapFragment[] fragments)
    {
        TestPlaceholderTarget.Builder builder = new(WasmArch);
        foreach (MockMemorySpace.HeapFragment fragment in fragments)
            builder.MemoryBuilder.AddHeapFragment(fragment);
        return builder.Build();
    }

    // Builds an R2R frame: [0] = function index, [4] = function-local virtual IP / 2.
    private static MockMemorySpace.HeapFragment Frame(ulong address, uint functionIndex, uint localVirtualIPHalf, string name)
    {
        TargetTestHelpers helpers = new(WasmArch);
        byte[] data = new byte[16];
        helpers.Write(data.AsSpan(0, sizeof(uint)), functionIndex);
        helpers.Write(data.AsSpan(4, sizeof(uint)), localVirtualIPHalf);
        return new MockMemorySpace.HeapFragment { Address = address, Data = data, Name = name };
    }

    private static MockMemorySpace.HeapFragment Blob(ulong address, byte[] uleb128, string name)
        => new() { Address = address, Data = uleb128, Name = name };

    [Fact]
    public void TryGetFramePointer_NormalFrame_ReturnsSelf()
    {
        TestPlaceholderTarget target = CreateTarget([Frame(FramesBase, FuncIndexLeaf, 3, "leaf")]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        Assert.True(unwinder.TryGetFramePointer(new TargetPointer(FramesBase), out TargetPointer fp));
        Assert.Equal(FramesBase, fp.Value);
    }

    [Fact]
    public void TryGetFramePointer_BelowFloor_ReturnsFalse()
    {
        TestPlaceholderTarget target = CreateTarget([Frame(FramesBase, FuncIndexLeaf, 3, "leaf")]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        Assert.False(unwinder.TryGetFramePointer(new TargetPointer(0x800), out _));
    }

    [Fact]
    public void TryGetFramePointer_TerminateMarker_ReturnsFalse()
    {
        // A frame whose first word is TERMINATE_R2R_STACK_WALK (1).
        TestPlaceholderTarget target = CreateTarget([Frame(FramesBase, 1, 0, "terminator")]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        Assert.False(unwinder.TryGetFramePointer(new TargetPointer(FramesBase), out _));
    }

    [Fact]
    public void TryGetFramePointer_LocallocIndirect_FollowsSavedFramePointer()
    {
        // localloc frame: first word is STACK_WALK_INDIRECT_TO_FRAMEPOINTER (0), and the real
        // frame base pointer follows one pointer-sized slot later.
        TargetTestHelpers helpers = new(WasmArch);
        ulong indirectSp = FramesBase;
        ulong realFp = FramesBase + 0x100;

        byte[] indirect = new byte[16];
        helpers.Write(indirect.AsSpan(0, sizeof(uint)), StackWalkSentinelIndirect);
        helpers.WritePointer(indirect.AsSpan((int)helpers.PointerSize, helpers.PointerSize), realFp);

        TestPlaceholderTarget target = CreateTarget(
        [
            new MockMemorySpace.HeapFragment { Address = indirectSp, Data = indirect, Name = "indirect" },
            Frame(realFp, FuncIndexLeaf, 3, "realFrame"),
        ]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        Assert.True(unwinder.TryGetFramePointer(new TargetPointer(indirectSp), out TargetPointer fp));
        Assert.Equal(realFp, fp.Value);
    }

    [Fact]
    public void GetEstablishingFramePointerFromTerminator_ReturnsStoredFramePointer()
    {
        TargetTestHelpers helpers = new(WasmArch);
        ulong terminatorSp = FramesBase;
        ulong establishingFp = FramesBase + 0x200;

        byte[] terminator = new byte[16];
        helpers.Write(terminator.AsSpan(0, sizeof(uint)), 1u); // TERMINATE_R2R_STACK_WALK
        helpers.WritePointer(terminator.AsSpan((int)helpers.PointerSize, helpers.PointerSize), establishingFp);

        TestPlaceholderTarget target = CreateTarget(
            [new MockMemorySpace.HeapFragment { Address = terminatorSp, Data = terminator, Name = "terminator" }]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        Assert.Equal(establishingFp, unwinder.GetEstablishingFramePointerFromTerminator(new TargetPointer(terminatorSp)).Value);
    }

    [Fact]
    public void GetVirtualIP_ResolvesBasePlusLocalTimesTwo()
    {
        FakeWasmR2RInfo info = new();
        info.VirtualIpBases[FuncIndexLeaf] = VirtualIpBase;

        TestPlaceholderTarget target = CreateTarget([Frame(FramesBase, FuncIndexLeaf, 3, "leaf")]);
        WasmUnwinder unwinder = new(target, info);

        // baseVirtualIP + (localVirtualIPHalf * 2) == 0x50000 + 6
        Assert.Equal(VirtualIpBase + 6, unwinder.GetVirtualIP(new TargetPointer(FramesBase)).Value);
    }

    [Fact]
    public void TryUnwindOneFrame_AdvancesBySingleByteFrameSize_AndYieldsCallerVirtualIP()
    {
        const uint leafFrameSize = 0x20;
        ulong callerBase = FramesBase + leafFrameSize;

        FakeWasmR2RInfo info = new();
        info.VirtualIpBases[FuncIndexLeaf] = VirtualIpBase;
        info.VirtualIpBases[FuncIndexCaller] = VirtualIpBase;
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 3, "leaf"),
            Frame(callerBase, FuncIndexCaller, 7, "caller"),
            Blob(BlobsBase, [(byte)leafFrameSize], "leafUnwind"), // ULEB128 0x20 == 32
        ]);
        WasmUnwinder unwinder = new(target, info);

        TargetPointer sp = new(FramesBase);
        Assert.True(unwinder.TryUnwindOneFrame(ref sp, out TargetCodePointer ip));
        Assert.Equal(callerBase, sp.Value);
        Assert.Equal(VirtualIpBase + 14, ip.Value); // caller local VIP 7*2
    }

    [Fact]
    public void TryUnwindOneFrame_DecodesMultiByteFrameSize()
    {
        const uint leafFrameSize = 200; // ULEB128: 0xC8 0x01
        ulong callerBase = FramesBase + leafFrameSize;

        FakeWasmR2RInfo info = new();
        info.VirtualIpBases[FuncIndexLeaf] = VirtualIpBase;
        info.VirtualIpBases[FuncIndexCaller] = VirtualIpBase;
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 0, "leaf"),
            Frame(callerBase, FuncIndexCaller, 1, "caller"),
            Blob(BlobsBase, [0xC8, 0x01], "leafUnwind"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        TargetPointer sp = new(FramesBase);
        Assert.True(unwinder.TryUnwindOneFrame(ref sp, out _));
        Assert.Equal(callerBase, sp.Value);
    }

    [Fact]
    public void TryUnwindOneFrame_AtTerminator_ReturnsFalse()
    {
        TestPlaceholderTarget target = CreateTarget([Frame(FramesBase, 1, 0, "terminator")]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        TargetPointer sp = new(FramesBase);
        Assert.False(unwinder.TryUnwindOneFrame(ref sp, out _));
        Assert.Equal(TargetPointer.Null, sp);
    }

    [Fact]
    public void TryGetFramePointer_LocallocToBelowFloor_ReturnsFalse()
    {
        // localloc frame whose saved real frame pointer is below the linear-stack floor.
        TargetTestHelpers helpers = new(WasmArch);
        ulong indirectSp = FramesBase;

        byte[] indirect = new byte[16];
        helpers.Write(indirect.AsSpan(0, sizeof(uint)), StackWalkSentinelIndirect);
        helpers.WritePointer(indirect.AsSpan((int)helpers.PointerSize, helpers.PointerSize), 0x10ul); // below LinearStackFloor

        TestPlaceholderTarget target = CreateTarget(
            [new MockMemorySpace.HeapFragment { Address = indirectSp, Data = indirect, Name = "indirect" }]);
        WasmUnwinder unwinder = new(target, new FakeWasmR2RInfo());

        Assert.False(unwinder.TryGetFramePointer(new TargetPointer(indirectSp), out _));
    }

    [Fact]
    public void TryUnwindOneFrame_ZeroFrameSize_TerminatesCleanly()
    {
        FakeWasmR2RInfo info = new();
        info.VirtualIpBases[FuncIndexLeaf] = VirtualIpBase;
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 3, "leaf"),
            Blob(BlobsBase, [0x00], "zeroFrameSize"), // ULEB128 0 -> no progress
        ]);
        WasmUnwinder unwinder = new(target, info);

        TargetPointer sp = new(FramesBase);
        Assert.False(unwinder.TryUnwindOneFrame(ref sp, out _));
        Assert.Equal(TargetPointer.Null, sp);
    }

    [Fact]
    public void TryUnwindOneFrame_MalformedUleb128_Throws()
    {
        FakeWasmR2RInfo info = new();
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 3, "leaf"),
            // 5 continuation bytes with no terminator -> exceeds the 5-byte uint32 ULEB128 limit.
            Blob(BlobsBase, [0x80, 0x80, 0x80, 0x80, 0x80], "malformed"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        TargetPointer sp = new(FramesBase);
        Assert.Throws<InvalidOperationException>(() => unwinder.TryUnwindOneFrame(ref sp, out _));
    }

    private const uint StackWalkSentinelIndirect = 0;
}
