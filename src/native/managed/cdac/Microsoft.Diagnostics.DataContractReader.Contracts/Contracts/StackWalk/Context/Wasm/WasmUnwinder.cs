// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers.Wasm;

/// <summary>
/// Information the WASM ReadyToRun unwinder needs about R2R function-table entries.
/// Mirrors the <c>ExecutionManager</c> APIs used by the native WASM stack walk in
/// <c>src/coreclr/vm/wasm/helpers.cpp</c> (<c>GetWasmVirtualIPFromFunctionTableIndex</c>) plus
/// access to the per-function unwind data from which the fixed frame size is decoded.
/// </summary>
internal interface IWasmR2RInfo
{
    /// <summary>
    /// Returns the base virtual IP for an R2R function table entry
    /// (<c>ExecutionManager::GetWasmVirtualIPFromFunctionTableIndex</c>). Returns false, or a
    /// base of 0, when the index does not map to a known R2R function.
    /// </summary>
    bool TryGetVirtualIPBase(uint functionTableIndex, out ulong baseVirtualIP);

    /// <summary>
    /// Returns the address of the WASM unwind blob for an R2R function table entry
    /// (<c>RUNTIME_FUNCTION.UnwindData + ImageBase</c>). The blob begins with a ULEB128 fixed
    /// frame size. Returns false when the index does not map to a known R2R function.
    /// </summary>
    bool TryGetUnwindData(uint functionTableIndex, out TargetPointer unwindDataAddress);

    /// <summary>
    /// Reports whether an R2R function table entry is a funclet rather than a method's root
    /// function (<c>ExecutionManager::IsFuncletFunctionIndex</c>). Returns false when the index
    /// does not map to a known R2R function.
    /// </summary>
    bool TryIsFunclet(uint functionTableIndex, out bool isFunclet);
}

/// <summary>
/// Walks CoreCLR WASM ReadyToRun frames over the managed linear stack (<c>$sp</c>), mirroring
/// the native implementation in <c>src/coreclr/vm/wasm/helpers.cpp</c>. The frame offsets and
/// sentinels below are defined in <c>src/coreclr/vm/wasm/callhelpers.hpp</c>, and the calling
/// convention is documented in <c>docs/design/coreclr/botr/clr-abi.md</c>.
/// </summary>
/// <remarks>
/// Each R2R frame base stores its R2R function table entry index at offset 0 and its
/// function-local virtual IP (divided by 2) at offset 4. A frame whose first word is
/// <see cref="StackWalkIndirectToFramePointer"/> is a <c>localloc</c> frame whose real base
/// pointer is stored one pointer-sized slot later. A frame whose first word is
/// <see cref="TerminateR2RStackWalk"/> is not R2R code (an interpreter transition or the stack
/// top), at which point R2R walking stops and the caller falls back to the explicit Frame chain
/// / interpreter frame chain.
/// </remarks>
internal sealed class WasmUnwinder
{
    // Sp values at or below the lowest linear-memory page carry nothing meaningful.
    private const ulong LinearStackFloor = 0x1000;

    // WASM_STACKFRAME_FUNCTION_INDEX_OFFSET: R2R function table entry index (32-bit).
    private const ulong FunctionIndexOffset = 0;

    // WASM_STACKFRAME_VIRTUALIP_OFFSET: function-local virtual IP / 2 (always 32-bit).
    private const ulong VirtualIpOffset = 4;

    // STACK_WALK_INDIRECT_TO_FRAMEPOINTER: this slot is not the frame base; the real base
    // pointer follows one pointer-sized slot later (localloc frames).
    private const uint StackWalkIndirectToFramePointer = 0;

    // TERMINATE_R2R_STACK_WALK: this frame is not R2R-generated managed code.
    private const uint TerminateR2RStackWalk = 1;

    private readonly Target _target;
    private readonly IWasmR2RInfo _r2rInfo;
    private readonly ulong _pointerSize;

    public WasmUnwinder(Target target, IWasmR2RInfo r2rInfo)
    {
        _target = target;
        _r2rInfo = r2rInfo;
        _pointerSize = (ulong)target.PointerSize;
    }

    /// <summary>
    /// Resolves the R2R frame base for a stack pointer, mirroring
    /// <c>GetWasmFramePointerFromStackPointer_Internal</c>. Returns false when there is no R2R
    /// frame at <paramref name="sp"/> (below the linear-stack floor, or a
    /// <see cref="TerminateR2RStackWalk"/> marker).
    /// </summary>
    public bool TryGetFramePointer(TargetPointer sp, out TargetPointer frameBase)
    {
        frameBase = TargetPointer.Null;
        if (sp.Value <= LinearStackFloor)
            return false;

        ulong current = sp.Value;
        if (_target.Read<uint>(current + FunctionIndexOffset) == StackWalkIndirectToFramePointer)
        {
            current = _target.ReadPointer(current + _pointerSize).Value;
            // Re-apply the linear-stack floor after following the localloc indirection: a null or
            // out-of-range saved frame pointer is not a valid frame base.
            if (current <= LinearStackFloor)
                return false;
        }

        if (_target.Read<uint>(current + FunctionIndexOffset) == TerminateR2RStackWalk)
            return false;

        frameBase = new TargetPointer(current);
        return true;
    }

