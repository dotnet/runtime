// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class DebuggerEval : IData<DebuggerEval>
{
    static DebuggerEval IData<DebuggerEval>.Create(Target target, TargetPointer address)
        => new DebuggerEval(target, address);

    public DebuggerEval(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.DebuggerEval);
        TargetContext = address + (ulong)type.Fields[nameof(TargetContext)].Offset;
        EvalDuringException = target.Read<byte>(address + (ulong)type.Fields[nameof(EvalDuringException)].Offset) != 0;
        Address = address;
    }

    public TargetPointer Address { get; }
    public TargetPointer TargetContext { get; }
    public bool EvalDuringException { get; }
}
