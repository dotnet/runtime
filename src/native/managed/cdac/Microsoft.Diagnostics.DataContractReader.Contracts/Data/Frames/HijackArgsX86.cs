// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class HijackArgsX86 : IData<HijackArgsX86>
{
    static HijackArgsX86 IData<HijackArgsX86>.Create(Target target, TargetPointer address)
        => new HijackArgsX86(target, address);

    public HijackArgsX86(Target target, TargetPointer address)
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
