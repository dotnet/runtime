// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.UnwindInfo))]
internal sealed partial class UnwindInfo : IData<UnwindInfo>
{
    public uint? FunctionLength { get; private set; }
    public uint? Header { get; private set; }

    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.UnwindInfo);
        if (type.Fields.ContainsKey(nameof(FunctionLength)))
        {
            // The unwind info contains the function length on some platforms (x86)
            FunctionLength = target.ReadField<uint>(address, type, nameof(FunctionLength));
        }
        else
        {
            // Otherwise, it starts with a bitfield header
            Header = target.Read<uint>(address);
        }
    }
}
