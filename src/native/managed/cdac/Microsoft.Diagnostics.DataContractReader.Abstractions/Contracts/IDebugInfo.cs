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
}

public readonly struct DebugInfo : IDebugInfo
{
    // Everything throws NotImplementedException
}
