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
}

public readonly struct DebugInfo : IDebugInfo
{
    // Everything throws NotImplementedException
}
