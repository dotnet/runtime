// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum SourceTypes : uint
{
    SourceTypeInvalid = 0x00, // To indicate that nothing else applies
    SequencePoint = 0x01, // The debugger asked for it.
    StackEmpty = 0x02, // The stack is empty here
    CallSite = 0x04, // This is a call site.
    NativeEndOffsetUnknown = 0x08, // Indicates a epilog endpoint
    CallInstruction = 0x10  // The actual instruction of a call.
}

public interface IOffsetMapping
{
    public uint NativeOffset { get; }
    public uint ILOffset { get; }
    public SourceTypes SourceType { get; }
}

public interface IDebugInfo : IContract
{
    static string IContract.Name { get; } = nameof(DebugInfo);
    IEnumerable<IOffsetMapping> GetMethodNativeMap(TargetCodePointer pCode, out uint codeOffset) => throw new NotImplementedException();
}

public readonly struct DebugInfo : IDebugInfo
{
    // Everything throws NotImplementedException
}
