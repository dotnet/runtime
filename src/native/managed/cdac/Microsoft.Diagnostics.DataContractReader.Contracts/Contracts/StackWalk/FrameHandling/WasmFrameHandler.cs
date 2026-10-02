// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Frame handler for CoreCLR on WebAssembly.
/// </summary>
/// <remarks>
/// WebAssembly has no native register context (see <see cref="WasmContext"/>). Seeding the
/// initial stack walk context therefore comes from the explicit Frame chain rather than a
/// captured <c>DT_CONTEXT</c>: the innermost transition frame carries the managed linear stack
/// pointer. The base <see cref="BaseFrameHandler.HandleInlinedCallFrame"/> already reads that
/// <c>InlinedCallFrame.CallSiteSP</c> (plus the caller return address and callee-saved frame
/// pointer) into the three synthetic <see cref="WasmContext"/> slots, which is the common
/// P/Invoke-boundary seeding path; an inlined P/Invoke from R2R code instead stores a marker and is
/// resolved from its R2R shadow frame. The software/faulting exception frame handlers likewise read a
/// serialized <see cref="WasmContext"/> blob from the frame's <c>TargetContext</c>.
///
/// Hijack frames are a debugger / GC-suspension concept that is not yet supported on WASM.
/// </remarks>
internal sealed class WasmFrameHandler(Target target, ContextHolder<WasmContext> contextHolder)
    : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    // INLINED_PINVOKE_FROM_R2R from src/coreclr/vm/frames.h. An R2R inlined P/Invoke has no native
    // return address on WASM, so the runtime stores this marker and derives IP/SP from CallSiteSP.
    private const ulong InlinedPInvokeFromR2R = 1;

    private readonly ContextHolder<WasmContext> _holder = contextHolder;

    public override void HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        if (inlinedCallFrame.CallerReturnAddress.Value == InlinedPInvokeFromR2R)
        {
            // Mirrors InlinedCallFrame::UpdateRegDisplay_Impl in src/coreclr/vm/wasm/helpers.cpp.
            // If no R2R virtual IP can be recovered the IP is left null (not managed code), and the
            // stack walker fails the walk as native does, rather than treating the marker as an address.
            Wasm.WasmUnwinder unwinder = new(_target, new Wasm.WasmR2RInfo(_target));
            _holder.Context.StackPointer = inlinedCallFrame.CallSiteSP;
            _holder.Context.InstructionPointer = unwinder.GetVirtualIP(inlinedCallFrame.CallSiteSP);
            // Native GetWasmFramePointerFromStackPointer: a funclet reports its establishing method's frame.
            _holder.Context.FramePointer = unwinder.TryGetLogicalFramePointer(inlinedCallFrame.CallSiteSP, out TargetPointer framePointer)
                ? framePointer
                : TargetPointer.Null;
        }
        else
        {
            base.HandleInlinedCallFrame(inlinedCallFrame);
        }

        // When the frame directly above this P/Invoke transition is an InterpreterFrame, stash its
        // address in the synthetic first-argument register so the subsequent interpreter virtual
        // unwind (InterpreterVirtualUnwind -> GetFirstArgReg) can recover the owning InterpreterFrame.
        // Mirrors the per-architecture handlers (e.g. AMD64FrameHandler) and the native
        // SetFirstArgReg(context->InterpreterWalkFramePointer) contract on WASM.
        Data.Frame? next = GetNextFrame(inlinedCallFrame.Address);
        if (next is not null && _frameHelpers.GetFrameType(next.Identifier) == FrameType.InterpreterFrame)
        {
            if (!_holder.Context.TrySetRegister(WasmContext.InterpreterWalkFramePointerRegister, new TargetNUInt(next.Address.Value)))
                throw new InvalidOperationException($"Failed to set WASM interpreter frame-pointer register '{WasmContext.InterpreterWalkFramePointerRegister}'.");
        }
    }

    // Mirrors TransitionFrame::UpdateRegDisplay_Impl in src/coreclr/vm/wasm/helpers.cpp. A transition
    // helper called from R2R code records the caller's linear-stack pointer; when it is set and a
    // return address is known (stored, or derived from that stack pointer), the caller is the R2R
    // frame at that stack pointer (native TransitionFrame::GetSP). Otherwise the frame was entered
    // from interpreted or native code and the caller's stack pointer is the end of the TransitionBlock.
    public override void HandleTransitionFrame(FramedMethodFrame framedMethodFrame)
    {
        Data.TransitionBlock transitionBlock = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(framedMethodFrame.TransitionBlockPtr);
        TargetCodePointer instructionPointer = _frameHelpers.GetTransitionBlockReturnAddress(transitionBlock);
        TargetPointer stackPointer = transitionBlock.StackPointer ?? TargetPointer.Null;

        _holder.Context.InstructionPointer = instructionPointer;
        if (stackPointer != TargetPointer.Null && instructionPointer != TargetCodePointer.Null)
        {
            _holder.Context.StackPointer = stackPointer;
            // Native GetWasmFramePointerFromStackPointer: a funclet reports its establishing method's frame.
            Wasm.WasmUnwinder unwinder = new(_target, new Wasm.WasmR2RInfo(_target));
            _holder.Context.FramePointer = unwinder.TryGetLogicalFramePointer(stackPointer, out TargetPointer framePointer)
                ? framePointer
                : TargetPointer.Null;
        }
        else
        {
            _holder.Context.StackPointer = framedMethodFrame.TransitionBlockPtr + Data.TransitionBlock.GetSize(_target);
            _holder.Context.FramePointer = TargetPointer.Null;
        }
    }

    public void HandleHijackFrame(HijackFrame frame)
        => throw new PlatformNotSupportedException("HijackFrame handling is not supported on WASM.");
}
