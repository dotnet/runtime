// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts;
using Microsoft.Diagnostics.DataContractReader.TestInfrastructure;
using Moq;

namespace Microsoft.Diagnostics.DataContractReader.Tests;

/// <summary>
/// Builds a WASM <see cref="Target"/> carrying enough of the ReadyToRun function-table state for
/// <c>WasmR2RInfo</c> / <c>WasmUnwinder</c> to run, plus a real ReadyToRun frame in linear memory.
/// </summary>
internal static class WasmMockTarget
{
    // WASM is a 32-bit little-endian target.
    private static readonly MockTarget.Architecture WasmArch = new() { IsLittleEndian = true, Is64Bit = false };

    internal const uint MinFunctionTableIndex = 5;
    internal const uint FunctionTableIndex = 5; // localIndex 0
    internal const uint FuncletFunctionTableIndex = 6; // localIndex 1
    internal const ulong MinVirtualIP = 0x0005_0000;
    internal const ulong LoadedImageBase = 0x0090_0000;
    internal const uint FunctionBeginAddress = 0x100;
    internal const uint FrameSize = 0x20;

    // Where the synthetic CallFunclet terminator stores the establishing (method) frame pointer.
    internal const ulong EstablishingFramePointer = 0x0030_0000;

    // TERMINATE_R2R_STACK_WALK, from src/coreclr/vm/wasm/callhelpers.hpp.
    private const uint TerminateR2RStackWalk = 1;

    /// <summary>
    /// Creates a target whose frame at <paramref name="frameAddress"/> belongs to
    /// <paramref name="functionIndex"/>. When <paramref name="isFunclet"/> is set, the frame is a
    /// funclet and the frame one <see cref="FrameSize"/> above it is the synthetic CallFunclet
    /// terminator carrying <see cref="EstablishingFramePointer"/>.
    /// </summary>
    internal static Target Create(
        ulong frameAddress,
        uint functionIndex,
        bool isFunclet,
        bool funcletCalledByParent = false,
        CodeKind codeKind = CodeKind.ReadyToRun)
        => CreateCore(
            frameAddress,
            functionIndex,
            isFunclet,
            funcletCalledByParent,
            addR2RInlinedCallFrame: false,
            codeKind: codeKind,
            out _);

    internal static (Target Target, ulong InlinedCallFrameAddress) CreateWithR2RInlinedCallFrame(
        ulong frameAddress,
        uint functionIndex)
    {
        Target target = CreateCore(
            frameAddress,
            functionIndex,
            isFunclet: false,
            funcletCalledByParent: false,
            addR2RInlinedCallFrame: true,
            codeKind: CodeKind.ReadyToRun,
            out ulong inlinedCallFrameAddress);
        return (target, inlinedCallFrameAddress);
    }

    internal static (Target Target, ulong TransitionFrameAddress) CreateWithR2RTransitionFrame(
        ulong frameAddress,
        uint functionIndex)
    {
        Target target = CreateCore(
            frameAddress,
            functionIndex,
            isFunclet: false,
            funcletCalledByParent: false,
            addR2RInlinedCallFrame: false,
            addR2RTransitionFrame: true,
            codeKind: CodeKind.ReadyToRun,
            out _,
            out ulong transitionFrameAddress);
        return (target, transitionFrameAddress);
    }

    private static Target CreateCore(
        ulong frameAddress,
        uint functionIndex,
        bool isFunclet,
        bool funcletCalledByParent,
        bool addR2RInlinedCallFrame,
        CodeKind codeKind,
        out ulong inlinedCallFrameAddress)
        => CreateCore(
            frameAddress,
            functionIndex,
            isFunclet,
            funcletCalledByParent,
            addR2RInlinedCallFrame,
            addR2RTransitionFrame: false,
            codeKind: codeKind,
            out inlinedCallFrameAddress,
            out _);

    private static Target CreateCore(
        ulong frameAddress,
        uint functionIndex,
        bool isFunclet,
        bool funcletCalledByParent,
        bool addR2RInlinedCallFrame,
        bool addR2RTransitionFrame,
        CodeKind codeKind,
        out ulong inlinedCallFrameAddress,
        out ulong transitionFrameAddress)
    {
        inlinedCallFrameAddress = 0;
        transitionFrameAddress = 0;
        TargetTestHelpers helpers = new(WasmArch);
        TestPlaceholderTarget.Builder targetBuilder = new(WasmArch);
        MockMemorySpace.Builder builder = targetBuilder.MemoryBuilder;
        MockMemorySpace.BumpAllocator allocator = builder.CreateAllocator(0x0010_0000, 0x0018_0000);

        int hashMapStride = MockHashMap.CreateLayout(WasmArch).Size;
        var moduleLayout = MockLoaderModule.CreateLayout(WasmArch);
        var r2rInfoLayout = MockReadyToRunInfo.CreateLayout(WasmArch, hashMapStride);
        var runtimeFunctionLayout = helpers.LayoutFields([
            new("BeginAddress", DataType.uint32),
            new("UnwindData", DataType.uint32),
        ]);
        var rangeSectionLayout = helpers.LayoutFields([
            new("MinFunctionTableIndex", DataType.uint32),
            new("NumRuntimeFunctions", DataType.uint32),
            new("R2RModule", DataType.pointer),
            new("Next", DataType.pointer),
        ]);

        // The unwind blob is a ULEB128 fixed frame size. FrameSize is < 0x80 so it is one byte.
        // It must live at a positive RVA from LoadedImageBase, since RuntimeFunction.UnwindData is
        // an image-relative offset.
        const uint UnwindDataRva = 0x40;
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment
        {
            Address = LoadedImageBase + UnwindDataRva,
            Data = [(byte)FrameSize],
            Name = "UnwindData",
        });

