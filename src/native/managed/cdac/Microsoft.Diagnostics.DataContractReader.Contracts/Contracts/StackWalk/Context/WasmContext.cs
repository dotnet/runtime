// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Degenerate platform context for CoreCLR on WebAssembly.
/// </summary>
/// <remarks>
/// WebAssembly has no native register context: the runtime's <c>DT_CONTEXT</c> is an empty
/// struct and <c>REGDISPLAY</c> is zeroed (see <c>src/coreclr/debug/inc/dbgtargetcontext.h</c>
/// and <c>src/coreclr/inc/regdisp.h</c>). Managed execution on WASM is fully interpreted, so a
/// managed stack walk is a pointer-chase over the interpreter's explicit frame chain
/// (<c>InterpreterFrame.TopInterpMethodContextFrame</c> -&gt; <c>InterpMethodContextFrame.pParent</c>)
/// rather than a register-based unwind.
///
/// This type exists so that <see cref="IPlatformAgnosticContext.GetContextForPlatform"/> resolves
/// for WASM targets instead of throwing. The instruction/stack/frame pointer slots are synthetic:
/// they are populated by the interpreter frame-chain walker, not read from a native context blob
/// (hence <see cref="Size"/> is 0). Register-unwind operations are intentionally unsupported.
/// </remarks>
[StructLayout(LayoutKind.Sequential)]
public struct WasmContext : IPlatformContext
{
    // WASM is a 32-bit target (wasm32). Synthetic pointer slots populated by the
    // interpreter frame-chain walker; there is no native register file to mirror.
    private uint _instructionPointer;
    private uint _stackPointer;
    private uint _framePointer;

    // WASM has no native register-context blob to read from the target.
    public readonly uint Size => 0;

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
        => throw new NotSupportedException(
            "WASM has no native register context to unwind. Managed frames are enumerated by walking the interpreter frame chain.");

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
