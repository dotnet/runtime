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
    // WASM is a 32-bit target (wasm32). Synthetic pointer slots populated by the
    // interpreter frame-chain walker; there is no native register file to mirror.
    private uint _instructionPointer;
    private uint _stackPointer;
    private uint _framePointer;

    // The synthetic context holds three 32-bit slots (IP/SP/FP). WASM has no native
    // register-context blob to read from the target; these slots are populated by the
    // interpreter frame-chain walker. Size matches the serialized struct so that
    // ContextHolder.GetBytes() and Size stay consistent.
    public readonly uint Size => 3 * sizeof(uint);

    public readonly uint ContextControlFlags => 0;

    public readonly uint FullContextFlags => 0;

    public readonly uint AllContextFlags => 0;

    // No register file: there is no stack-pointer register index.
    public readonly int StackPointerRegister => -1;

    public TargetPointer StackPointer
    {
        readonly get => new(_stackPointer);
        set => _stackPointer = (uint)value.Value;
    }

    public TargetCodePointer InstructionPointer
    {
        readonly get => new(_instructionPointer);
        set => _instructionPointer = (uint)value.Value;
    }

    public TargetPointer FramePointer
    {
        readonly get => new(_framePointer);
        set => _framePointer = (uint)value.Value;
    }

    public uint RawContextFlags { readonly get => 0; set { } }

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

    // WASM has no hardware single-step flag.
    public void UnsetSingleStepFlag()
        => throw new NotSupportedException("Single-step flag is not supported on WASM.");

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        switch (name.ToLowerInvariant())
        {
            case "pc" or "ip":
                _instructionPointer = (uint)value.Value;
                return true;
            case "sp":
                _stackPointer = (uint)value.Value;
                return true;
            case "fp":
                _framePointer = (uint)value.Value;
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
                value = new TargetNUInt(_instructionPointer);
                return true;
            case "sp":
                value = new TargetNUInt(_stackPointer);
                return true;
            case "fp":
                value = new TargetNUInt(_framePointer);
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
