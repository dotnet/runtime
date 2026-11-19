// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

/// <summary>
/// Platform context for CoreCLR on 64-bit WebAssembly (memory64). Identical in meaning to
/// <see cref="WasmContext"/>; only the slot width differs.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct Wasm64Context : IPlatformContext
{
    // Field order and size mirror the native wasm64 T_CONTEXT (src/coreclr/pal/inc/pal.h):
    //   ContextFlags @0 (ULONG, then 4 bytes of padding), InterpreterWalkFramePointer @8,
    //   InterpreterSP @16, InterpreterFP @24, InterpreterIP @32 (ULONG_PTR each; 40 bytes).
    private uint _contextFlags;
    private ulong _interpreterWalkFramePointer;
    private ulong _interpreterSP;
    private ulong _interpreterFP;
    private ulong _interpreterIP;

    public readonly uint Size => 40;

    public readonly uint ContextControlFlags => 0;

    public readonly uint FullContextFlags => 0;

    public readonly uint AllContextFlags => 0;

    public readonly int StackPointerRegister => -1;

    public TargetPointer StackPointer
    {
        readonly get => new(_interpreterSP);
        set => _interpreterSP = value.Value;
    }

    public TargetCodePointer InstructionPointer
    {
        readonly get => new(_interpreterIP);
        set => _interpreterIP = value.Value;
    }

    public TargetPointer FramePointer
    {
        readonly get => new(_interpreterFP);
        set => _interpreterFP = value.Value;
    }

    public uint RawContextFlags { readonly get => _contextFlags; set => _contextFlags = value; }

    public void Unwind(Target target)
        => new Wasm.WasmUnwinder(target, new Wasm.WasmR2RInfo(target)).UnwindContext(ref this);

    public void UnsetSingleStepFlag() { }

    public bool TrySetRegister(string name, TargetNUInt value)
    {
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase) || name.Equals("ip", StringComparison.OrdinalIgnoreCase))
            _interpreterIP = value.Value;
        else if (name.Equals("sp", StringComparison.OrdinalIgnoreCase))
            _interpreterSP = value.Value;
        else if (name.Equals("fp", StringComparison.OrdinalIgnoreCase))
            _interpreterFP = value.Value;
        else if (name.Equals(WasmContext.InterpreterWalkFramePointerRegister, StringComparison.OrdinalIgnoreCase))
            _interpreterWalkFramePointer = value.Value;
        else
            return false;
        return true;
    }

    public readonly bool TryReadRegister(string name, out TargetNUInt value)
    {
        if (name.Equals("pc", StringComparison.OrdinalIgnoreCase) || name.Equals("ip", StringComparison.OrdinalIgnoreCase))
            value = new TargetNUInt(_interpreterIP);
        else if (name.Equals("sp", StringComparison.OrdinalIgnoreCase))
            value = new TargetNUInt(_interpreterSP);
        else if (name.Equals("fp", StringComparison.OrdinalIgnoreCase))
            value = new TargetNUInt(_interpreterFP);
        else if (name.Equals(WasmContext.InterpreterWalkFramePointerRegister, StringComparison.OrdinalIgnoreCase))
            value = new TargetNUInt(_interpreterWalkFramePointer);
        else
        {
            value = default;
            return false;
        }
        return true;
    }

    public bool TrySetRegister(int number, TargetNUInt value) => false;

    public readonly bool TryReadRegister(int number, out TargetNUInt value)
    {
        value = default;
        return false;
    }

    public readonly bool TryReadFloatingPointRegister(int index, out double value)
    {
        value = default;
        return false;
    }

    public readonly bool TryWriteFloatingPointRegister(int index, double value) => false;
    public readonly void CopyContext(ReadOnlySpan<byte> source) { }
}
