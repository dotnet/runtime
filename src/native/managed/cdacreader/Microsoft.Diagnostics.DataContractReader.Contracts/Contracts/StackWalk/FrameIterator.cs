// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
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
    public static bool TryUpdateContext(Target target, Data.Frame frame, ref IContext context)
    {
        switch (frame.Type)
        {
            case DataType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(frame.Address);
                context.Clear();
                context.InstructionPointer = inlinedCallFrame.CallerReturnAddress;
                context.StackPointer = inlinedCallFrame.CallSiteSP;
                context.FramePointer = inlinedCallFrame.CalleeSavedFP;
                return true;
            case DataType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame softwareExceptionFrame = target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(frame.Address);
                context.ReadFromAddress(target, softwareExceptionFrame.TargetContext);
                return true;
            case DataType.HelperMethodFrame:
            case DataType.HelperMethodFrame_1OBJ:
            case DataType.HelperMethodFrame_2OBJ:
            case DataType.HelperMethodFrame_3OBJ:
            case DataType.HelperMethodFrame_PROTECTOBJ:
                Data.HelperMethodFrame helperMethodFrame = target.ProcessedData.GetOrAdd<Data.HelperMethodFrame>(frame.Address);
                if (helperMethodFrame.LazyMachState.StackPointer is null || helperMethodFrame.LazyMachState.InstructionPointer is null)
                {
                    return false;
                }
                context.Clear();
                if (helperMethodFrame.LazyMachState.InstructionPointer is TargetPointer ip)
                {
                    context.InstructionPointer = ip;
                }
                if (helperMethodFrame.LazyMachState.StackPointer is TargetPointer sp)
                {
                    context.StackPointer = sp;
                }
                return true;
            default:
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
            case DataType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame softwareExceptionFrame = target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(frame.Address);
                Print(target, softwareExceptionFrame);
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
        Console.WriteLine($"[InlinedCallFrame: IP={inlinedCallFrame.CallerReturnAddress}, SP={inlinedCallFrame.CallSiteSP}, FP={inlinedCallFrame.CalleeSavedFP}]");
    }

    public static void Print(Target target, SoftwareExceptionFrame softwareExceptionFrame)
    {
        IContext context = IContext.GetContextForPlatform(target);
        context.ReadFromAddress(target, softwareExceptionFrame.TargetContext);
        Console.WriteLine($"[SoftwareExceptionFrame: IP={context.InstructionPointer.Value:x16}, SP={context.StackPointer.Value:x16}, FP={context.FramePointer.Value:x16}]");
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
