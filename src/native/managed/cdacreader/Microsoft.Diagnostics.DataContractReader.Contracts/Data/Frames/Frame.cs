// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Diagnostics.DataContractReader.Data;

internal sealed class Frame : IData<Frame>
{
    private static readonly List<DataType> SupportedFrameTypes = [
        DataType.InlinedCallFrame,
        DataType.HelperMethodFrame,
        DataType.HelperMethodFrame_1OBJ,
        DataType.HelperMethodFrame_2OBJ,
        DataType.HelperMethodFrame_3OBJ,
        DataType.HelperMethodFrame_PROTECTOBJ,

        DataType.ResumableFrame,
        DataType.RedirectedTHreadFrame,
        DataType.FaultingExceptionFrame,
        DataType.SoftwareExceptionFrame,
        DataType.FuncEvalFrame,
        DataType.UnmanagedToManagedFrame,
        DataType.ComMethodFrame,
        DataType.CLRToCOMMethodFrame,
        DataType.ComPrestubMethodFrame,
        DataType.PInvokeCalliFrame,
        DataType.HijackFrame,
        DataType.PrestubMethodFrame,
        DataType.CallCountingHelperFrame,
        DataType.StubDispatchFrame,
        DataType.ExternalMethodFrame,
        DataType.DynamicHelperFrame,
        DataType.InterpreterFrame,
        DataType.ProtectByRefsFrame,
        DataType.ProtectValueClassFrame,
        DataType.DebuggerClassInitMarkFrame,
        DataType.DebuggerSecurityCodeMarkFrame,
        DataType.DebuggerExitFrame,
        DataType.DebuggerU2MCatchHandlerFrame,
        DataType.TailCallFrame,
        DataType.ExceptionFilterFrame,
        DataType.AssumeByrefFromJITStack,
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
            }
            catch (InvalidOperationException)
            {
                continue;
            }
            if (instanceVptr == typeVptr)
            {
                return frameType;
            }
        }

        return DataType.Unknown;
    }

    public TargetPointer Address { get; init; }
    public TargetPointer Next { get; init; }
    public DataType Type { get; init; }
}
