// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal sealed class FrameIterator
{
    private enum FrameType
    {
        Unknown,

        InlinedCallFrame,
        SoftwareExceptionFrame,

        /* TransitionFrame Types */
        FramedMethodFrame,
        CLRToCOMMethodFrame,
        PInvokeCalliFrame,
        PrestubMethodFrame,
        StubDispatchFrame,
        CallCountingHelperFrame,
        ExternalMethodFrame,
        DynamicHelperFrame,

        FuncEvalFrame,

        /* ResumableFrame Types */
        ResumableFrame,
        RedirectedThreadFrame,

        FaultingExceptionFrame,

        HijackFrame,
    }

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

    public bool TryUpdateContext(IPlatformAgnosticContext context)
    {
        switch (GetFrameType(CurrentFrame))
        {
            case FrameType.InlinedCallFrame:
                Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(CurrentFrame.Address);
                return GetFrameHandler(context).HandleInlinedCallFrame(inlinedCallFrame);

            case FrameType.SoftwareExceptionFrame:
                Data.SoftwareExceptionFrame softwareExceptionFrame = target.ProcessedData.GetOrAdd<Data.SoftwareExceptionFrame>(CurrentFrame.Address);
                return GetFrameHandler(context).HandleSoftwareExceptionFrame(softwareExceptionFrame);

            // TransitionFrame type frames
            case FrameType.FramedMethodFrame:
            case FrameType.CLRToCOMMethodFrame:
            case FrameType.PInvokeCalliFrame:
            case FrameType.PrestubMethodFrame:
            case FrameType.StubDispatchFrame:
            case FrameType.CallCountingHelperFrame:
            case FrameType.ExternalMethodFrame:
            case FrameType.DynamicHelperFrame:
                Data.FramedMethodFrame framedMethodFrame = target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(CurrentFrame.Address);
                Data.TransitionBlock transitionBlock = target.ProcessedData.GetOrAdd<Data.TransitionBlock>(framedMethodFrame.TransitionBlockPtr);
                if (target.GetTypeInfo(DataType.TransitionBlock).Size is not uint transitionBlockSize)
                {
                    throw new InvalidOperationException("TransitionBlock size is not set");
                }
                return GetFrameHandler(context).HandleTransitionFrame(framedMethodFrame, transitionBlock, transitionBlockSize);

            case FrameType.FuncEvalFrame:
                Data.FuncEvalFrame funcEvalFrame = target.ProcessedData.GetOrAdd<Data.FuncEvalFrame>(CurrentFrame.Address);
                Data.DebuggerEval debuggerEval = target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEvalFrame.DebuggerEvalPtr);
                return GetFrameHandler(context).HandleFuncEvalFrame(funcEvalFrame, debuggerEval);

            // ResumableFrame type frames
            case FrameType.ResumableFrame:
            case FrameType.RedirectedThreadFrame:
                Data.ResumableFrame resumableFrame = target.ProcessedData.GetOrAdd<Data.ResumableFrame>(CurrentFrame.Address);
                return GetFrameHandler(context).HandleResumableFrame(resumableFrame);

            case FrameType.FaultingExceptionFrame:
                Data.FaultingExceptionFrame faultingExceptionFrame = target.ProcessedData.GetOrAdd<Data.FaultingExceptionFrame>(CurrentFrame.Address);
                return GetFrameHandler(context).HandleFaultingExceptionFrame(faultingExceptionFrame);

            case FrameType.HijackFrame:
                Data.HijackFrame hijackFrame = target.ProcessedData.GetOrAdd<Data.HijackFrame>(CurrentFrame.Address);
                return GetFrameHandler(context).HandleHijackFrame(hijackFrame);
            default:
                return false;
        }
    }

    public bool IsInlineCallFrameWithActiveCall()
    {
        if (GetFrameType(CurrentFrame) != FrameType.InlinedCallFrame)
        {
            return false;
        }
        Data.InlinedCallFrame inlinedCallFrame = target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(currentFramePointer);
        return inlinedCallFrame.CallerReturnAddress != 0;
    }

    private FrameType GetFrameType(Data.Frame frame)
    {
        foreach (FrameType frameType in Enum.GetValues<FrameType>())
        {
            TargetPointer typeVptr;
            try
            {
                // not all Frames are in all builds, so we need to catch the exception
                typeVptr = target.ReadGlobalPointer(frameType.ToString() + "Identifier");
                if (frame.VPtr == typeVptr)
                {
                    return frameType;
                }
            }
            catch (InvalidOperationException)
            {
            }
        }

        return FrameType.Unknown;
    }

    private IPlatformFrameHandler GetFrameHandler(IPlatformAgnosticContext context)
    {
        return context switch
        {
            ContextHolder<AMD64Context> contextHolder => new AMD64FrameHandler(target, contextHolder),
            ContextHolder<ARM64Context> contextHolder => new ARM64FrameHandler(target, contextHolder),
            _ => throw new InvalidOperationException("Unsupported context type"),
        };
    }
}
