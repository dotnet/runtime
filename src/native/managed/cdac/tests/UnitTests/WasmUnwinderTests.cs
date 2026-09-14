// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;
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

        // A function index is "known" here if it has unwind data or a virtual IP base registered.
        public bool TryIsFunclet(uint functionTableIndex, out bool isFunclet)
        {
            isFunclet = Funclets.Contains(functionTableIndex);
            return UnwindData.ContainsKey(functionTableIndex)
                || VirtualIpBases.ContainsKey(functionTableIndex)
                || isFunclet;
        }
    }

    private static TestPlaceholderTarget CreateTarget(
        MockMemorySpace.HeapFragment[] fragments,
        IExecutionManager? executionManager = null,
        IGCInfo? gcInfo = null)
    {
        TestPlaceholderTarget.Builder builder = new(WasmArch);
        foreach (MockMemorySpace.HeapFragment fragment in fragments)
            builder.MemoryBuilder.AddHeapFragment(fragment);
        if (executionManager is not null)
            builder.AddMockContract(executionManager);
        if (gcInfo is not null)
            builder.AddMockContract(gcInfo);
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
    public void GCInfo_WasmUsesPlatformDecoder()
    {
        TestPlaceholderTarget target = new TestPlaceholderTarget.Builder(WasmArch)
            .AddGlobalStrings((Constants.Globals.Architecture, "wasm"))
            .AddContract<IRuntimeInfo>(version: "c1")
            .AddContract<IGCInfo>(version: "c1")
            .Build();

        IGCInfoHandle handle = target.Contracts.GCInfo.DecodePlatformSpecificGCInfo(
            new TargetPointer(BlobsBase), gcVersion: 5);

        Assert.Contains("Wasm32GCInfoTraits", handle.GetType().FullName);
    }

    [Fact]
    public void GCInfo_WasmTraitsMatchNativeEncoding()
    {
        Assert.Equal(6, Wasm32GCInfoTraits.GENERICS_INST_CONTEXT_STACK_SLOT_ENCBASE);
        Assert.Equal(6, Wasm32GCInfoTraits.GS_COOKIE_STACK_SLOT_ENCBASE);
        Assert.Equal(6, Wasm32GCInfoTraits.CODE_LENGTH_ENCBASE);
        Assert.Equal(3, Wasm32GCInfoTraits.STACK_BASE_REGISTER_ENCBASE);
        Assert.Equal(6, Wasm32GCInfoTraits.SIZE_OF_STACK_AREA_ENCBASE);
        Assert.Equal(3, Wasm32GCInfoTraits.SIZE_OF_EDIT_AND_CONTINUE_PRESERVED_AREA_ENCBASE);
        Assert.Equal(6, Wasm32GCInfoTraits.REVERSE_PINVOKE_FRAME_ENCBASE);
        Assert.Equal(3, Wasm32GCInfoTraits.NUM_REGISTERS_ENCBASE);
        Assert.Equal(5, Wasm32GCInfoTraits.NUM_STACK_SLOTS_ENCBASE);
        Assert.Equal(5, Wasm32GCInfoTraits.NUM_UNTRACKED_SLOTS_ENCBASE);
        Assert.Equal(4, Wasm32GCInfoTraits.NORM_PROLOG_SIZE_ENCBASE);
        Assert.Equal(3, Wasm32GCInfoTraits.NORM_EPILOG_SIZE_ENCBASE);
        Assert.Equal(5, Wasm32GCInfoTraits.INTERRUPTIBLE_RANGE_DELTA1_ENCBASE);
        Assert.Equal(5, Wasm32GCInfoTraits.INTERRUPTIBLE_RANGE_DELTA2_ENCBASE);
        Assert.Equal(3, Wasm32GCInfoTraits.REGISTER_ENCBASE);
        Assert.Equal(3, Wasm32GCInfoTraits.REGISTER_DELTA_ENCBASE);
        Assert.Equal(6, Wasm32GCInfoTraits.STACK_SLOT_ENCBASE);
        Assert.Equal(4, Wasm32GCInfoTraits.STACK_SLOT_DELTA_ENCBASE);
        Assert.Equal(4, Wasm32GCInfoTraits.NUM_SAFE_POINTS_ENCBASE);
        Assert.Equal(1, Wasm32GCInfoTraits.NUM_INTERRUPTIBLE_RANGES_ENCBASE);
        Assert.False(Wasm32GCInfoTraits.HAS_FIXED_STACK_PARAMETER_SCRATCH_AREA);
        Assert.Equal(7u, Wasm32GCInfoTraits.DenormalizeStackBaseRegister(7));
        Assert.Equal(0x24, Wasm32GCInfoTraits.DenormalizeStackSlot(0x24));
        Assert.Equal(0x18Cu, Wasm32GCInfoTraits.DenormalizeCodeOffset(0x18C));
        Assert.Equal(0x2B1u, Wasm32GCInfoTraits.DenormalizeCodeLength(0x2B1));
    }

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
    public void TryUnwindOneFrame_CallerWithoutVirtualIp_PreservesCallerStackPointer()
    {
        const uint leafFrameSize = 0x20;
        ulong callerBase = FramesBase + leafFrameSize;

        FakeWasmR2RInfo info = new();
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 3, "leaf"),
            Frame(callerBase, FuncIndexCaller, 7, "nonR2RCaller"),
            Blob(BlobsBase, [(byte)leafFrameSize], "leafUnwind"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        TargetPointer sp = new(FramesBase);
        Assert.True(unwinder.TryUnwindOneFrame(ref sp, out TargetCodePointer ip));
        Assert.Equal(callerBase, sp.Value);
        Assert.Equal(TargetCodePointer.Null, ip);
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
    public void TryUnwindOneFrame_ReversePInvoke_DoesNotProbeNativeCallerAsShadowFrame()
    {
        const uint frameSize = 0x20;
        const ulong controlPc = VirtualIpBase + 6;
        ulong nativeCallerSp = FramesBase + frameSize;

        FakeWasmR2RInfo info = new();
        info.UnwindData[FuncIndexLeaf] = BlobsBase;
        // Deliberately make native caller bytes look like a valid R2R frame. Without the
        // reverse-P/Invoke guard, GetVirtualIP would manufacture this false managed caller.
        info.VirtualIpBases[FuncIndexCaller] = VirtualIpBase + 0x100;

        Mock<IExecutionManager> executionManager = new();
        executionManager
            .Setup(e => e.GetCodeBlockHandle(new TargetCodePointer(controlPc)))
            .Returns(new CodeBlockHandle(new TargetPointer(0x30000)));
        TargetPointer gcInfoAddress = new(0x40000);
        uint gcVersion = 5;
        executionManager
            .Setup(e => e.GetGCInfo(
                It.IsAny<CodeBlockHandle>(),
                out gcInfoAddress,
                out gcVersion));

        IGCInfoHandle gcInfoHandle = Mock.Of<IGCInfoHandle>();
        Mock<IGCInfo> gcInfo = new();
        gcInfo
            .Setup(g => g.DecodePlatformSpecificGCInfo(gcInfoAddress, gcVersion))
            .Returns(gcInfoHandle);
        gcInfo
            .Setup(g => g.GetHeader(gcInfoHandle))
            .Returns(default(GCInfoHeader) with { HasReversePInvokeFrame = true });

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 3, "reversePInvoke"),
            Frame(nativeCallerSp, FuncIndexCaller, 2, "nativeBytesResemblingR2R"),
            Blob(BlobsBase, [(byte)frameSize], "frameSize"),
        ],
        executionManager.Object,
        gcInfo.Object);
        WasmUnwinder unwinder = new(target, info);

        TargetPointer sp = new(FramesBase);
        Assert.True(unwinder.TryUnwindOneFrame(
            ref sp,
            new TargetCodePointer(controlPc),
            out TargetCodePointer ip));
        Assert.Equal(nativeCallerSp, sp.Value);
        Assert.Equal(TargetCodePointer.Null, ip);
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
    private const uint StackWalkSentinelTerminate = 1;

    private const uint FuncIndexFunclet = 12;
    private const uint FuncIndexNestedFunclet = 13;

    // A ULEB128-encoded frame size, used as the whole unwind blob.
    private static byte[] FrameSize(uint size)
    {
        List<byte> bytes = new();
        do
        {
            byte b = (byte)(size & 0x7F);
            size >>= 7;
            if (size != 0)
                b |= 0x80;
            bytes.Add(b);
        }
        while (size != 0);
        return bytes.ToArray();
    }

    [Fact]
    public void TryGetLogicalFramePointer_RootFunction_ReturnsOwnFrameBase()
    {
        FakeWasmR2RInfo info = new();
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexLeaf, 3, "root"),
            Blob(BlobsBase, FrameSize(0x20), "rootFrameSize"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        Assert.True(unwinder.TryGetLogicalFramePointer(new TargetPointer(FramesBase), out TargetPointer fp));
        Assert.Equal(FramesBase, fp.Value);
    }

    /// <summary>
    /// A funclet called directly by its containing method: unwinding out of the funclet lands on
    /// the method's own frame, whose base is the establishing frame pointer.
    /// </summary>
    [Fact]
    public void TryGetLogicalFramePointer_FuncletCalledByParent_ReturnsParentFrameBase()
    {
        const uint FuncletFrameSize = 0x20;
        ulong parentFrame = FramesBase + FuncletFrameSize;

        FakeWasmR2RInfo info = new();
        info.Funclets.Add(FuncIndexFunclet);
        info.UnwindData[FuncIndexFunclet] = BlobsBase;
        info.UnwindData[FuncIndexCaller] = BlobsBase + 0x10;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexFunclet, 1, "funclet"),
            Frame(parentFrame, FuncIndexCaller, 5, "parent"),
            Blob(BlobsBase, FrameSize(FuncletFrameSize), "funcletFrameSize"),
            Blob(BlobsBase + 0x10, FrameSize(0x40), "parentFrameSize"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        Assert.True(unwinder.TryGetLogicalFramePointer(new TargetPointer(FramesBase), out TargetPointer fp));
        Assert.Equal(parentFrame, fp.Value);
    }

    [Fact]
    public void WasmContext_UnwindFromFuncletCalledByParent_SetsCallerStackInstructionAndFramePointers()
    {
        ulong funcletFrame = FramesBase;
        ulong parentFrame = FramesBase + WasmMockTarget.FrameSize;
        Target target = WasmMockTarget.Create(
            funcletFrame,
            WasmMockTarget.FuncletFunctionTableIndex,
            isFunclet: true,
            funcletCalledByParent: true);

        WasmR2RInfo r2rInfo = new(target);
        Assert.True(r2rInfo.TryGetFunctionIdentity(
            WasmMockTarget.FuncletFunctionTableIndex,
            out TargetPointer module,
            out uint runtimeFunctionIndex,
            out bool isFunclet));
        Assert.NotEqual(TargetPointer.Null, module);
        Assert.Equal(1u, runtimeFunctionIndex);
        Assert.True(isFunclet);

        // The funclet and its root resolve to the same virtual-IP base. This exercises the
        // backward walk over two real RUNTIME_FUNCTION entries without treating the fixture's
        // synthetic MinVirtualIP as captured-world truth.
        Assert.True(r2rInfo.TryGetVirtualIPBase(
            WasmMockTarget.FunctionTableIndex,
            out ulong rootVirtualIpBase));
        Assert.True(r2rInfo.TryGetVirtualIPBase(
            WasmMockTarget.FuncletFunctionTableIndex,
            out ulong funcletVirtualIpBase));
        Assert.Equal(rootVirtualIpBase, funcletVirtualIpBase);

        WasmContext context = new()
        {
            StackPointer = new TargetPointer(funcletFrame),
            InstructionPointer = new TargetCodePointer(funcletVirtualIpBase + 6),
            FramePointer = new TargetPointer(funcletFrame),
        };

        context.Unwind(target);

        Assert.Equal(parentFrame, context.StackPointer.Value);
        Assert.Equal(rootVirtualIpBase + 6, context.InstructionPointer.Value);
        Assert.Equal(parentFrame, context.FramePointer.Value);
    }

    /// <summary>
    /// A funclet invoked by the VM through CallFuncletWith[out]Throwable: unwinding terminates at
    /// the synthetic TERMINATE_R2R_STACK_WALK frame, which carries the establishing frame pointer
    /// one pointer-sized slot after the marker.
    /// </summary>
    [Fact]
    public void TryGetLogicalFramePointer_FuncletCalledByVM_RecoversEstablishingFramePointer()
    {
        const uint FuncletFrameSize = 0x20;
        ulong terminatorFrame = FramesBase + FuncletFrameSize;
        ulong establishingFp = FramesBase + 0x1000;

        TargetTestHelpers helpers = new(WasmArch);
        byte[] terminator = new byte[16];
        helpers.Write(terminator.AsSpan(0, sizeof(uint)), StackWalkSentinelTerminate);
        helpers.WritePointer(terminator.AsSpan((int)helpers.PointerSize, helpers.PointerSize), establishingFp);

        FakeWasmR2RInfo info = new();
        info.Funclets.Add(FuncIndexFunclet);
        info.UnwindData[FuncIndexFunclet] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexFunclet, 1, "funclet"),
            new MockMemorySpace.HeapFragment { Address = terminatorFrame, Data = terminator, Name = "terminator" },
            Blob(BlobsBase, FrameSize(FuncletFrameSize), "funcletFrameSize"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        Assert.True(unwinder.TryGetLogicalFramePointer(new TargetPointer(FramesBase), out TargetPointer fp));
        Assert.Equal(establishingFp, fp.Value);
    }

    /// <summary>
    /// A handler nested inside another funclet unwinds through both before reaching the method.
    /// </summary>
    [Fact]
    public void TryGetLogicalFramePointer_NestedFunclets_WalksOutToMethod()
    {
        const uint InnerFrameSize = 0x10;
        const uint OuterFrameSize = 0x20;
        ulong outerFunclet = FramesBase + InnerFrameSize;
        ulong parentFrame = outerFunclet + OuterFrameSize;

        FakeWasmR2RInfo info = new();
        info.Funclets.Add(FuncIndexNestedFunclet);
        info.Funclets.Add(FuncIndexFunclet);
        info.UnwindData[FuncIndexNestedFunclet] = BlobsBase;
        info.UnwindData[FuncIndexFunclet] = BlobsBase + 0x10;
        info.UnwindData[FuncIndexCaller] = BlobsBase + 0x20;

        TestPlaceholderTarget target = CreateTarget(
        [
            Frame(FramesBase, FuncIndexNestedFunclet, 1, "innerFunclet"),
            Frame(outerFunclet, FuncIndexFunclet, 2, "outerFunclet"),
            Frame(parentFrame, FuncIndexCaller, 5, "parent"),
            Blob(BlobsBase, FrameSize(InnerFrameSize), "innerFrameSize"),
            Blob(BlobsBase + 0x10, FrameSize(OuterFrameSize), "outerFrameSize"),
            Blob(BlobsBase + 0x20, FrameSize(0x40), "parentFrameSize"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        Assert.True(unwinder.TryGetLogicalFramePointer(new TargetPointer(FramesBase), out TargetPointer fp));
        Assert.Equal(parentFrame, fp.Value);
    }

    /// <summary>
    /// A localloc frame indirects to its real base before anything else is read, so the logical
    /// frame pointer must be the indirected base rather than the stack pointer.
    /// </summary>
    [Fact]
    public void TryGetLogicalFramePointer_LocallocRootFunction_ReturnsIndirectedBase()
    {
        TargetTestHelpers helpers = new(WasmArch);
        ulong realFp = FramesBase + 0x100;

        byte[] indirect = new byte[16];
        helpers.Write(indirect.AsSpan(0, sizeof(uint)), StackWalkSentinelIndirect);
        helpers.WritePointer(indirect.AsSpan((int)helpers.PointerSize, helpers.PointerSize), realFp);

        FakeWasmR2RInfo info = new();
        info.UnwindData[FuncIndexLeaf] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget(
        [
            new MockMemorySpace.HeapFragment { Address = FramesBase, Data = indirect, Name = "locallocSp" },
            Frame(realFp, FuncIndexLeaf, 3, "realFrame"),
            Blob(BlobsBase, FrameSize(0x20), "frameSize"),
        ]);
        WasmUnwinder unwinder = new(target, info);

        Assert.True(unwinder.TryGetLogicalFramePointer(new TargetPointer(FramesBase), out TargetPointer fp));
        Assert.Equal(realFp, fp.Value);
    }

    [Fact]
    public void TryGetFunctionIndex_ReportsIndexAndFuncletFlag()
    {
        FakeWasmR2RInfo info = new();
        info.Funclets.Add(FuncIndexFunclet);
        info.UnwindData[FuncIndexFunclet] = BlobsBase;

        TestPlaceholderTarget target = CreateTarget([Frame(FramesBase, FuncIndexFunclet, 1, "funclet")]);
        WasmUnwinder unwinder = new(target, info);

        Assert.True(unwinder.TryGetFunctionIndex(new TargetPointer(FramesBase), out uint functionIndex));
        Assert.Equal(FuncIndexFunclet, functionIndex);
        Assert.True(info.TryIsFunclet(functionIndex, out bool isFunclet));
        Assert.True(isFunclet);
    }
}
