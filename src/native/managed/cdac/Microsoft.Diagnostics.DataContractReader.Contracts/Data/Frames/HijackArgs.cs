// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.HijackArgs))]
internal partial class HijackArgs : IData<HijackArgs>
{
    [CustomInit(nameof(InitRegisters))] public partial IReadOnlyDictionary<string, TargetNUInt> Registers { get; }

    private partial IReadOnlyDictionary<string, TargetNUInt> InitRegisters(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HijackArgs);
        Dictionary<string, TargetNUInt> registers = new(type.Fields.Count);
        foreach ((string name, Target.FieldInfo field) in type.Fields)
        {
            TargetNUInt value = target.ReadNUInt(address + (ulong)field.Offset);
            registers.Add(name, value);
        }
        return registers;
    }
}