        uint runtimeFunctionCount = funcletCalledByParent ? 2u : 1u;
        MockMemorySpace.HeapFragment runtimeFuncFrag = allocator.Allocate(
            runtimeFunctionLayout.Stride * runtimeFunctionCount,
            "RuntimeFunctions");

        void WriteRuntimeFunction(uint index, uint beginAddress, bool funclet)
        {
            int entryOffset = checked((int)(index * runtimeFunctionLayout.Stride));
            // RUNTIME_FUNCTION__IsFunclet: the funclet flag is the high bit of BeginAddress.
            helpers.Write(
                runtimeFuncFrag.Data.AsSpan().Slice(
                    entryOffset + runtimeFunctionLayout.Fields["BeginAddress"].Offset,
                    sizeof(uint)),
                funclet ? beginAddress | 0x80000000u : beginAddress);
            helpers.Write(
                runtimeFuncFrag.Data.AsSpan().Slice(
                    entryOffset + runtimeFunctionLayout.Fields["UnwindData"].Offset,
                    sizeof(uint)),
                UnwindDataRva);
        }

        if (funcletCalledByParent)
        {
            WriteRuntimeFunction(0, FunctionBeginAddress, funclet: false);
            WriteRuntimeFunction(1, FunctionBeginAddress + 0x20, funclet: true);
        }
        else
        {
            WriteRuntimeFunction(0, FunctionBeginAddress, isFunclet);
        }

        MockReadyToRunInfo r2rInfo = r2rInfoLayout.Create(allocator.Allocate((ulong)r2rInfoLayout.Size, "ReadyToRunInfo"));
        r2rInfo.CompositeInfo = r2rInfo.Address;
        r2rInfo.NumRuntimeFunctions = runtimeFunctionCount;
        r2rInfo.RuntimeFunctions = runtimeFuncFrag.Address;
        r2rInfo.LoadedImageBase = LoadedImageBase;
        r2rInfo.MinVirtualIP = MinVirtualIP;

        MockLoaderModule module = moduleLayout.Create(allocator.Allocate((ulong)moduleLayout.Size, "Module"));
        module.ReadyToRunInfo = r2rInfo.Address;

