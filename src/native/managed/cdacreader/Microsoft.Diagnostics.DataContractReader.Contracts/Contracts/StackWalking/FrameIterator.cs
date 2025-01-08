// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class FrameIterator
{
    public static void EnumerateFrames(Target target, TargetPointer framePointer)
    {
        string outputhPath = "C:\\Users\\maxcharlamb\\OneDrive - Microsoft\\Desktop\\out.txt";
        using StreamWriter writer = new StreamWriter(outputhPath);
        Console.SetOut(writer);

        TargetPointer terminator = new TargetPointer(target.PointerSize == 8 ? ulong.MaxValue : uint.MaxValue);

        while (framePointer != terminator)
        {
            Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(framePointer);
            HandleFrame(target, framePointer);
            framePointer = frame.Next;
        }

        writer.Flush();
    }

    public static void HandleFrame(Target target, TargetPointer framePointer)
    {
        Data.Frame frame = target.ProcessedData.GetOrAdd<Data.Frame>(framePointer);
        switch (frame.Type)
        {
            case DataType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(framePointer);
                Print(inlinedCallFrame);
                break;
            case DataType.HelperMethodFrame:
            case DataType.HelperMethodFrame_1OBJ:
            case DataType.HelperMethodFrame_2OBJ:
            case DataType.HelperMethodFrame_3OBJ:
            case DataType.HelperMethodFrame_PROTECTOBJ:
                Data.HelperMethodFrame helperMethodFrame = target.ProcessedData.GetOrAdd<Data.HelperMethodFrame>(framePointer);
                Print(helperMethodFrame);
                break;
            default:
                Console.WriteLine($"Unknown frame type: {frame.Type}");
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
