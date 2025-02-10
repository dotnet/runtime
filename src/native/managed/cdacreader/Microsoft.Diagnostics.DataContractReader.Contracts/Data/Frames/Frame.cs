// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Frame : IData<Frame>
{
    private static readonly List<DataType> SupportedFrameTypes = [
        DataType.InlinedCallFrame,
        DataType.SoftwareExceptionFrame,
    ];

    static Frame IData<Frame>.Create(Target target, TargetPointer address)
        => new Frame(target, address);

    public Frame(Target target, TargetPointer address)
    {
        Address = address;
        Target.TypeInfo type = target.GetTypeInfo(DataType.Frame);
        Next = target.ReadPointer(address + (ulong)type.Fields[nameof(Next)].Offset);
        Type = FindType(target, address);
    }

    private static DataType FindType(Target target, TargetPointer address)
    {
        TargetPointer instanceVptr = target.ReadPointer(address);

        foreach (DataType frameType in SupportedFrameTypes)
        {
            TargetPointer typeVptr;
            try
            {
                // not all Frames are in all builds, so we need to catch the exception
                typeVptr = target.ReadGlobalPointer(frameType.ToString() + "VPtr");
                if (instanceVptr == typeVptr)
                {
                    return frameType;
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        return DataType.Unknown;
    }

    public TargetPointer Address { get; init; }
    public TargetPointer Next { get; init; }
    public DataType Type { get; init; }
}
