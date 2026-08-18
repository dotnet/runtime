// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.TypeDesc))]
internal sealed partial class TypeDesc : IData<TypeDesc>
{
    [Field] public partial uint TypeAndFlags { get; }
}

[CdacType(nameof(DataType.ParamTypeDesc))]
internal sealed partial class ParamTypeDesc : IData<ParamTypeDesc>
{
    [CustomInit(nameof(InitTypeAndFlags))] public partial uint TypeAndFlags { get; }
    [Field] public partial TargetPointer TypeArg { get; }

    private partial uint InitTypeAndFlags(Target target, TargetPointer address)
    {
        return target.ProcessedData.GetOrAdd<TypeDesc>(address).TypeAndFlags;
    }
}

[CdacType(nameof(DataType.TypeVarTypeDesc))]
internal sealed partial class TypeVarTypeDesc : IData<TypeVarTypeDesc>
{
    [CustomInit(nameof(InitTypeAndFlags))] public partial uint TypeAndFlags { get; }
    [Field] public partial TargetPointer Module { get; }
    [Field] public partial uint Token { get; }
    [Field] public partial uint Index { get; }

    private partial uint InitTypeAndFlags(Target target, TargetPointer address)
    {
        return target.ProcessedData.GetOrAdd<TypeDesc>(address).TypeAndFlags;
    }
}

[CdacType(nameof(DataType.FnPtrTypeDesc))]
internal sealed partial class FnPtrTypeDesc : IData<FnPtrTypeDesc>
{
    [CustomInit(nameof(InitTypeAndFlags))] public partial uint TypeAndFlags { get; }
    [Field] public partial uint NumArgs { get; }
    [Field] public partial uint CallConv { get; }

    [FieldAddress]
    public partial TargetPointer RetAndArgTypes { get; }

    [Field] public partial TargetPointer LoaderModule { get; }

    private partial uint InitTypeAndFlags(Target target, TargetPointer address)
    {
        return target.ProcessedData.GetOrAdd<TypeDesc>(address).TypeAndFlags;
    }
}
