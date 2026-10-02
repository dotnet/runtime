// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
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

    // ---------------------------------------------------------------------------------------
    // Funclet stack walks over linear-stack layouts that mirror what the producers emit:
    //  * RyuJIT funclet prolog (genFuncletProlog, src/coreclr/jit/codegenwasm.cpp): an unwindable
    //    funclet moves its own $sp down by AlignUp(2 * pointer, STACK_ALIGN) (16 on wasm32), stores
    //    its own function table index at $sp[0], and its unwind blob encodes that 16-byte frame.
    //  * fgWasmVirtualIP (src/coreclr/jit/fgwasm.cpp): a funclet stores its virtual IP at $sp[4] of
    //    its own frame; virtual IPs are relative to the controlling method, so the funclet's table
    //    index resolves to the parent's base virtual IP.
    //  * genCallFinally (codegenwasm.cpp): a non-exceptional finally is called with the parent's
    //    current $sp and FP; WasmRegAlloc::AllocateFramePointer (regallocwasm.cpp) makes the
    //    funclet's FP local that parent FP.
    //  * genLclHeap (codegenwasm.cpp): after localloc, $sp[0] == STACK_WALK_INDIRECT_TO_FRAMEPOINTER
    //    and $sp[pointer] == FP.
    //  * CallFuncletWith[out]Throwable (src/coreclr/vm/wasm/helpers.cpp): the VM pushes a 16-byte
    //    frame holding TERMINATE_R2R_STACK_WALK at +0 and the establishing FP at
    //    TERMINATE_R2R_STACK_WALK_FP_OFFSET (one pointer), then calls the funclet with that $sp.
    // Expected frame pointers follow native GetWasmFramePointerFromStackPointer, which reports a
    // funclet's logical FP: the frame base of the establishing method.
    // ---------------------------------------------------------------------------------------

    private const ulong LinearStackBase = 0x0001_0000;
    private const int LinearStackSize = 0x1000;
    private const ulong ProducerMinVirtualIP = 0x8001_0000;
    private const uint WasmFuncletFlag = 0x8000_0000;

    // Function table indices are assigned consecutively per R2R module; a method's funclets
    // immediately follow it, which is what lets a funclet index walk back to its parent.
    private const uint ParentIndex = 100;
    private const uint OuterFuncletIndex = 101;
    private const uint InnerFuncletIndex = 102;
    private const uint CalleeIndex = 103;

    private const uint ParentBegin = 0x100;
    private const uint CalleeBegin = 0x200;

    private const uint ParentFrameSize = 0x30;
    private const uint FuncletFrameSize = 16; // AlignUp(2 * TARGET_POINTER_SIZE, STACK_ALIGN) on wasm32
    private const uint CalleeFrameSize = 0x20;

    private const uint ParentVipHalf = 0x05;
    private const uint OuterFuncletVipHalf = 0x21;
    private const uint InnerFuncletVipHalf = 0x29;
    private const uint CalleeVipHalf = 0x03;

    private const ulong ParentIp = ProducerMinVirtualIP + ParentBegin + ParentVipHalf * 2;
    private const ulong OuterFuncletIp = ProducerMinVirtualIP + ParentBegin + OuterFuncletVipHalf * 2;
    private const ulong InnerFuncletIp = ProducerMinVirtualIP + ParentBegin + InnerFuncletVipHalf * 2;
    private const ulong CalleeIp = ProducerMinVirtualIP + CalleeBegin + CalleeVipHalf * 2;

    // The parent method's frame base, near the top of the modeled linear stack. Its own caller
    // slot holds TERMINATE_R2R_STACK_WALK (e.g. it was entered from the interpreter).
    private const ulong ParentFp = LinearStackBase + 0xF00;

    private sealed class LinearStack
    {
        private readonly TargetTestHelpers _helpers = new(WasmArch);
        public byte[] Data { get; } = new byte[LinearStackSize];

        private Span<byte> At(ulong address, int length) => Data.AsSpan((int)(address - LinearStackBase), length);

        public void Record(ulong address, uint functionIndex, uint vipHalf)
        {
            _helpers.Write(At(address, sizeof(uint)), functionIndex);
            _helpers.Write(At(address + 4, sizeof(uint)), vipHalf);
        }

        public void Terminator(ulong address, ulong establishingFp = 0)
        {
            _helpers.Write(At(address, sizeof(uint)), 1u);
            _helpers.WritePointer(At(address + (ulong)_helpers.PointerSize, _helpers.PointerSize), establishingFp);
        }

        public void LocallocSlot(ulong address, ulong framePointer)
        {
            _helpers.Write(At(address, sizeof(uint)), 0u);
            _helpers.WritePointer(At(address + (ulong)_helpers.PointerSize, _helpers.PointerSize), framePointer);
        }
    }

    // Parent root method, the two funclets it owns, and an unrelated callee root method, in one
    // R2R module, registered through FunctionTableIndexRangeList exactly as the runtime does.
    private static TestPlaceholderTarget CreateProducerLayoutTarget(LinearStack stack)
    {
        TestPlaceholderTarget.Builder targetBuilder = new(WasmArch);
        TargetTestHelpers helpers = targetBuilder.MemoryBuilder.TargetTestHelpers;
        MockMemorySpace.BumpAllocator allocator = targetBuilder.MemoryBuilder.CreateAllocator(0x0020_0000, 0x0020_4000);

        (uint BeginAddress, uint FrameSize)[] functions =
        [
            (ParentBegin, ParentFrameSize),
            (WasmFuncletFlag | (ParentBegin + 0x40), FuncletFrameSize),
            (WasmFuncletFlag | (ParentBegin + 0x50), FuncletFrameSize),
            (CalleeBegin, CalleeFrameSize),
        ];

        int hashMapStride = MockHashMap.CreateLayout(WasmArch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(WasmArch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(WasmArch, hashMapStride, isWasm: true);
        TargetTestHelpers.LayoutResult runtimeFunctionLayout = helpers.LayoutFields([
            new(nameof(Data.RuntimeFunction.BeginAddress), DataType.uint32),
            new(nameof(Data.RuntimeFunction.UnwindData), DataType.uint32),
        ]);
        TargetTestHelpers.LayoutResult rangeSectionLayout = helpers.LayoutFields([
            new(nameof(Data.FunctionTableIndexRangeSection.MinFunctionTableIndex), DataType.uint32),
            new(nameof(Data.FunctionTableIndexRangeSection.NumRuntimeFunctions), DataType.uint32),
            new(nameof(Data.FunctionTableIndexRangeSection.R2RModule), DataType.pointer),
            new(nameof(Data.FunctionTableIndexRangeSection.Next), DataType.pointer),
        ]);

        MockMemorySpace.HeapFragment runtimeFunctions = allocator.Allocate(runtimeFunctionLayout.Stride * (ulong)functions.Length, "RuntimeFunctions");
        for (int i = 0; i < functions.Length; i++)
        {
            // Unwind data is the ULEB128 fixed frame size, addressed as LoadedImageBase (0) + UnwindData.
            Assert.True(functions[i].FrameSize < 0x80);
            MockMemorySpace.HeapFragment unwindData = allocator.Allocate(1, "UnwindData");
            unwindData.Data[0] = (byte)functions[i].FrameSize;

            Span<byte> entry = runtimeFunctions.Data.AsSpan(i * (int)runtimeFunctionLayout.Stride, (int)runtimeFunctionLayout.Stride);
            helpers.Write(entry.Slice(runtimeFunctionLayout.Fields[nameof(Data.RuntimeFunction.BeginAddress)].Offset, sizeof(uint)), functions[i].BeginAddress);
            helpers.Write(entry.Slice(runtimeFunctionLayout.Fields[nameof(Data.RuntimeFunction.UnwindData)].Offset, sizeof(uint)), (uint)unwindData.Address);
        }

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = (uint)functions.Length;
        r2rInfo.RuntimeFunctions = runtimeFunctions.Address;
        r2rInfo.MinVirtualIP = ProducerMinVirtualIP;

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        MockMemorySpace.HeapFragment section = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        helpers.Write(section.Data.AsSpan(rangeSectionLayout.Fields[nameof(Data.FunctionTableIndexRangeSection.MinFunctionTableIndex)].Offset, sizeof(uint)), ParentIndex);
        helpers.Write(section.Data.AsSpan(rangeSectionLayout.Fields[nameof(Data.FunctionTableIndexRangeSection.NumRuntimeFunctions)].Offset, sizeof(uint)), (uint)functions.Length);
        helpers.WritePointer(section.Data.AsSpan(rangeSectionLayout.Fields[nameof(Data.FunctionTableIndexRangeSection.R2RModule)].Offset, helpers.PointerSize), module.Address);

        MockMemorySpace.HeapFragment listSlot = allocator.Allocate((ulong)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(listSlot.Data.AsSpan(), section.Address);

        targetBuilder.MemoryBuilder.AddHeapFragment(new MockMemorySpace.HeapFragment { Address = LinearStackBase, Data = stack.Data, Name = "LinearStack" });
        targetBuilder
            .AddTypes(new Dictionary<DataType, Target.TypeInfo>
            {
                [DataType.RuntimeFunction] = new() { Fields = runtimeFunctionLayout.Fields, Size = runtimeFunctionLayout.Stride },
                [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(r2rInfoLayout),
                [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
                [DataType.FunctionTableIndexRangeSection] = new() { Fields = rangeSectionLayout.Fields, Size = rangeSectionLayout.Stride },
            })
            .AddGlobals((Constants.Globals.FunctionTableIndexRangeList, listSlot.Address));
        return targetBuilder.Build();
    }

    // The walk starts in a root method called from inside the innermost funclet (for example
    // a breakpoint or Debugger.Break in a method called from a catch/finally body).
    private static WasmContext CalleeContext(ulong calleeSp) => new()
    {
        StackPointer = new TargetPointer(calleeSp),
        InstructionPointer = new TargetCodePointer(CalleeIp),
        FramePointer = new TargetPointer(calleeSp),
    };

    private static void AssertFrame(WasmContext context, ulong sp, ulong ip, ulong fp, string frame)
    {
        Assert.True(sp == context.StackPointer.Value, $"{frame}: SP 0x{context.StackPointer.Value:x}, expected 0x{sp:x}");
        Assert.True(ip == context.InstructionPointer.Value, $"{frame}: IP 0x{context.InstructionPointer.Value:x}, expected 0x{ip:x}");
        Assert.True(fp == context.FramePointer.Value, $"{frame}: FP 0x{context.FramePointer.Value:x}, expected 0x{fp:x}");
    }

    // The walk leaves R2R code at a TERMINATE_R2R_STACK_WALK frame; no R2R caller is reported.
    private static void AssertLeftR2R(WasmContext context)
        => Assert.Equal(TargetCodePointer.Null, context.InstructionPointer);

    /// <summary>
    /// Non-exceptional finally: the parent calls the finally funclet directly (genCallFinally)
    /// with its own $sp and FP, so the funclet's 16-byte frame sits immediately below the parent's
    /// $sp. When <paramref name="parentUsedLocalloc"/>, the parent's $sp is a localloc slot that
    /// indirects to its FP, and the funclet frame sits below that slot.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unwind_ProducerLayout_FinallyCalledByParent_ReportsParentFramePointer(bool parentUsedLocalloc)
    {
        ulong parentSp = parentUsedLocalloc ? ParentFp - 0x40 : ParentFp;
        ulong funcletSp = parentSp - FuncletFrameSize;
        ulong calleeSp = funcletSp - CalleeFrameSize;

        LinearStack stack = new();
        stack.Terminator(ParentFp + ParentFrameSize);
        stack.Record(ParentFp, ParentIndex, ParentVipHalf);
        if (parentUsedLocalloc)
            stack.LocallocSlot(parentSp, ParentFp);
        stack.Record(funcletSp, OuterFuncletIndex, OuterFuncletVipHalf);
        stack.Record(calleeSp, CalleeIndex, CalleeVipHalf);
        Target target = CreateProducerLayoutTarget(stack);

        WasmContext context = CalleeContext(calleeSp);

        context.Unwind(target);
        AssertFrame(context, funcletSp, OuterFuncletIp, ParentFp, "finally funclet");

        context.Unwind(target);
        AssertFrame(context, parentSp, ParentIp, ParentFp, "parent");

        context.Unwind(target);
        AssertLeftR2R(context);
    }

    /// <summary>
    /// Catch, finally, fault or filter invoked by the VM (EECodeManager::CallFunclet ->
    /// CallFuncletWith[out]Throwable). The funclet's frame sits directly below the synthetic
    /// TERMINATE_R2R_STACK_WALK frame, which carries the establishing FP; the establishing frame
    /// itself is far above, past native VM frames. A filter runs during the first pass, so the
    /// R2R frames of the try body that threw are still live between the establishing frame and
    /// the terminator (<paramref name="throwingFramesStillLive"/>); they must not be visited.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unwind_ProducerLayout_FuncletInvokedByVM_ReportsEstablishingFramePointer(bool throwingFramesStillLive)
    {
        ulong terminatorSp = LinearStackBase + 0x800;
        ulong funcletSp = terminatorSp - FuncletFrameSize;
        ulong calleeSp = funcletSp - CalleeFrameSize;

        LinearStack stack = new();
        stack.Terminator(ParentFp + ParentFrameSize);
        stack.Record(ParentFp, ParentIndex, ParentVipHalf);
        if (throwingFramesStillLive)
            stack.Record(ParentFp - CalleeFrameSize, CalleeIndex, CalleeVipHalf);
        stack.Terminator(terminatorSp, ParentFp);
        stack.Record(funcletSp, OuterFuncletIndex, OuterFuncletVipHalf);
        stack.Record(calleeSp, CalleeIndex, CalleeVipHalf);
        Target target = CreateProducerLayoutTarget(stack);

        WasmContext context = CalleeContext(calleeSp);

        context.Unwind(target);
        AssertFrame(context, funcletSp, OuterFuncletIp, ParentFp, "VM-invoked funclet");

        context.Unwind(target);
        AssertLeftR2R(context);
    }

    /// <summary>
    /// A finally nested in another funclet (for example try/finally inside a catch) is called
    /// directly by the outer funclet with the outer funclet's $sp and its FP local, which is
    /// already the establishing FP. Both funclets report the establishing method's frame base,
    /// whether the outer funclet was called by the parent or invoked by the VM.
    /// </summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Unwind_ProducerLayout_NestedFinally_ReportsEstablishingFramePointer(bool outerInvokedByVM)
    {
        ulong outerCallerSp = outerInvokedByVM ? LinearStackBase + 0x800 : ParentFp;
        ulong outerSp = outerCallerSp - FuncletFrameSize;
        ulong innerSp = outerSp - FuncletFrameSize;
        ulong calleeSp = innerSp - CalleeFrameSize;

        LinearStack stack = new();
        stack.Terminator(ParentFp + ParentFrameSize);
        stack.Record(ParentFp, ParentIndex, ParentVipHalf);
        if (outerInvokedByVM)
            stack.Terminator(outerCallerSp, ParentFp);
        stack.Record(outerSp, OuterFuncletIndex, OuterFuncletVipHalf);
        stack.Record(innerSp, InnerFuncletIndex, InnerFuncletVipHalf);
        stack.Record(calleeSp, CalleeIndex, CalleeVipHalf);
        Target target = CreateProducerLayoutTarget(stack);

        WasmContext context = CalleeContext(calleeSp);

        context.Unwind(target);
        AssertFrame(context, innerSp, InnerFuncletIp, ParentFp, "inner finally");

        context.Unwind(target);
        AssertFrame(context, outerSp, OuterFuncletIp, ParentFp, "outer funclet");

        context.Unwind(target);
        if (outerInvokedByVM)
        {
            AssertLeftR2R(context);
        }
        else
        {
            AssertFrame(context, ParentFp, ParentIp, ParentFp, "parent");
            context.Unwind(target);
            AssertLeftR2R(context);
        }
    }
}