    /// <summary>
    /// Recovers the establishing (method) frame pointer stored beside a
    /// <see cref="TerminateR2RStackWalk"/> marker by <c>CallFuncletWith[out]Throwable</c>,
    /// mirroring <c>GetWasmEstablishingFramePointerFromTerminator</c>. <paramref name="sp"/> must
    /// point at such a synthetic terminator frame.
    /// </summary>
    public TargetPointer GetEstablishingFramePointerFromTerminator(TargetPointer sp)
        => _target.ReadPointer(sp.Value + _pointerSize);

    /// <summary>
    /// Computes the current R2R virtual IP for a stack pointer, mirroring
    /// <c>GetWasmVirtualIPFromStackPointer</c>. Returns <see cref="TargetCodePointer.Null"/> when
    /// there is no R2R frame or the function index does not map to a known base virtual IP.
    /// </summary>
    public TargetCodePointer GetVirtualIP(TargetPointer sp)
    {
        if (!TryGetFramePointer(sp, out TargetPointer frameBase))
            return TargetCodePointer.Null;

        uint functionIndex = _target.Read<uint>(frameBase.Value + FunctionIndexOffset);
        // Virtual IPs are stored divided by 2; the low bit distinguishes virtual IPs from
        // interpreter addresses / portable entrypoints.
        uint functionLocalVirtualIP = _target.Read<uint>(frameBase.Value + VirtualIpOffset) * 2;

        if (!_r2rInfo.TryGetVirtualIPBase(functionIndex, out ulong baseVirtualIP) || baseVirtualIP == 0)
            return TargetCodePointer.Null;

        return new TargetCodePointer(baseVirtualIP + functionLocalVirtualIP);
    }

    /// <summary>
    /// Advances <paramref name="sp"/> by one R2R frame and produces the caller's virtual IP,
    /// mirroring <c>WasmUnwindStackFrameCore</c>. Returns false when the R2R walk terminates
    /// (no R2R frame at <paramref name="sp"/>), in which case <paramref name="sp"/> is set to
    /// <see cref="TargetPointer.Null"/>.
    /// </summary>
    public bool TryUnwindOneFrame(
        ref TargetPointer sp,
        TargetCodePointer controlPC,
        out TargetCodePointer ip)
    {
        ip = TargetCodePointer.Null;
        if (!TryGetFramePointer(sp, out TargetPointer frameBase))
        {
            sp = TargetPointer.Null;
            return false;
        }

        uint functionIndex = _target.Read<uint>(frameBase.Value + FunctionIndexOffset);
        if (!_r2rInfo.TryGetUnwindData(functionIndex, out TargetPointer unwindData))
        {
            sp = TargetPointer.Null;
            return false;
        }

        uint frameSize = DecodeULEB128(unwindData.Value);
        if (frameSize == 0)
        {
            // A zero frame size makes no progress; terminate rather than risk an unbounded walk.
            sp = TargetPointer.Null;
            return false;
        }

        bool callerIsNative = false;
        if (controlPC != TargetCodePointer.Null &&
            _r2rInfo.TryIsFunclet(functionIndex, out bool isFunclet) &&
            !isFunclet &&
            _target.Contracts.ExecutionManager.GetCodeBlockHandle(controlPC) is CodeBlockHandle codeBlock)
        {
            _target.Contracts.ExecutionManager.GetGCInfo(
                codeBlock,
                out TargetPointer gcInfoAddress,
                out uint gcVersion);
            IGCInfoHandle gcInfoHandle = _target.Contracts.GCInfo.DecodePlatformSpecificGCInfo(
                gcInfoAddress,
                gcVersion);
            callerIsNative = _target.Contracts.GCInfo.GetHeader(gcInfoHandle).HasReversePInvokeFrame;
        }

        sp = new TargetPointer(frameBase.Value + frameSize);
        ip = callerIsNative ? TargetCodePointer.Null : GetVirtualIP(sp);
        return true;
    }

    public bool TryUnwindOneFrame(ref TargetPointer sp, out TargetCodePointer ip)
        => TryUnwindOneFrame(ref sp, TargetCodePointer.Null, out ip);

    /// <summary>
    /// Returns the R2R function table entry index recorded in the frame at <paramref name="sp"/>.
    /// Returns false when there is no R2R frame there.
    /// </summary>
    public bool TryGetFunctionIndex(TargetPointer sp, out uint functionIndex)
    {
        functionIndex = 0;
        if (!TryGetFramePointer(sp, out TargetPointer frameBase))
            return false;

        functionIndex = _target.Read<uint>(frameBase.Value + FunctionIndexOffset);
        return true;
    }

