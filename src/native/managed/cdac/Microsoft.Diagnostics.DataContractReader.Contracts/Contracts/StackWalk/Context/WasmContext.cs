// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Platform context for CoreCLR on WebAssembly.
/// </summary>
/// <remarks>
/// WebAssembly has no native register context: the runtime's <c>DT_CONTEXT</c> is an empty
/// struct and <c>REGDISPLAY</c> is zeroed (see <c>src/coreclr/debug/inc/dbgtargetcontext.h</c>
/// and <c>src/coreclr/inc/regdisp.h</c>). Instead, the context is driven by the managed linear
/// stack pointer (<c>$sp</c>): ReadyToRun frames are unwound over the linear stack with a
/// frameSize-based virtual unwind (see <see cref="Wasm.WasmUnwinder"/>) using synthetic virtual
/// IPs, and interpreter frames are the explicit
/// <c>InterpreterFrame.TopInterpMethodContextFrame</c> -&gt; <c>InterpMethodContextFrame.pParent</c>
/// chain. A real stack is a mix of the two.
///
/// The instruction/stack/frame pointer slots are 32-bit (wasm32): <see cref="StackPointer"/> is
/// the managed linear stack pointer and <see cref="InstructionPointer"/> is the current virtual IP.
/// <see cref="Unwind"/> advances the context by one ReadyToRun frame.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct WasmContext : IPlatformContext
{
    // Field order and size mirror the native wasm T_CONTEXT (src/coreclr/pal/inc/pal.h,
    // HOST_WASM branch) so that a serialized WasmContext is byte-compatible with the
    // runtime's context blob:
    //   ContextFlags @0, InterpreterWalkFramePointer @4, InterpreterSP @8,
    //   InterpreterFP @12, InterpreterIP @16  (20 bytes, all 32-bit / wasm32).
    // There is no native register file; these slots are populated by the R2R virtual
    // unwind and the interpreter frame-chain walker.
    private uint _contextFlags;
    private uint _interpreterWalkFramePointer;
    private uint _interpreterSP;
    private uint _interpreterFP;
    private uint _interpreterIP;

    // Name of the synthetic "first argument register" the interpreter stack walk uses to
    // stash the owning InterpreterFrame address (native SetFirstArgReg / GetFirstArgReg in
    // src/coreclr/vm/wasm/cgencpu.h write context->InterpreterWalkFramePointer).
    internal const string InterpreterWalkFramePointerRegister = "interpreterwalkframepointer";

    // Size matches the serialized native wasm T_CONTEXT so that ContextHolder.GetBytes()
    // and Size stay consistent.
    public readonly uint Size => 5 * sizeof(uint);

    public readonly uint ContextControlFlags => 0;

    public readonly uint FullContextFlags => 0;

    public readonly uint AllContextFlags => 0;

    // No register file: there is no stack-pointer register index.
    public readonly int StackPointerRegister => -1;

    public TargetPointer StackPointer
    {
        readonly get => new(_interpreterSP);
        set => _interpreterSP = (uint)value.Value;
    }

    public TargetCodePointer InstructionPointer
    {
        readonly get => new(_interpreterIP);
        set => _interpreterIP = (uint)value.Value;
    }

    public TargetPointer FramePointer
    {
        readonly get => new(_interpreterFP);
        set => _interpreterFP = (uint)value.Value;
    }

    public uint RawContextFlags { readonly get => _contextFlags; set => _contextFlags = value; }

    public void Unwind(Target target)
    {
        // Advance one ReadyToRun frame over the managed linear stack. When the R2R walk
        // terminates (an interpreter transition or the stack top), StackPointer becomes null and
        // the caller falls back to the explicit Frame chain / interpreter frame chain.
        Wasm.WasmUnwinder unwinder = new(target, new Wasm.WasmR2RInfo(target));
        TargetPointer sp = StackPointer;
        if (unwinder.TryUnwindOneFrame(ref sp, out TargetCodePointer ip))
        {
            StackPointer = sp;
            InstructionPointer = ip;
        }
        else
        {
            StackPointer = TargetPointer.Null;
            InstructionPointer = TargetCodePointer.Null;
        }
    }

    // WASM has no hardware single-step flag; like other architectures without one (ARM, LoongArch64,
    // RISC-V) this is a no-op. Callers (e.g. Debugger_1.PrepareExceptionHijack) invoke it
    // unconditionally, so it must not throw.
    public void UnsetSingleStepFlag() { }

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        switch (name.ToLowerInvariant())
        {
            case "pc" or "ip":
                _interpreterIP = (uint)value.Value;
                return true;
            case "sp":
                _interpreterSP = (uint)value.Value;
                return true;
            case "fp":
                _interpreterFP = (uint)value.Value;
                return true;
            case InterpreterWalkFramePointerRegister:
                _interpreterWalkFramePointer = (uint)value.Value;
                return true;
            default:
                return false;
        }
    }

    public readonly bool TryReadRegister(string name, out TargetNUInt value)
    {
        switch (name.ToLowerInvariant())
        {
            case "pc" or "ip":
                value = new TargetNUInt(_interpreterIP);
                return true;
            case "sp":
                value = new TargetNUInt(_interpreterSP);
                return true;
            case "fp":
                value = new TargetNUInt(_interpreterFP);
                return true;
            case InterpreterWalkFramePointerRegister:
                value = new TargetNUInt(_interpreterWalkFramePointer);
                return true;
            default:
                value = default;
                return false;
        }
    }

    public bool TrySetRegister(int number, TargetNUInt value) => false;

    public readonly bool TryReadRegister(int number, out TargetNUInt value)
    {
        value = default;
        return false;
    }
}
