// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Contracts.Extensions;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Data;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class StackWalk_1 : IStackWalk
{
    private readonly Target _target;
    private readonly IExecutionManager _eman;

    internal StackWalk_1(Target target)
    {
        _target = target;
        _eman = target.Contracts.ExecutionManager;
    }

    public enum StackWalkState
    {
        SW_COMPLETE,
        SW_ERROR,

        // The current Context is managed
        SW_FRAMELESS,

        // The current Context is unmanaged.
        // The next update will use a Frame to get a managed context
        // When SW_FRAME, the FrameAddress is valid
        SW_FRAME,
        SW_SKIPPED_FRAME,
    }

    private record StackDataFrameHandle(
        IPlatformAgnosticContext Context,
        StackWalkState State,
        TargetPointer FrameAddress,
        ThreadData ThreadData) : IStackDataFrameHandle
    { }

    private class StackWalkData(IPlatformAgnosticContext context, StackWalkState state, FrameIterator frameIter, ThreadData threadData)
    {
        public IPlatformAgnosticContext Context { get; set; } = context;
        public StackWalkState State { get; set; } = state;
        public FrameIterator FrameIter { get; set; } = frameIter;
        public ThreadData ThreadData { get; set; } = threadData;

        public StackDataFrameHandle ToDataFrame() => new(Context.Clone(), State, FrameIter.CurrentFrameAddress, ThreadData);
    }

    IEnumerable<IStackDataFrameHandle> IStackWalk.CreateStackWalk(ThreadData threadData)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        FillContextFromThread(context, threadData);
        StackWalkState state = IsManaged(context.InstructionPointer, out _) ? StackWalkState.SW_FRAMELESS : StackWalkState.SW_FRAME;
        FrameIterator frameIterator = new(_target, threadData);

        // if the next Frame is not valid and we are not in managed code, there is nothing to return
        if (state == StackWalkState.SW_FRAME && !frameIterator.IsValid())
        {
            yield break;
        }

        StackWalkData stackWalkData = new(context, state, frameIterator, threadData);

        yield return stackWalkData.ToDataFrame();

        while (Next(stackWalkData))
        {
            yield return stackWalkData.ToDataFrame();
        }
    }

    void IStackWalk.WalkStackReferences(ThreadData threadData)
    {
        // TODO(stackref): This isn't quite right. We need to check if the FilterContext or ProfilerFilterContext
        // is set and prefer that if either is not null.
        IEnumerable<IStackDataFrameHandle> stackFrames = ((IStackWalk)this).CreateStackWalk(threadData);
        IEnumerable<StackDataFrameHandle> frames = stackFrames.Select(AssertCorrectHandle);
        IEnumerable<GCFrameData> gcFrames = Filter(frames);

        GcScanContext scanContext = new(_target, resolveInteriorPointers: false);

        foreach (GCFrameData gcFrame in gcFrames)
        {
            Console.WriteLine(gcFrame);

            TargetPointer pMethodDesc = ((IStackWalk)this).GetMethodDescPtr(gcFrame.Frame);

            bool reportGcReferences = gcFrame.ShouldCrawlFrameReportGCReferences;

            try
            {
                TargetPointer pFrame = ((IStackWalk)this).GetFrameAddress(gcFrame.Frame);
                scanContext.UpdateScanContext(
                    gcFrame.Frame.Context.StackPointer,
                    gcFrame.Frame.Context.InstructionPointer,
                    pFrame);

                if (reportGcReferences)
                {
                    if (IsFrameless(gcFrame.Frame))
                    {
                        // TODO(stackref): are the "GetCodeManagerFlags" flags relevant?
                        if (!IsManaged(gcFrame.Frame.Context.InstructionPointer, out CodeBlockHandle? cbh))
                            throw new InvalidOperationException("Expected managed code");
                        GcScanner gcScanner = new(_target);
                        gcScanner.EnumGcRefs(gcFrame.Frame.Context, cbh.Value, scanContext);
                    }
                    else
                    {

                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Exception during WalkStackReferences: {ex}");
                // TODO(stackref): Handle exceptions properly
            }
        }
    }

    private record GCFrameData
    {
        public GCFrameData(StackDataFrameHandle frame)
        {
            Frame = frame;
        }

        public StackDataFrameHandle Frame { get; }
        public bool IsFilterFunclet { get; set; }
        public bool IsFilterFuncletCached { get; set; }
        public bool ShouldParentToFuncletSkipReportingGCReferences { get; set; }
        public bool ShouldCrawlFrameReportGCReferences { get; set; } // required
        public bool ShouldParentFrameUseUnwindTargetPCforGCReporting { get; set; }
        public bool ShouldSaveFuncletInfo { get; set; }
        public bool ShouldParentToFuncletReportSavedFuncletSlots { get; set; }
    }

    private enum ForceGcReportingStage
    {
        Off,
        LookForManagedFrame,
        LookForMarkerFrame,
    }

    private IEnumerable<GCFrameData> Filter(IEnumerable<StackDataFrameHandle> handles)
    {
        // StackFrameIterator::Filter assuming GC_FUNCLET_REFERENCE_REPORTING is defined

        // global tracking variables
        bool movedPastFirstExInfo = false;
        bool processNonFilterFunclet = false;
        bool processIntermediaryNonFilterFunclet = false;
        bool didFuncletReportGCReferences = true;
        bool funcletNotSeen = false;
        TargetPointer parentStackFrame = TargetPointer.Null;
        TargetPointer funcletParentStackFrame = TargetPointer.Null;
        TargetPointer intermediaryFuncletParentStackFrame;

        ForceGcReportingStage forceReportingWhileSkipping = ForceGcReportingStage.Off;
        bool foundFirstFunclet = false;

        foreach (StackDataFrameHandle handle in handles)
        {
            GCFrameData gcFrame = new(handle);

            // per-frame tracking variables
            bool stop = false;
            bool skippingFunclet = false;
            bool recheckCurrentFrame = false;
            bool skipFuncletCallback = true;

            TargetPointer pExInfo = GetCurrentExceptionTracker(handle);
            TargetPointer frameSp = handle.State == StackWalkState.SW_FRAME ? handle.FrameAddress : handle.Context.StackPointer;
            if (pExInfo != TargetPointer.Null && frameSp > pExInfo)
            {
                if (!movedPastFirstExInfo)
                {
                    Data.ExceptionInfo exInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(pExInfo);
                    if (exInfo.PassNumber == 2 &&
                        exInfo.CSFEnclosingClause != TargetPointer.Null &&
                        funcletParentStackFrame == TargetPointer.Null &&
                        exInfo.LastReportedFuncletInfo.IP != TargetCodePointer.Null)
                    {
                        // We are in the 2nd pass and we have already called an exceptionally called
                        // finally funclet and reported that to GC in a previous GC run. But we have
                        // not seen any funclet on the call stack yet.
                        // Simulate that we have actualy seen a finally funclet during this pass and
                        // that it didn't report GC references to ensure that the references will be
                        // reported by the parent correctly.
                        funcletParentStackFrame = exInfo.CSFEnclosingClause;
                        parentStackFrame = exInfo.CSFEnclosingClause;
                        processNonFilterFunclet = true;
                        didFuncletReportGCReferences = false;
                        funcletNotSeen = true;
                    }
                    movedPastFirstExInfo = true;
                }
            }

            gcFrame.ShouldParentToFuncletReportSavedFuncletSlots = false;

            // by default, there is no funclet for the current frame
            // that reported GC references
            gcFrame.ShouldParentToFuncletSkipReportingGCReferences = false;

            // by default, assume that we are going to report GC references
            gcFrame.ShouldCrawlFrameReportGCReferences = true;

            gcFrame.ShouldSaveFuncletInfo = false;

            // by default, assume that parent frame is going to report GC references from
            // the actual location reported by the stack walk
            gcFrame.ShouldParentFrameUseUnwindTargetPCforGCReporting = false;

            if (parentStackFrame != TargetPointer.Null)
            {
                // we are now skipping frames to get to the funclet's parent
                skippingFunclet = true;
            }

            switch (handle.State)
            {
                case StackWalkState.SW_FRAMELESS:
                    do
                    {
                        recheckCurrentFrame = false;
                        if (funcletParentStackFrame != TargetPointer.Null)
                        {
                            // Have we been processing a filter funclet without encountering any non-filter funclets?
                            if (!processNonFilterFunclet && !processIntermediaryNonFilterFunclet)
                            {
                                if (IsUnwoundToTargetParentFrame(handle, funcletParentStackFrame))
                                {
                                    gcFrame.ShouldParentToFuncletSkipReportingGCReferences = false;

                                    /* ResetGCRefReportingState */
                                    funcletParentStackFrame = TargetPointer.Null;
                                    processNonFilterFunclet = false;
                                    intermediaryFuncletParentStackFrame = TargetPointer.Null;
                                    processIntermediaryNonFilterFunclet = false;

                                    // We have reached the parent of the filter funclet.
                                    // It is possible this is another funclet (e.g. a catch/fault/finally),
                                    // so reexamine this frame and see if it needs any skipping.
                                    recheckCurrentFrame = true;
                                }
                                else
                                {
                                    Debug.Assert(!IsFilterFunclet(handle));
                                    if (IsFunclet(handle))
                                    {
                                        intermediaryFuncletParentStackFrame = FindParentStackFrameForStackWalk(handle, forGCReporting: true);
                                        Debug.Assert(intermediaryFuncletParentStackFrame != TargetPointer.Null);
                                        processIntermediaryNonFilterFunclet = true;

                                        // Set the parent frame so that the funclet skipping logic (below) can use it.
                                        parentStackFrame = intermediaryFuncletParentStackFrame;
                                        skippingFunclet = false;

                                        IPlatformAgnosticContext callerContext = handle.Context.Clone();
                                        callerContext.Unwind(_target);
                                        if (!IsManaged(callerContext.InstructionPointer, out _))
                                        {
                                            // Initiate force reporting of references in the new managed exception handling code frames.
                                            // These frames are still alive when we are in a finally funclet.
                                            forceReportingWhileSkipping = ForceGcReportingStage.LookForManagedFrame;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.Assert(funcletParentStackFrame == TargetPointer.Null);

                            // We don't have any funclet parent reference. Check if the current frame represents a funclet.
                            if (IsFunclet(handle))
                            {
                                // Get a reference to the funclet's parent frame.
                                funcletParentStackFrame = FindParentStackFrameForStackWalk(handle, forGCReporting: true);

                                bool frameWasUnwound = HasFrameBeenUnwoundByAnyActiveException(handle);

                                if (funcletParentStackFrame == TargetPointer.Null)
                                {
                                    Debug.Assert(frameWasUnwound, "This can only happen if the funclet (and its parent) have been unwound");
                                }
                                else
                                {
                                    Debug.Assert(funcletParentStackFrame != TargetPointer.Null);

                                    bool isFilterFunclet = IsFilterFunclet(handle);

                                    if (!isFilterFunclet)
                                    {
                                        processNonFilterFunclet = true;

                                        // Set the parent frame so that the funclet skipping logic (below) can use it.
                                        parentStackFrame = funcletParentStackFrame;

                                        if (!foundFirstFunclet &&
                                            pExInfo > handle.Context.StackPointer &&
                                            parentStackFrame > pExInfo)
                                        {
                                            Debug.Assert(pExInfo != TargetPointer.Null);
                                            gcFrame.ShouldSaveFuncletInfo = true;
                                            foundFirstFunclet = true;
                                        }

                                        IPlatformAgnosticContext callerContext = handle.Context.Clone();
                                        callerContext.Unwind(_target);
                                        if (!frameWasUnwound && IsManaged(callerContext.InstructionPointer, out _))
                                        {
                                            // Initiate force reporting of references in the new managed exception handling code frames.
                                            // These frames are still alive when we are in a finally funclet.
                                            forceReportingWhileSkipping = ForceGcReportingStage.LookForManagedFrame;
                                        }

                                        // For non-filter funclets, we will make the callback for the funclet
                                        // but skip all the frames until we reach the parent method. When we do,
                                        // we will make a callback for it as well and then continue to make callbacks
                                        // for all upstack frames, until we reach another funclet or the top of the stack
                                        // is reached.
                                        skipFuncletCallback = false;
                                    }
                                    else
                                    {
                                        Debug.Assert(isFilterFunclet);
                                        processNonFilterFunclet = false;

                                        // Nothing more to do as we have come across a filter funclet. In this case, we will:
                                        //
                                        // 1) Get a reference to the parent frame
                                        // 2) Report the funclet
                                        // 3) Continue to report the parent frame, along with a flag that funclet has been reported (see above)
                                        // 4) Continue to report all upstack frames
                                    }
                                }
                            }
                        }
                    } while (recheckCurrentFrame);

                    if (processNonFilterFunclet || processIntermediaryNonFilterFunclet)
                    {
                        bool skipFrameDueToUnwind = false;

                        if (HasFrameBeenUnwoundByAnyActiveException(handle))
                        {
                            // This frame has been unwound by an active exception. It is not part of the live stack.
                            gcFrame.ShouldCrawlFrameReportGCReferences = false;
                            skipFrameDueToUnwind = true;

                            if (IsFunclet(handle) && !skippingFunclet)
                            {
                                // we have come across a funclet that has been unwound and we haven't yet started to
                                // look for its parent.  in such a case, the funclet will not have anything to report
                                // so set the corresponding flag to indicate so.

                                Debug.Assert(didFuncletReportGCReferences);
                                didFuncletReportGCReferences = false;
                            }
                        }

                        if (skipFrameDueToUnwind)
                        {
                            if (parentStackFrame != TargetPointer.Null)
                            {
                                // Check if our have reached our target method frame.
                                // parentStackFrame == MaxValue is a special value to indicate that we should skip one frame.
                                if (parentStackFrame == TargetPointer.PlatformMaxValue(_target) ||
                                    IsUnwoundToTargetParentFrame(handle, parentStackFrame))
                                {
                                    // Reset flag as we have reached target method frame so no more skipping required
                                    skippingFunclet = false;

                                    // We've finished skipping as told.  Now check again.

                                    if (processIntermediaryNonFilterFunclet || processNonFilterFunclet)
                                    {
                                        gcFrame.ShouldParentToFuncletSkipReportingGCReferences = true;

                                        didFuncletReportGCReferences = true;

                                        /* ResetGCRefReportingState */
                                        if (!processIntermediaryNonFilterFunclet)
                                        {
                                            funcletParentStackFrame = TargetPointer.Null;
                                            processNonFilterFunclet = false;
                                        }
                                        intermediaryFuncletParentStackFrame = TargetPointer.Null;
                                        processIntermediaryNonFilterFunclet = false;
                                    }

                                    parentStackFrame = TargetPointer.Null;

                                    if (IsFunclet(handle))
                                    {
                                        // We have reached another funclet.  Reexamine this frame.
                                        recheckCurrentFrame = true;
                                        goto case StackWalkState.SW_FRAMELESS;
                                    }
                                }
                            }

                            if (gcFrame.ShouldCrawlFrameReportGCReferences)
                            {
                                // Skip the callback for this frame - we don't do this for unwound frames encountered
                                // in GC stackwalk since they may represent dynamic methods whose resolver objects
                                // the GC may need to keep alive.
                                break;
                            }
                        }
                        else
                        {
                            Debug.Assert(!skipFrameDueToUnwind);

                            if (parentStackFrame != TargetPointer.Null)
                            {
                                // Check if our have reached our target method frame.
                                // parentStackFrame == MaxValue is a special value to indicate that we should skip one frame.
                                if (parentStackFrame == TargetPointer.PlatformMaxValue(_target) ||
                                    IsUnwoundToTargetParentFrame(handle, parentStackFrame))
                                {
                                    if (processIntermediaryNonFilterFunclet || processNonFilterFunclet)
                                    {
                                        bool shouldSkipReporting = true;

                                        if (!didFuncletReportGCReferences)
                                        {
                                            Debug.Assert(pExInfo != TargetPointer.Null);
                                            Data.ExceptionInfo exInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(pExInfo);
                                            if (exInfo.CallerOfActualHandlerFrame == funcletParentStackFrame)
                                            {
                                                shouldSkipReporting = false;

                                                didFuncletReportGCReferences = true;

                                                gcFrame.ShouldParentFrameUseUnwindTargetPCforGCReporting = true;

                                                // TODO(stackref): Is this required?
                                                // gcFrame.ehClauseForCatch = exInfo.ClauseForCatch;
                                            }
                                            else if (!IsFunclet(handle))
                                            {
                                                if (funcletNotSeen)
                                                {
                                                    gcFrame.ShouldParentToFuncletReportSavedFuncletSlots = true;
                                                    funcletNotSeen = false;
                                                }

                                                didFuncletReportGCReferences = true;
                                            }
                                        }
                                        gcFrame.ShouldParentToFuncletSkipReportingGCReferences = shouldSkipReporting;

                                        /* ResetGCRefReportingState */
                                        if (!processIntermediaryNonFilterFunclet)
                                        {
                                            funcletParentStackFrame = TargetPointer.Null;
                                            processNonFilterFunclet = false;
                                        }
                                        intermediaryFuncletParentStackFrame = TargetPointer.Null;
                                        processIntermediaryNonFilterFunclet = false;
                                    }

                                    parentStackFrame = TargetPointer.Null;
                                }
                            }

                            if (parentStackFrame == TargetPointer.Null && IsFunclet(handle))
                            {
                                recheckCurrentFrame = true;
                                goto case StackWalkState.SW_FRAMELESS;
                            }

                            if (skipFuncletCallback)
                            {
                                if (parentStackFrame != TargetPointer.Null &&
                                    forceReportingWhileSkipping == ForceGcReportingStage.Off)
                                {
                                    break;
                                }

                                if (forceReportingWhileSkipping == ForceGcReportingStage.LookForManagedFrame)
                                {
                                    // State indicating that the next marker frame should turn off the reporting again. That would be the caller of the managed RhThrowEx
                                    forceReportingWhileSkipping = ForceGcReportingStage.LookForMarkerFrame;
                                    // TODO(stackref): need to add case to find the marker frame
                                }

                                if (forceReportingWhileSkipping != ForceGcReportingStage.Off)
                                {
                                    // TODO(stackref): add debug assert that we are in the EH code
                                }
                            }
                        }
                    }
                    else
                    {
                        // If we are enumerating frames for GC reporting and we determined that
                        // the current frame needs to be reported, ensure that it has not already
                        // been unwound by the active exception. If it has been, then we will
                        // simply skip it and not deliver a callback for it.
                        if (HasFrameBeenUnwoundByAnyActiveException(handle))
                        {
                            // Invoke the GC callback for this crawlframe (to keep any dynamic methods alive) but do not report its references.
                            gcFrame.ShouldCrawlFrameReportGCReferences = false;
                        }
                    }

                    stop = true;
                    break;

                case StackWalkState.SW_FRAME:
                case StackWalkState.SW_SKIPPED_FRAME:
                    if (!skippingFunclet)
                    {
                        if (HasFrameBeenUnwoundByAnyActiveException(handle))
                        {
                            // This frame has been unwound by an active exception. It is not part of the live stack.
                            gcFrame.ShouldCrawlFrameReportGCReferences = false;
                        }
                        stop = true;
                    }
                    break;
                default:
                    stop = true;
                    break;
            }

            if (stop)
                yield return gcFrame;
        }
    }

    private bool IsUnwoundToTargetParentFrame(StackDataFrameHandle handle, TargetPointer targetParentFrame)
    {
        Debug.Assert(handle.State is StackWalkState.SW_FRAMELESS);

        IPlatformAgnosticContext callerContext = handle.Context.Clone();
        callerContext.Unwind(_target);

        return callerContext.StackPointer == targetParentFrame;
    }

    private bool Next(StackWalkData handle)
    {
        switch (handle.State)
        {
            case StackWalkState.SW_FRAMELESS:
                try
                {
                    handle.Context.Unwind(_target);
                }
                catch
                {
                    handle.State = StackWalkState.SW_ERROR;
                    throw;
                }
                break;
            case StackWalkState.SW_SKIPPED_FRAME:
                handle.FrameIter.Next();
                break;
            case StackWalkState.SW_FRAME:
                handle.FrameIter.UpdateContextFromFrame(handle.Context);
                if (!handle.FrameIter.IsInlineCallFrameWithActiveCall())
                {
                    handle.FrameIter.Next();
                }
                break;
            case StackWalkState.SW_ERROR:
            case StackWalkState.SW_COMPLETE:
                return false;
        }
        UpdateState(handle);

        return handle.State is not (StackWalkState.SW_ERROR or StackWalkState.SW_COMPLETE);
    }

    private void UpdateState(StackWalkData handle)
    {
        // If we are complete or in a bad state, no updating is required.
        if (handle.State is StackWalkState.SW_ERROR or StackWalkState.SW_COMPLETE)
        {
            return;
        }

        bool isManaged = IsManaged(handle.Context.InstructionPointer, out _);
        bool validFrame = handle.FrameIter.IsValid();

        if (isManaged)
        {
            handle.State = StackWalkState.SW_FRAMELESS;
            if (CheckForSkippedFrames(handle))
            {
                handle.State = StackWalkState.SW_SKIPPED_FRAME;
                return;
            }
        }
        else
        {
            handle.State = validFrame ? StackWalkState.SW_FRAME : StackWalkState.SW_COMPLETE;
        }
    }

    /// <summary>
    /// If an explicit frame is allocated in a managed stack frame (e.g. an inlined pinvoke call),
    /// we may have skipped an explicit frame.  This function checks for them.
    /// </summary>
    /// <returns> true if there are skipped frames. </returns>
    private bool CheckForSkippedFrames(StackWalkData handle)
    {
        // ensure we can find the caller context
        Debug.Assert(IsManaged(handle.Context.InstructionPointer, out _));

        // if there are no more Frames, vacuously false
        if (!handle.FrameIter.IsValid())
        {
            return false;
        }

        // get the caller context
        IPlatformAgnosticContext parentContext = handle.Context.Clone();
        parentContext.Unwind(_target);

        return handle.FrameIter.CurrentFrameAddress.Value < parentContext.StackPointer.Value;
    }

    byte[] IStackWalk.GetRawContext(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        return handle.Context.GetBytes();
    }

    TargetPointer IStackWalk.GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        if (handle.State is StackWalkState.SW_FRAME or StackWalkState.SW_SKIPPED_FRAME)
        {
            return handle.FrameAddress;
        }
        return TargetPointer.Null;
    }

    string IStackWalk.GetFrameName(TargetPointer frameIdentifier)
        => FrameIterator.GetFrameName(_target, frameIdentifier);

    TargetPointer IStackWalk.GetMethodDescPtr(TargetPointer framePtr)
        => FrameIterator.GetMethodDescPtr(framePtr, _target);

    TargetPointer IStackWalk.GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);

        // if we are at a capital F Frame, we can get the method desc from the frame
        TargetPointer framePtr = ((IStackWalk)this).GetFrameAddress(handle);
        if (framePtr != TargetPointer.Null)
            return ((IStackWalk)this).GetMethodDescPtr(framePtr);

        // otherwise try to get the method desc from the IP
        if (!IsManaged(handle.Context.InstructionPointer, out CodeBlockHandle? codeBlockHandle))
            return TargetPointer.Null;

        return _eman.GetMethodDesc(codeBlockHandle.Value);
    }

    private bool IsManaged(TargetPointer ip, [NotNullWhen(true)] out CodeBlockHandle? codeBlockHandle)
    {
        TargetCodePointer codePointer = CodePointerUtils.CodePointerFromAddress(ip, _target);
        if (_eman.GetCodeBlockHandle(codePointer) is CodeBlockHandle cbh && cbh.Address != TargetPointer.Null)
        {
            codeBlockHandle = cbh;
            return true;
        }
        codeBlockHandle = default;
        return false;
    }

    private unsafe void FillContextFromThread(IPlatformAgnosticContext context, ThreadData threadData)
    {
        byte[] bytes = new byte[context.Size];
        Span<byte> buffer = new Span<byte>(bytes);
        // The underlying ICLRDataTarget.GetThreadContext has some variance depending on the host.
        // SOS's managed implementation sets the ContextFlags to platform specific values defined in ThreadService.cs (diagnostics repo)
        // SOS's native implementation keeps the ContextFlags passed into this function.
        // To match the DAC behavior, the DefaultContextFlags are what the DAC passes in in DacGetThreadContext.
        // In most implementations, this will be overridden by the host, but in some cases, it may not be.
        if (!_target.TryGetThreadContext(threadData.OSId.Value, context.DefaultContextFlags, buffer))
        {
            throw new InvalidOperationException($"GetThreadContext failed for thread {threadData.OSId.Value}");
        }

        context.FillFromBuffer(buffer);
    }

    private static StackDataFrameHandle AssertCorrectHandle(IStackDataFrameHandle stackDataFrameHandle)
    {
        if (stackDataFrameHandle is not StackDataFrameHandle handle)
        {
            throw new ArgumentException("Invalid stack data frame handle", nameof(stackDataFrameHandle));
        }

        return handle;
    }
};
