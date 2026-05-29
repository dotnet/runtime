// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.JITNotification))]
internal sealed partial class JITNotification : IData<JITNotification>
{
    [Field(Writable = true)] public ushort State { get; private set; }
    [Field(Writable = true)] public TargetNUInt ClrModule { get; private set; }
    [Field(Writable = true)] public uint MethodToken { get; private set; }

    public bool IsFree => State == 0;

    public void Clear()
    {
        WriteState(0);
        WriteClrModule(new TargetNUInt(0));
        WriteMethodToken(0);
    }

    public void WriteEntry(TargetPointer module, uint methodToken, ushort state)
    {
        WriteClrModule(new TargetNUInt(module.Value));
        WriteMethodToken(methodToken);
        WriteState(state);
    }
}
