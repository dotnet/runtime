// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterpreterRealCodeHeader))]
internal sealed partial class InterpreterRealCodeHeader : IData<InterpreterRealCodeHeader>
{
    [Field] public partial TargetPointer MethodDesc { get; }
    [Field] public partial TargetPointer DebugInfo { get; }
    [Field] public partial TargetPointer GCInfo { get; }
    [CustomInit(nameof(InitJitEHInfo))] public partial EEILException? JitEHInfo { get; }

    [DataDescriptorDependency(nameof(JitEHInfo), "pointer")]
    private partial EEILException? InitJitEHInfo(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpreterRealCodeHeader);
        TargetPointer jitEHInfoAddr = target.ReadPointerField(address, type, nameof(JitEHInfo));
        return jitEHInfoAddr != TargetPointer.Null
            ? target.ProcessedData.GetOrAdd<EEILException>(jitEHInfoAddr)
            : null;
    }
}
