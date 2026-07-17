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
    private readonly ContextHolder<WasmContext> _holder = contextHolder;

    public override void HandleInlinedCallFrame(InlinedCallFrame inlinedCallFrame)
    {
        base.HandleInlinedCallFrame(inlinedCallFrame);

        // When the frame directly above this P/Invoke transition is an InterpreterFrame, stash its
        // address in the synthetic first-argument register so the subsequent interpreter virtual
        // unwind (InterpreterVirtualUnwind -> GetFirstArgReg) can recover the owning InterpreterFrame.
        // Mirrors the per-architecture handlers (e.g. AMD64FrameHandler) and the native
        // SetFirstArgReg(context->InterpreterWalkFramePointer) contract on WASM.
        Data.Frame? next = GetNextFrame(inlinedCallFrame.Address);
        if (next is not null && _frameHelpers.GetFrameType(next.Identifier) == FrameType.InterpreterFrame)
        {
            _holder.Context.TrySetRegister(WasmContext.InterpreterWalkFramePointerRegister, new TargetNUInt(next.Address.Value));
        }
    }

    public void HandleHijackFrame(HijackFrame frame)
        => throw new PlatformNotSupportedException("HijackFrame handling is not supported on WASM.");
}
