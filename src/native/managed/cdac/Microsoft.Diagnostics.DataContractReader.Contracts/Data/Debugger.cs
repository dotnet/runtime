// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Debugger : IData<Debugger>
{
    static Debugger IData<Debugger>.Create(Target target, TargetPointer address)
        => new Debugger(target, address);

    private readonly TargetPointer _address;

    public Debugger(Target target, TargetPointer address)
    {
        _address = address;
        Target.TypeInfo type = target.GetTypeInfo(DataType.Debugger);

        LeftSideInitialized = target.ReadField<int>(address, type, nameof(LeftSideInitialized));
        Defines = target.ReadField<uint>(address, type, nameof(Defines));
        MDStructuresVersion = target.ReadField<uint>(address, type, nameof(MDStructuresVersion));
        RCThread = target.ReadPointerFieldOrNull(address, type, nameof(RCThread));
        RSRequestedSync = target.ReadFieldOrDefault<int>(address, type, nameof(RSRequestedSync));
        SendExceptionsOutsideOfJMC = target.ReadFieldOrDefault<int>(address, type, nameof(SendExceptionsOutsideOfJMC));
        GCNotificationEventsEnabled = target.ReadFieldOrDefault<int>(address, type, nameof(GCNotificationEventsEnabled));
    }

    public int LeftSideInitialized { get; init; }
    public uint Defines { get; init; }
    public uint MDStructuresVersion { get; init; }
    public TargetPointer RCThread { get; init; }
    public int RSRequestedSync { get; private set; }
    public int SendExceptionsOutsideOfJMC { get; private set; }
    public int GCNotificationEventsEnabled { get; private set; }

    public void SetField(Target target, string fieldName, int value)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Debugger);
        ulong addr = _address + (ulong)type.Fields[fieldName].Offset;
        target.Write<int>(addr, value);

        switch (fieldName)
        {
            case nameof(RSRequestedSync):
                RSRequestedSync = value;
                break;
            case nameof(SendExceptionsOutsideOfJMC):
                SendExceptionsOutsideOfJMC = value;
                break;
            case nameof(GCNotificationEventsEnabled):
                GCNotificationEventsEnabled = value;
                break;
            default:
                throw new ArgumentException($"Field '{fieldName}' is not a writable Debugger field.", nameof(fieldName));
        }
    }
}
