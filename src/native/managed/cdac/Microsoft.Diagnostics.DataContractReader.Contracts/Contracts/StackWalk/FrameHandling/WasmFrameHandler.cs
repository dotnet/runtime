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
/// P/Invoke-boundary seeding path. The software/faulting exception frame handlers likewise read a
/// serialized <see cref="WasmContext"/> blob from the frame's <c>TargetContext</c>.
///
/// Hijack frames are a debugger / GC-suspension concept that is not yet supported on WASM.
/// </remarks>
internal sealed class WasmFrameHandler(Target target, ContextHolder<WasmContext> contextHolder)
    : BaseFrameHandler(target, contextHolder), IPlatformFrameHandler
{
    // INLINED_PINVOKE_FROM_R2R from src/coreclr/vm/frames.h. WASM has no native return address
    // for an R2R inline P/Invoke, so the native frame stores this marker and derives IP/FP from SP.
    private const uint InlinedPInvokeFromR2R = 1;

    private readonly ContextHolder<WasmContext> _holder = contextHolder;

    public override void HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        if (inlinedCallFrame.CallerReturnAddress == TargetCodePointer.Null)
            return;

        if (inlinedCallFrame.CallerReturnAddress == InlinedPInvokeFromR2R)
        {
            SetR2RContext(inlinedCallFrame.CallSiteSP);
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

    public override void HandleSoftwareExceptionFrame(SoftwareExceptionFrame softwareExceptionFrame)
    {
        // The serialized WASM T_CONTEXT carries SP/IP/FP together. The base implementation copies
        // only IP/SP plus hardware callee-saved registers; WASM has no such register dictionary.
        _holder.ReadFromAddress(_target, softwareExceptionFrame.TargetContext);
    }

    public override void HandleTransitionFrame(FramedMethodFrame framedMethodFrame)
    {
        Data.TransitionBlock transitionBlock = _target.ProcessedData.GetOrAdd<Data.TransitionBlock>(
            framedMethodFrame.TransitionBlockPtr);
        TargetPointer savedStackPointer = transitionBlock.StackPointer ?? TargetPointer.Null;
        bool hasR2RStackPointer = savedStackPointer != TargetPointer.Null;

        TargetCodePointer instructionPointer = transitionBlock.ReturnAddress;
        if (instructionPointer == TargetCodePointer.Null && hasR2RStackPointer)
        {
            Wasm.WasmUnwinder unwinder = new(_target, new Wasm.WasmR2RInfo(_target));
            instructionPointer = unwinder.GetVirtualIP(savedStackPointer);
        }

        _holder.Context.InstructionPointer = instructionPointer;
        _holder.Context.StackPointer = hasR2RStackPointer && instructionPointer != TargetCodePointer.Null
            ? savedStackPointer
            : framedMethodFrame.TransitionBlockPtr + Data.TransitionBlock.GetSize(_target);

        if (hasR2RStackPointer && instructionPointer != TargetCodePointer.Null)
        {
            Wasm.WasmUnwinder unwinder = new(_target, new Wasm.WasmR2RInfo(_target));
            if (unwinder.TryGetLogicalFramePointer(savedStackPointer, out TargetPointer fp))
            {
                _holder.Context.FramePointer = fp;
                return;
            }
        }

        _holder.Context.FramePointer = TargetPointer.Null;
    }

    public void HandleHijackFrame(HijackFrame frame)
        => throw new PlatformNotSupportedException("HijackFrame handling is not supported on WASM.");

    private void SetR2RContext(TargetPointer stackPointer)
    {
        Wasm.WasmUnwinder unwinder = new(_target, new Wasm.WasmR2RInfo(_target));
        TargetCodePointer instructionPointer = unwinder.GetVirtualIP(stackPointer);
        if (instructionPointer == TargetCodePointer.Null)
            throw new InvalidOperationException($"Failed to resolve WASM R2R virtual IP from stack pointer {stackPointer}.");
        if (!unwinder.TryGetLogicalFramePointer(stackPointer, out TargetPointer framePointer))
            throw new InvalidOperationException($"Failed to resolve WASM R2R frame pointer from stack pointer {stackPointer}.");

        _holder.Context.StackPointer = stackPointer;
        _holder.Context.InstructionPointer = instructionPointer;
        _holder.Context.FramePointer = framePointer;
    }
}
