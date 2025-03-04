// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class HijackArgsARM64 : IData<HijackArgsARM64>
{
    static HijackArgsARM64 IData<HijackArgsARM64>.Create(Target target, TargetPointer address)
        => new HijackArgsARM64(target, address);

    public HijackArgsARM64(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.HijackArgs);

        Dictionary<string, TargetNUInt> registers = new Dictionary<string, TargetNUInt>(type.Fields.Count);
        foreach ((string name, Target.FieldInfo field) in type.Fields)
        {
            TargetNUInt value = target.ReadNUInt(address + (ulong)field.Offset);
            registers.Add(name, value);
        }
        Registers = registers;
    }

    public IReadOnlyDictionary<string, TargetNUInt> Registers { get; }
}
