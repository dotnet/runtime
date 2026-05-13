// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class InterpreterRealCodeHeader : IData<InterpreterRealCodeHeader>
{
    static InterpreterRealCodeHeader IData<InterpreterRealCodeHeader>.Create(Target target, TargetPointer address)
        => new InterpreterRealCodeHeader(target, address);

    public InterpreterRealCodeHeader(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpreterRealCodeHeader);
        MethodDesc = target.ReadPointerField(address, type, nameof(MethodDesc));
        DebugInfo = target.ReadPointerField(address, type, nameof(DebugInfo));
        GCInfo = target.ReadPointerField(address, type, nameof(GCInfo));
        TargetPointer jitEHInfoAddr = target.ReadPointerField(address, type, nameof(JitEHInfo));
        JitEHInfo = jitEHInfoAddr != TargetPointer.Null ? target.ProcessedData.GetOrAdd<EEILException>(jitEHInfoAddr) : null;
    }

    public TargetPointer MethodDesc { get; init; }
    public TargetPointer DebugInfo { get; init; }
    public TargetPointer GCInfo { get; init; }
    public EEILException? JitEHInfo { get; init; }
}
