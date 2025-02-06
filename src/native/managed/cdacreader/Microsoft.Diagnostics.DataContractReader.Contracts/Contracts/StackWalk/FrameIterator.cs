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
    private readonly Target target;
    private readonly TargetPointer terminator;
    private TargetPointer currentFramePointer;

    internal Data.Frame CurrentFrame => target.ProcessedData.GetOrAdd<Data.Frame>(currentFramePointer);

    public TargetPointer CurrentFrameAddress => currentFramePointer;

    public FrameIterator(Target target, ThreadData threadData)
    {
        this.target = target;
        terminator = new TargetPointer(target.PointerSize == 8 ? ulong.MaxValue : uint.MaxValue);
        currentFramePointer = threadData.Frame;
    }

    public bool IsValid()
    {
        return currentFramePointer != terminator;
    }

    public bool Next()
    {
        if (currentFramePointer == terminator)
            return false;

        currentFramePointer = CurrentFrame.Next;
        return true;
    }

    public bool TryUpdateContext(ref IContext context)
    {
        switch (CurrentFrame.Type)
        {
            case DataType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(CurrentFrame.Address);
                context.Clear();
                context.InstructionPointer = inlinedCallFrame.CallerReturnAddress;
                context.StackPointer = inlinedCallFrame.CallSiteSP;
                context.FramePointer = inlinedCallFrame.CalleeSavedFP;
                return true;
            case DataType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame softwareExceptionFrame = target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(CurrentFrame.Address);
                context.ReadFromAddress(target, softwareExceptionFrame.TargetContext);
                return true;
            default:
                return false;
        }
    }

    public bool IsInlineCallFrameWithActiveCall()
    {
        if (CurrentFrame.Type != DataType.InlinedCallFrame)
        {
            return false;
        }
        Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(currentFramePointer);
        return inlinedCallFrame.CallerReturnAddress != 0;
    }
}
