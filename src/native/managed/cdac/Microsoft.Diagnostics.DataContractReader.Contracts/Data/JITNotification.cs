// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class JITNotification : IData<JITNotification>
{
    static JITNotification IData<JITNotification>.Create(Target target, TargetPointer address)
        => new JITNotification(target, address);

    private readonly Target _target;
    private readonly Target.TypeInfo _type;
    private readonly TargetPointer _address;

    private ushort _state;
    private TargetNUInt _clrModule;
    private uint _methodToken;

    public JITNotification(Target target, TargetPointer address)
    {
        _target = target;
        _type = target.GetTypeInfo(DataType.JITNotification);
        _address = address;

        _state = target.ReadField<ushort>(address, _type, nameof(State));
        _clrModule = target.ReadNUIntField(address, _type, nameof(ClrModule));
        _methodToken = target.ReadField<uint>(address, _type, nameof(MethodToken));
    }

    public ushort State
    {
        get => _state;
        set => _state = _target.WriteField(_address, _type, nameof(State), value);
    }

    public TargetNUInt ClrModule
    {
        get => _clrModule;
        set => _clrModule = _target.WriteNUIntField(_address, _type, nameof(ClrModule), value);
    }

    public uint MethodToken
    {
        get => _methodToken;
        set => _methodToken = _target.WriteField(_address, _type, nameof(MethodToken), value);
    }

    public bool IsFree => _state == 0;

    public void Clear()
    {
        State = 0;
        ClrModule = new TargetNUInt(0);
        MethodToken = 0;
    }

    public void WriteEntry(TargetPointer module, uint methodToken, ushort state)
    {
        ClrModule = new TargetNUInt(module.Value);
        MethodToken = methodToken;
        State = state;
    }
}
