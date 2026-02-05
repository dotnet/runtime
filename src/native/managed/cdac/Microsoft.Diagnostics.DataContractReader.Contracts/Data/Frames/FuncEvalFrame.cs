// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

/// <summary>
/// Only exists if DEBUGGING_SUPPORTED defined in the target runtime.
/// </summary>
internal class FuncEvalFrame : IData<FuncEvalFrame>
{
    static FuncEvalFrame IData<FuncEvalFrame>.Create(Target target, TargetPointer address)
        => new FuncEvalFrame(target, address);

    public FuncEvalFrame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.FuncEvalFrame);
        DebuggerEvalPtr = target.ReadPointer(address + (ulong)type.Fields[nameof(DebuggerEvalPtr)].Offset);
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer DebuggerEvalPtr { get; }
}
