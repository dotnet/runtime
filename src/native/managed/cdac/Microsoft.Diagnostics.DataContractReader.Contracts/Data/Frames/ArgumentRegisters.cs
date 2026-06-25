// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Data;

[CdacType(nameof(DataType.ArgumentRegisters))]
internal partial class ArgumentRegisters : IData<ArgumentRegisters>
{
    public IReadOnlyDictionary<string, TargetNUInt> Registers { get; private set; }

    [MemberNotNull(nameof(Registers))]
    partial void OnInit(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.ArgumentRegisters);
        Dictionary<string, TargetNUInt> registers = new(type.Fields.Count);
        foreach ((string name, Target.FieldInfo field) in type.Fields)
        {
            TargetNUInt value = target.ReadNUInt(address + (ulong)field.Offset);
            registers.Add(name, value);
        }
        Registers = registers;
    }
}
