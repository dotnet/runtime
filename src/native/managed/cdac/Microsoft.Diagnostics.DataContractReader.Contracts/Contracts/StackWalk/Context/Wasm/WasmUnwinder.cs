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
    /// Returns the base virtual IP represented by a frame identity. Returns false, or a base of 0,
    /// when the identity does not map to a known R2R function.
    /// </summary>
    bool TryGetVirtualIPBase(uint frameIdentity, out ulong baseVirtualIP);

    /// <summary>
    /// Returns the address of the WASM unwind blob for the function containing
    /// <paramref name="controlPC"/>. The blob begins with a ULEB128 fixed frame size.
    /// </summary>
    bool TryGetUnwindData(TargetCodePointer controlPC, out TargetPointer unwindDataAddress);
}

/// <summary>
/// Walks CoreCLR WASM ReadyToRun frames over the managed linear stack (<c>$sp</c>), mirroring
/// the native implementation in <c>src/coreclr/vm/wasm/helpers.cpp</c>. The frame offsets and
/// sentinels below are defined in <c>src/coreclr/vm/wasm/callhelpers.hpp</c>, and the calling
/// convention is documented in <c>docs/design/coreclr/botr/clr-abi.md</c>.
/// </summary>
/// <remarks>
/// Each R2R frame base stores its frame identity at offset 0 and its function-local virtual IP
/// (divided by 2) at offset 4. A frame whose first word is
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

    // WASM_STACKFRAME_FUNCTION_INDEX_OFFSET: tagged frame identity (32-bit).
    private const ulong FrameIdentityOffset = 0;

    // WASM_STACKFRAME_VIRTUALIP_OFFSET: function-local virtual IP / 2 (always 32-bit).
    private const ulong VirtualIpOffset = 4;

    // STACK_WALK_INDIRECT_TO_FRAMEPOINTER: this slot is not the frame base; the real base
    // pointer follows one pointer-sized slot later (localloc frames).
    private const uint StackWalkIndirectToFramePointer = 0;

    // TERMINATE_R2R_STACK_WALK: this frame is not R2R-generated managed code.
    private const uint TerminateR2RStackWalk = 1;

    private const uint FrameIdentityTagMask = 3;
    private const uint EstablishingFrameTag = 2;
    private const int MaxEstablishingFrameDepth = 4096;

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
        if (_target.Read<uint>(current + FrameIdentityOffset) == StackWalkIndirectToFramePointer)
        {
            current = _target.ReadPointer(current + _pointerSize).Value;
            // Re-apply the linear-stack floor after following the localloc indirection: a null or
            // out-of-range saved frame pointer is not a valid frame base.
            if (current <= LinearStackFloor)
                return false;
        }

        if (_target.Read<uint>(current + FrameIdentityOffset) == TerminateR2RStackWalk)
            return false;

        frameBase = new TargetPointer(current);
        return true;
    }

    /// <summary>
    /// Resolves any establishing-frame identity tags to the root method frame.
    /// </summary>
    private bool TryGetEstablishingFramePointer(TargetPointer frameBase, out TargetPointer establishingFrame)
    {
        establishingFrame = frameBase;
        for (int i = 0; i < MaxEstablishingFrameDepth; i++)
        {
            uint identity = _target.Read<uint>(establishingFrame.Value + FrameIdentityOffset);
            if ((identity & FrameIdentityTagMask) != EstablishingFrameTag)
                return true;

            ulong next = identity - EstablishingFrameTag;
            if (next <= establishingFrame.Value)
            {
                establishingFrame = TargetPointer.Null;
                return false;
            }

            establishingFrame = new TargetPointer(next);
        }

        establishingFrame = TargetPointer.Null;
        return false;
    }

    /// <summary>
    /// Computes the current R2R virtual IP for a stack pointer, mirroring
    /// <c>GetWasmVirtualIPFromStackPointer</c>. Returns <see cref="TargetCodePointer.Null"/> when
    /// there is no R2R frame or the function index does not map to a known base virtual IP.
    /// </summary>
    public TargetCodePointer GetVirtualIP(TargetPointer sp)
    {
        if (!TryGetFramePointer(sp, out TargetPointer frameBase))
            return TargetCodePointer.Null;

        if (!TryGetEstablishingFramePointer(frameBase, out TargetPointer establishingFrame))
            return TargetCodePointer.Null;

        uint frameIdentity = _target.Read<uint>(establishingFrame.Value + FrameIdentityOffset);
        // Virtual IPs are stored divided by 2; the low bit distinguishes virtual IPs from
        // interpreter addresses / portable entrypoints.
        uint functionLocalVirtualIP = _target.Read<uint>(frameBase.Value + VirtualIpOffset) * 2;

        if (!_r2rInfo.TryGetVirtualIPBase(frameIdentity, out ulong baseVirtualIP) || baseVirtualIP == 0)
            return TargetCodePointer.Null;

        return new TargetCodePointer(baseVirtualIP + functionLocalVirtualIP);
    }

    /// <summary>
    /// Advances <paramref name="sp"/> by one R2R frame and produces the caller's virtual IP,
    /// mirroring <c>WasmUnwindStackFrameCore</c>. Returns false when the R2R walk terminates
    /// (no R2R frame at <paramref name="sp"/>), in which case <paramref name="sp"/> is set to
    /// <see cref="TargetPointer.Null"/>.
    /// </summary>
    public bool TryUnwindOneFrame(ref TargetPointer sp, TargetCodePointer controlPC, out TargetCodePointer ip)
    {
        ip = TargetCodePointer.Null;
        if (!TryGetFramePointer(sp, out TargetPointer frameBase))
        {
            sp = TargetPointer.Null;
            return false;
        }

        if (!_r2rInfo.TryGetUnwindData(controlPC, out TargetPointer unwindData))
        {
            sp = TargetPointer.Null;
            return false;
        }

        uint frameIdentity = _target.Read<uint>(frameBase.Value + FrameIdentityOffset);
        uint frameSize = DecodeULEB128(unwindData.Value);
        if (frameSize == 0)
        {
            // A zero frame size makes no progress; terminate rather than risk an unbounded walk.
            sp = TargetPointer.Null;
            return false;
        }

        sp = new TargetPointer(frameBase.Value + frameSize);
        if ((frameIdentity & 1) != 0 && (int)frameIdentity > 0)
        {
            // Positive odd identities are reverse P/Invoke roots. Their caller is native, so
            // preserve the caller SP for the Frame-chain walk but do not interpret it as an R2R
            // shadow frame.
            return true;
        }

        ip = GetVirtualIP(sp);
        if (ip == TargetCodePointer.Null)
        {
            // The caller is not R2R-generated code (an interpreter transition or the stack top);
            // the R2R walk is exhausted.
            sp = TargetPointer.Null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the logical (establishing) frame pointer for the frame at <paramref name="sp"/>,
    /// mirroring <c>GetWasmFramePointerFromStackPointer</c> in
    /// <c>src/coreclr/vm/wasm/helpers.cpp</c>.
    /// </summary>
    /// <remarks>
    /// For a method's root function this is its own frame base. A funclet stores its establishing
    /// frame pointer tagged with <see cref="EstablishingFrameTag"/>.
    /// </remarks>
    public bool TryGetLogicalFramePointer(TargetPointer sp, out TargetPointer framePointer)
    {
        framePointer = TargetPointer.Null;

        return TryGetFramePointer(sp, out TargetPointer frameBase)
            && TryGetEstablishingFramePointer(frameBase, out framePointer);
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
