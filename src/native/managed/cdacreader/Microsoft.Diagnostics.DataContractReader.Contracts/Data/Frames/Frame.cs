// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Frame : IData<Frame>
{
    private static readonly List<string> SupportedFrameTypes = [
        "InlinedCallFrame",
        "HelperMethodFrame",
        "HelperMethodFrame_1OBJ",
        "HelperMethodFrame_2OBJ",
        "HelperMethodFrame_3OBJ",
        "HelperMethodFrame_PROTECTOBJ",
        "DebuggerU2MCatchHandlerFrame",
        "DynamicHelperFrame",
    ];

    static Frame IData<Frame>.Create(Target target, TargetPointer address)
        => new Frame(target, address);

    public Frame(Target target, TargetPointer address)
    {
        Target.TypeInfo type = target.GetTypeInfo(DataType.Frame);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        Type = FindType(target, address);
    }

    private static DataType FindType(Target target, TargetPointer address)
    {
        TargetPointer instanceVptr = target.ReadPointer(address);

        foreach (string frameTypeName in SupportedFrameTypes)
        {
            TargetPointer typeVptr = target.ReadGlobalPointer(frameTypeName + "VPtr");
            if (instanceVptr == typeVptr)
            {
                return Enum.TryParse(frameTypeName, out DataType type) ? type : DataType.Unknown;
            }
        }

        return DataType.Unknown;
    }

    public TargetPointer Next { get; init; }
    public DataType Type { get; init; }
}