        MockMemorySpace.HeapFragment sectionFrag = allocator.Allocate(rangeSectionLayout.Stride, "FunctionTableIndexRangeSection");
        var secFields = rangeSectionLayout.Fields;
        helpers.Write(sectionFrag.Data.AsSpan().Slice(secFields["MinFunctionTableIndex"].Offset, sizeof(uint)), MinFunctionTableIndex);
        helpers.Write(
            sectionFrag.Data.AsSpan().Slice(secFields["NumRuntimeFunctions"].Offset, sizeof(uint)),
            runtimeFunctionCount);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["R2RModule"].Offset, helpers.PointerSize), module.Address);
        helpers.WritePointer(sectionFrag.Data.AsSpan().Slice(secFields["Next"].Offset, helpers.PointerSize), 0ul);

        // The global points at a slot holding the list head, not at the head itself.
        MockMemorySpace.HeapFragment slotFrag = allocator.Allocate((uint)helpers.PointerSize, "FunctionTableIndexRangeListSlot");
        helpers.WritePointer(slotFrag.Data.AsSpan().Slice(0, helpers.PointerSize), sectionFrag.Address);

        // The frame itself: [0] = function index, [4] = function-local virtual IP / 2.
        byte[] frame = new byte[FrameSize];
        helpers.Write(frame.AsSpan(0, sizeof(uint)), functionIndex);
        helpers.Write(frame.AsSpan(4, sizeof(uint)), 3u);
        builder.AddHeapFragment(new MockMemorySpace.HeapFragment { Address = frameAddress, Data = frame, Name = "frame" });

        if (funcletCalledByParent)
        {
            byte[] callerFrame = new byte[FrameSize];
            helpers.Write(callerFrame.AsSpan(0, sizeof(uint)), FunctionTableIndex);
            helpers.Write(callerFrame.AsSpan(4, sizeof(uint)), 3u);
            builder.AddHeapFragment(new MockMemorySpace.HeapFragment
            {
                Address = frameAddress + FrameSize,
                Data = callerFrame,
                Name = "parentFrame",
            });
        }
        else if (isFunclet)
        {
            // Unwinding the funclet lands here: the synthetic CallFuncletWith[out]Throwable frame,
            // which stores the establishing frame pointer one pointer-sized slot after the marker.
            byte[] terminator = new byte[FrameSize];
            helpers.Write(terminator.AsSpan(0, sizeof(uint)), TerminateR2RStackWalk);
            helpers.WritePointer(terminator.AsSpan((int)helpers.PointerSize, helpers.PointerSize), EstablishingFramePointer);
            builder.AddHeapFragment(new MockMemorySpace.HeapFragment
            {
                Address = frameAddress + FrameSize,
                Data = terminator,
                Name = "callFuncletTerminator",
            });
        }

        var types = new Dictionary<DataType, Target.TypeInfo>
        {
            [DataType.RuntimeFunction] = new() { Fields = runtimeFunctionLayout.Fields, Size = runtimeFunctionLayout.Stride },
            [DataType.ReadyToRunInfo] = TargetTestHelpers.CreateTypeInfo(r2rInfoLayout),
            [DataType.Module] = TargetTestHelpers.CreateTypeInfo(moduleLayout),
            [DataType.FunctionTableIndexRangeSection] = new() { Fields = rangeSectionLayout.Fields, Size = rangeSectionLayout.Stride },
        };

        if (addR2RInlinedCallFrame)
        {
            MockFrameBuilder frameBuilder = new(builder);
            MockInlinedCallFrame inlinedFrame = frameBuilder.AddInlinedCallFrame(
                callerReturnAddress: 1, // INLINED_PINVOKE_FROM_R2R
                datum: 0,
                callSiteSP: frameAddress,
                calleeSavedFP: 0);
            inlinedCallFrameAddress = inlinedFrame.Address;

            types.Add(DataType.Frame, TargetTestHelpers.CreateTypeInfo(frameBuilder.FrameLayout));
            types.Add(DataType.InlinedCallFrame, TargetTestHelpers.CreateTypeInfo(frameBuilder.InlinedCallFrameLayout));
            targetBuilder.AddGlobals(
                ("InlinedCallFrameIdentifier", MockFrameBuilder.InlinedCallFrameIdentifierValue));
        }

        if (addR2RTransitionFrame)
        {
            MockFrameBuilder frameBuilder = new(builder);
            Layout<MockWasmTransitionBlock> transitionBlockLayout =
                MockWasmTransitionBlock.CreateLayout(WasmArch);
            MockWasmTransitionBlock transitionBlock = transitionBlockLayout.Create(
                allocator.Allocate((ulong)transitionBlockLayout.Size, "TransitionBlock"));
            // Match the native lazy state: the return address starts at zero and is derived from
            // the saved R2R stack pointer by FramedMethodFrame::GetTransitionBlock_Impl.
            transitionBlock.ReturnAddress = 0;
            transitionBlock.StackPointer = frameAddress;

            MockFramedMethodFrame transitionFrame = frameBuilder.AddFramedMethodFrame(methodDescPtr: 0);
            transitionFrame.TransitionBlockPtr = transitionBlock.Address;
            transitionFrameAddress = transitionFrame.Address;

            types.Add(DataType.Frame, TargetTestHelpers.CreateTypeInfo(frameBuilder.FrameLayout));
            types.Add(DataType.FramedMethodFrame, TargetTestHelpers.CreateTypeInfo(frameBuilder.FramedMethodFrameLayout));
            types.Add(DataType.TransitionBlock, TargetTestHelpers.CreateTypeInfo(transitionBlockLayout));
            targetBuilder.AddGlobals(
                ("FramedMethodFrameIdentifier", MockFrameBuilder.FramedMethodFrameIdentifierValue));
        }

        Mock<IExecutionManager> executionManager = new();
        executionManager
            .Setup(e => e.GetCodeKind(It.IsAny<TargetCodePointer>()))
            .Returns(codeKind);

        return targetBuilder
            .AddTypes(types)
            .AddGlobals(
                ("FunctionTableIndexRangeList", slotFrag.Address),
                ("WasmDebugRegisterTypeShift", 29),
                ("WasmDebugValueTypeCount", 7))
            .AddGlobalStrings((Constants.Globals.Architecture, nameof(RuntimeInfoArchitecture.Wasm).ToLowerInvariant()))
            .AddContract<IRuntimeInfo>(version: "c1")
            .AddContract<IStackWalk>(version: "c1")
            .AddMockContract(executionManager.Object)
            .AddMockContract(Mock.Of<IGCInfo>())
            .Build();
    }
}
