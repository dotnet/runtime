// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal class HelperMethodFrame : IData<HelperMethodFrame>
{
    private static readonly List<string> HelperMethodFrameTypes = [
        "HelperMethodFrame",
        "HelperMethodFrame_1OBJ",
        "HelperMethodFrame_2OBJ",
        "HelperMethodFrame_3OBJ",
        "HelperMethodFrame_PROTECTOBJ",
    ];

    private static DataType FindType(Target target, TargetPointer address)
    {
        TargetPointer instanceVptr = target.ReadPointer(address);

        foreach (string frameTypeName in HelperMethodFrameTypes)
        {
            TargetPointer typeVptr = target.ReadGlobalPointer(frameTypeName + "VPtr");
            if (instanceVptr == typeVptr)
            {
                return Enum.TryParse(frameTypeName, out DataType type) ? type : DataType.Unknown;
            }
        }

        return DataType.Unknown;
    }

    static HelperMethodFrame IData<HelperMethodFrame>.Create(Target target, TargetPointer address)
        => new HelperMethodFrame(target, address);

    public HelperMethodFrame(Target target, TargetPointer address)
    {
        Type = FindType(target, address);
        Debug.Assert(Type != DataType.Unknown);
        Target.TypeInfo type = target.GetTypeInfo(Type);
        FrameAttributes = target.Read<uint>(address + (ulong)type.Fields[nameof(FrameAttributes)].Offset);
        FCallEntry = target.ReadPointer(address + (ulong)type.Fields[nameof(FCallEntry)].Offset);
        LazyMachState = target.ProcessedData.GetOrAdd<LazyMachState>(address + (ulong)type.Fields[nameof(LazyMachState)].Offset);
    }

    public DataType Type { get; }
    public uint FrameAttributes { get; }
    public TargetPointer FCallEntry { get; }
    public LazyMachState LazyMachState { get; }
}