    /// <summary>
    /// Advances <paramref name="sp"/> by one R2R frame and returns the caller's stack pointer,
    /// without requiring the caller to be R2R-generated code. This is the frame-size half of
    /// <c>WasmUnwindStackFrameCore</c>, which <see cref="TryGetLogicalFramePointer"/> needs in
    /// order to inspect a synthetic <see cref="TerminateR2RStackWalk"/> frame.
    /// </summary>
    private bool TryUnwindToCallerStackPointer(TargetPointer sp, out TargetPointer callerSp)
    {
        callerSp = TargetPointer.Null;
        if (!TryGetFramePointer(sp, out TargetPointer frameBase))
            return false;

        uint functionIndex = _target.Read<uint>(frameBase.Value + FunctionIndexOffset);
        if (!_r2rInfo.TryGetUnwindData(functionIndex, out TargetPointer unwindData))
            return false;

        uint frameSize = DecodeULEB128(unwindData.Value);
        if (frameSize == 0)
        {
            // A zero frame size makes no progress; refuse rather than risk an unbounded walk.
            return false;
        }

        callerSp = new TargetPointer(frameBase.Value + frameSize);
        return true;
    }

    /// <summary>
    /// Returns the logical (establishing) frame pointer for the frame at <paramref name="sp"/>,
    /// mirroring <c>GetWasmFramePointerFromStackPointer</c> in
    /// <c>src/coreclr/vm/wasm/helpers.cpp</c>.
    /// </summary>
    /// <remarks>
    /// For a method's root function this is simply its own frame base. For a funclet it is the
    /// frame base of the method that established the frame, because a funclet's WASM frame pointer
    /// local refers to the fixed portion of its parent's frame rather than its own
    /// (<c>WasmRegAlloc</c> in <c>src/coreclr/jit/regallocwasm.cpp</c>). Reaching it means
    /// unwinding out of the funclet, either to its containing function or to the synthetic
    /// <see cref="TerminateR2RStackWalk"/> frame that <c>CallFuncletWith[out]Throwable</c> pushes,
    /// which carries the establishing frame pointer beside the marker.
    /// </remarks>
    public bool TryGetLogicalFramePointer(TargetPointer sp, out TargetPointer framePointer)
    {
        framePointer = TargetPointer.Null;

        // The native original recurses until it reaches a non-funclet frame or the CallFunclet
        // terminator, relying on the stack being finite. The cDAC reads potentially untrustworthy
        // memory, so require strictly increasing stack pointers instead: every unwind step must
        // move toward the caller, which guarantees termination without capping legitimate funclet
        // nesting depth. The iteration count is a backstop only.
        const int MaxUnwindSteps = 4096;
        TargetPointer current = sp;

        for (int i = 0; i < MaxUnwindSteps; i++)
        {
            if (!TryGetFramePointer(current, out TargetPointer frameBase))
                return false;

            uint functionIndex = _target.Read<uint>(frameBase.Value + FunctionIndexOffset);

            // Native ExecutionManager::IsFuncletFunctionIndex returns FALSE when the function
            // index matches no range section, and GetWasmFramePointerFromStackPointer then treats
            // the frame as a root function. We deliberately fail instead: an index that belongs to
            // no R2R range section is not R2R code, so it has no variable debug info to resolve
            // against, and reporting its raw frame base would be a guess.
            if (!_r2rInfo.TryIsFunclet(functionIndex, out bool isFunclet))
                return false;

            if (!isFunclet)
            {
                framePointer = frameBase;
                return true;
            }

            if (!TryUnwindToCallerStackPointer(current, out TargetPointer callerSp))
                return false;

            if (callerSp.Value <= current.Value)
            {
                // No progress toward the caller; refuse rather than risk an unbounded walk.
                return false;
            }

            if (_target.Read<uint>(callerSp.Value + FunctionIndexOffset) == TerminateR2RStackWalk)
            {
                // The funclet was invoked by the VM through CallFuncletWith[out]Throwable, so
                // unwinding terminates at that synthetic frame before reaching the method's own
                // frame. Recover the establishing frame pointer stored beside the marker.
                framePointer = GetEstablishingFramePointerFromTerminator(callerSp);
                return true;
            }

            // The funclet was called by its containing method or funclet; keep walking out.
            current = callerSp;
        }

        return false;
    }

    // Standard little-endian base-128 varint, matching the native DecodeULEB128AsU32. A ULEB128
    // uint32 is at most 5 bytes (5 * 7 = 35 >= 32 bits); a longer encoding is malformed.
    private uint DecodeULEB128(ulong address)
    {
        const int MaxBytes = 5;
        uint result = 0;
        int shift = 0;
        for (ulong offset = 0; offset < MaxBytes; offset++)
        {
            byte b = _target.Read<byte>(address + offset);
            result |= (uint)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
                return result;
            shift += 7;
        }

        throw new InvalidOperationException("Malformed ULEB128 value in WASM unwind data.");
    }
}
