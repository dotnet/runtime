// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

[Flags]
public enum SourceTypes : uint
{
    /// <summary>
    /// Indicates that no other options apply
    /// </summary>
    Default = 0x00,
    /// <summary>
    /// The stack is empty here
    /// </summary>
    StackEmpty = 0x01,
    /// <summary>
    /// The actual instruction of a call
    /// </summary>
    CallInstruction = 0x02,
    /// <summary>
    /// Indicates suspension/resumption for an async call
    /// </summary>
    Async = 0x04,
}

public readonly struct OffsetMapping
{
    public uint NativeOffset { get; init; }
    public uint ILOffset { get; init; }
    public SourceTypes SourceType { get; init; }
}

/// <summary>
/// Describes the kind of location where a variable is stored.
/// This is a stable public enum that abstracts over runtime-internal VarLocType values.
/// </summary>
public enum DebugVarLocKind
{
    Register,
    Stack,
    RegisterRegister,
    RegisterStack,
    StackRegister,
    DoubleStack,
    FloatingPointStack,
    FixedVarArg,
    /// <summary>
    /// The variable lives in a WebAssembly local. WASM locals are engine-private frame state:
    /// they are not in linear memory and cannot be read through the data target. Consumers with
    /// access to the WASM engine (for example a debugger attached over the Chrome DevTools
    /// Protocol) can fetch the value using <see cref="DebugVarInfo.WasmLocal"/>.
    /// </summary>
    WasmLocal,
    /// <summary>
    /// The variable spans two WebAssembly locals, described by
    /// <see cref="DebugVarInfo.WasmLocal"/> and <see cref="DebugVarInfo.WasmLocal2"/>.
    /// </summary>
    WasmLocalPair,
}

/// <summary>
/// A value type in the JIT's WebAssembly debug-register encoding.
/// This is encoding vocabulary, not the WebAssembly specification's complete or stable type set.
/// </summary>
public enum WasmDebugValueType : uint
{
    /// <summary>Reserved so that small packed values can encode pseudo-registers.</summary>
    Invalid = 0,
    I32 = 1,
    I64 = 2,
    F32 = 3,
    F64 = 4,
    V128 = 5,
    ExnRef = 6,
    Count = 7,
}

/// <summary>
/// Identifies a WebAssembly local by index and value type.
/// </summary>
/// <remarks>
/// <para>
/// On WASM, RyuJIT has no physical registers. It packs a <c>(local index, value type)</c> tuple
/// into <c>regNumber</c> using the target-described <c>WasmDebugRegisterTypeShift</c>
/// (<c>MakeWasmReg</c> in <c>src/coreclr/jit/registeropswasm.cpp</c>). That packed value is what
/// lands in the <c>ICorDebugInfo</c> variable-location stream. <see cref="Index"/> is the index
/// the emitted <c>local.get</c> / <c>local.set</c> instructions use directly, so it can be handed
/// to a WASM engine as-is after selecting the correct module/function.
/// </para>
/// <para>
/// WASM local index spaces are per function, and a method's funclets are separate WASM functions
/// from its root (see <c>WasmRegAlloc</c> in <c>src/coreclr/jit/regallocwasm.cpp</c>). Variable
/// ranges, by contrast, are method-relative. A consumer must therefore know which WASM function
/// the current virtual IP belongs to before interpreting <see cref="Index"/>.
/// </para>
/// </remarks>
public readonly struct WasmLocalInfo
{
    /// <summary>The WASM local index, as used by <c>local.get</c> / <c>local.set</c>.</summary>
    public uint Index { get; init; }
    /// <summary>The JIT debug-encoding value type of the local.</summary>
    public WasmDebugValueType ValueType { get; init; }
}

