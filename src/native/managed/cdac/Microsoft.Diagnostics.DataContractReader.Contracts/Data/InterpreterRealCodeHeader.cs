// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.InterpreterRealCodeHeader))]
internal sealed partial class InterpreterRealCodeHeader : IData<InterpreterRealCodeHeader>
{
    [Field] public partial TargetPointer MethodDesc { get; }
    [Field] public partial TargetPointer DebugInfo { get; }
    [Field] public partial TargetPointer GCInfo { get; }

    public EEILException? JitEHInfo { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.InterpreterRealCodeHeader);
        TargetPointer jitEHInfoAddr = target.ReadPointerField(address, type, nameof(JitEHInfo));
        if (jitEHInfoAddr != TargetPointer.Null)
            JitEHInfo = target.ProcessedData.GetOrAdd<EEILException>(jitEHInfoAddr);
    }
}
