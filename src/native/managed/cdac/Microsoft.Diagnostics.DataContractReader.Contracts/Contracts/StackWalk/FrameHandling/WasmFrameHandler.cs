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
    public void HandleHijackFrame(HijackFrame frame)
        => throw new NotSupportedException("HijackFrame handling is not supported on WASM.");
}