/// <summary>
/// Describes the location of a native variable at a particular native offset range.
/// This is a stable public type exposed by the DebugInfo contract.
/// </summary>
public readonly struct DebugVarInfo
{
    public uint StartOffset { get; init; }
    public uint EndOffset { get; init; }
    public uint VarNumber { get; init; }
    public DebugVarLocKind Kind { get; init; }
    public bool IsByRef { get; init; }
    public bool IsFloatingPoint { get; init; }

    /// <summary>Primary register number (Register, RegisterRegister, RegisterStack, StackRegister).</summary>
    public uint Register { get; init; }
    /// <summary>Second register number (RegisterRegister).</summary>
    public uint Register2 { get; init; }
    /// <summary>Stack base register number (Stack, DoubleStack, StackRegister, RegisterStack).</summary>
    public uint BaseRegister { get; init; }
    /// <summary>Stack offset from base register (Stack, DoubleStack, StackRegister).</summary>
    public int StackOffset { get; init; }
    /// <summary>Second stack base register (RegisterStack).</summary>
    public uint BaseRegister2 { get; init; }
    /// <summary>Second stack offset (RegisterStack).</summary>
    public int StackOffset2 { get; init; }
    /// <summary>Floating-point stack register number (FloatingPointStack).</summary>
    public uint FloatingPointStackRegister { get; init; }
    /// <summary>Offset of a fixed argument in a varargs function (FixedVarArg).</summary>
    public uint FixedVarArgOffset { get; init; }
    /// <summary>
    /// For <see cref="VarNumber"/> == <c>ICorDebugInfo::CALL_RETURN_ILNUM</c> entries, the IL offset of
    /// the call site whose return value this entry describes. Zero for all other entries.
    /// </summary>
    public uint CallReturnValueILOffset { get; init; }

    /// <summary>
    /// On WASM, <see cref="Register"/> decoded into a local index and value type.
    /// Null on every other architecture.
    /// </summary>
    public WasmLocalInfo? WasmLocal { get; init; }
    /// <summary>
    /// On WASM, <see cref="Register2"/> decoded into a local index and value type.
    /// Null on every other architecture.
    /// </summary>
    public WasmLocalInfo? WasmLocal2 { get; init; }
}

/// <summary>
/// A native code location at which an async method may suspend, together with the
/// continuation-object locals captured at that point.
/// </summary>
public readonly struct AsyncSuspensionInfo
{
    /// <summary>The native code offset of the suspension point.</summary>
    public uint NativeOffset { get; init; }
    /// <summary>The continuation-object locals live at this suspension point.</summary>
    public IReadOnlyList<AsyncLocalInfo> Locals { get; init; }
}

/// <summary>
/// A single local captured into the continuation object at a suspension point.
/// </summary>
public readonly struct AsyncLocalInfo
{
    /// <summary>Offset of the local within the continuation object's data area.</summary>
    public uint Offset { get; init; }
    /// <summary>IL var number of the local (or a synthetic marker such as MAX_ILNUM-relative values).</summary>
    public uint ILVarNumber { get; init; }
}

public interface IDebugInfo : IContract
{
    static string IContract.Name { get; } = nameof(DebugInfo);
    /// <summary>
    /// Returns true if the method at <paramref name="pCode"/> has debug info associated with it.
    /// Methods such as ILStubs may be JIT-compiled but have no debug metadata.
    /// </summary>
    bool HasDebugInfo(TargetCodePointer pCode) => throw new NotImplementedException();
    /// <summary>
    /// Given a code pointer, return the associated native/IL offset mapping and codeOffset.
    /// </summary>
    IEnumerable<OffsetMapping> GetMethodNativeMap(TargetCodePointer pCode, bool preferUninstrumented, out uint codeOffset) => throw new NotImplementedException();
    /// <summary>
    /// Given a code pointer, return the variable location info for the method.
    /// Each entry describes where a variable is stored at a particular native offset range.
    /// </summary>
    IEnumerable<DebugVarInfo> GetMethodVarInfo(TargetCodePointer pCode, out uint codeOffset) => throw new NotImplementedException();
    /// <summary>
    /// Given a code pointer, return the async-suspension points for the method together with the
    /// continuation-object locals captured at each suspension point. Returns an empty list when
    /// the method has no async debug info.
    /// </summary>
    IReadOnlyList<AsyncSuspensionInfo> GetAsyncSuspensionPoints(TargetCodePointer pCode) =>
        Array.Empty<AsyncSuspensionInfo>();
}

public readonly struct DebugInfo : IDebugInfo
{
    // Everything throws NotImplementedException
}
