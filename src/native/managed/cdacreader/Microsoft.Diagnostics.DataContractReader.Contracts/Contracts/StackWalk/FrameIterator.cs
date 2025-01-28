// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class FrameIterator
{
    public static IEnumerable<Data.Frame> EnumerateFrames(Target target, TargetPointer framePointer)
    {
        TargetPointer terminator = new TargetPointer(target.PointerSize == 8 ? ulong.MaxValue : uint.MaxValue);

        while (framePointer != terminator)
        {
            Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(framePointer);
            yield return frame;
            framePointer = frame.Next;
        }
    }

    public static bool TryGetContext(Target target, Data.Frame frame, [NotNullWhen(true)] out TargetPointer? IP, [NotNullWhen(true)] out TargetPointer? SP)
    {
        switch (frame.Type)
        {
            case DataType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                IP = inlinedCallFrame.CallerReturnAddress;
                SP = inlinedCallFrame.CallSiteSP;
                return true;
            case DataType.HelperMethodFrame:
            case DataType.HelperMethodFrame_1OBJ:
            case DataType.HelperMethodFrame_2OBJ:
            case DataType.HelperMethodFrame_3OBJ:
            case DataType.HelperMethodFrame_PROTECTOBJ:
                Data.HelperMethodFrame helperMethodFrame = target.ProcessedData.GetOrAdd<Data.HelperMethodFrame>(frame.Address);
                if (helperMethodFrame.LazyMachState.StackPointer is null || helperMethodFrame.LazyMachState.InstructionPointer is null)
                {
                    IP = null;
                    SP = null;
                    return false;
                }
                IP = helperMethodFrame.LazyMachState.InstructionPointer;
                SP = helperMethodFrame.LazyMachState.StackPointer;
                return true;
            default:
                IP = null;
                SP = null;
                Console.WriteLine($"Unable to parse frame further: {frame.Type}");
                break;
        }
        return false;
    }

    public static void PrintFrame(Target target, Data.Frame frame)
    {
        switch (frame.Type)
        {
            case DataType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                Print(inlinedCallFrame);
                break;
            case DataType.HelperMethodFrame:
            case DataType.HelperMethodFrame_1OBJ:
            case DataType.HelperMethodFrame_2OBJ:
            case DataType.HelperMethodFrame_3OBJ:
            case DataType.HelperMethodFrame_PROTECTOBJ:
                Data.HelperMethodFrame helperMethodFrame = target.ProcessedData.GetOrAdd<Data.HelperMethodFrame>(frame.Address);
                Print(helperMethodFrame);
                break;
            default:
                Console.WriteLine($"Unable to parse frame further: {frame.Type}");
                break;
        }
    }

    public static void Print(InlinedCallFrame inlinedCallFrame)
    {
        Console.WriteLine($"[InlinedCallFrame: IP={inlinedCallFrame.CallerReturnAddress}, SP={inlinedCallFrame.CallSiteSP}]");
    }

    public static void Print(HelperMethodFrame helperMethodFrame)
    {
        bool isValid = helperMethodFrame.LazyMachState.ReturnAddress != 0;
        if (isValid)
        {
            Console.WriteLine($"[{helperMethodFrame.Type}: IP={helperMethodFrame.LazyMachState.InstructionPointer}, SP={helperMethodFrame.LazyMachState.StackPointer}, RA={helperMethodFrame.LazyMachState.ReturnAddress}]");
        }
        else
        {
            Console.WriteLine($"[{helperMethodFrame.Type}: IP={helperMethodFrame.LazyMachState.InstructionPointer}, Invalid LazyMachState]");
        }
    }
}
