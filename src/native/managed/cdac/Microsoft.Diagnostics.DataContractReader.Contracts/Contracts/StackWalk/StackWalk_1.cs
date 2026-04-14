// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;
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
        ThreadData ThreadData,
        bool IsResumableFrame = false,
        bool IsActiveFrame = false) : IStackDataFrameHandle
    { }

    private class StackWalkData(IPlatformAgnosticContext context, StackWalkState state, FrameIterator frameIter, ThreadData threadData)
    {
        public IPlatformAgnosticContext Context { get; set; } = context;
        public StackWalkState State { get; set; } = state;
        public FrameIterator FrameIter { get; set; } = frameIter;
        public ThreadData ThreadData { get; set; } = threadData;


        // Track isFirst exactly like native CrawlFrame::isFirst in StackFrameIterator.
        // Starts true, set false after processing a managed (frameless) frame,
        // set back to true when encountering a ResumableFrame (FRAME_ATTR_RESUMABLE).
        public bool IsFirst { get; set; } = true;

        public bool IsCurrentFrameResumable()
        {
            if (State is not (StackWalkState.SW_FRAME or StackWalkState.SW_SKIPPED_FRAME))
                return false;

            var ft = FrameIter.GetCurrentFrameType();
            // Only frame types with FRAME_ATTR_RESUMABLE set isFirst=true.
            // FaultingExceptionFrame has FRAME_ATTR_FAULTED (sets hasFaulted)
            // but NOT FRAME_ATTR_RESUMABLE, so it must not be included here.
            // TODO: HijackFrame only has FRAME_ATTR_RESUMABLE on non-x86 platforms.
            // When x86 stack walking is supported, this should be conditioned on
            // the target architecture.
            return ft is FrameIterator.FrameType.ResumableFrame
                      or FrameIterator.FrameType.RedirectedThreadFrame
                      or FrameIterator.FrameType.HijackFrame;
        }

        /// <summary>
        /// Update the IsFirst state for the NEXT frame, matching native stackwalk.cpp:
        /// - After a frameless frame: isFirst = false (line 2202)
        /// - After a ResumableFrame: isFirst = true (line 2235)
        /// - After other Frames: isFirst = false (implicit in line 2235 assignment)
        /// </summary>
        public void AdvanceIsFirst()
        {
            if (State == StackWalkState.SW_FRAMELESS)
            {
                IsFirst = false;
            }
            else
            {
                IsFirst = IsCurrentFrameResumable();
            }
        }

        public StackDataFrameHandle ToDataFrame()
        {
            bool isResumable = IsCurrentFrameResumable();
            bool isActiveFrame = IsFirst && State == StackWalkState.SW_FRAMELESS;
            return new(Context.Clone(), State, FrameIter.CurrentFrameAddress, ThreadData, isResumable, isActiveFrame);
        }
    }

    IEnumerable<IStackDataFrameHandle> IStackWalk.CreateStackWalk(ThreadData threadData)
        => CreateStackWalkCore(threadData, skipInitialFrames: false);

    /// <summary>
    /// Core stack walk implementation.
    /// </summary>
    /// <param name="threadData">Thread to walk.</param>
    /// <param name="skipInitialFrames">
    /// When true, pre-advances the FrameIterator past explicit Frames below the initial
    /// managed frame's caller SP. This matches the native DacStackReferenceWalker behavior
    /// for GC reference enumeration, where these frames are within the current managed
    /// frame's stack range and don't contribute additional GC roots.
    ///
    /// Must be false for ClrDataStackWalk, which advances the cDAC and legacy DAC in
    /// lockstep and must yield the same frame sequence (including initial skipped frames).
    /// </param>
    private IEnumerable<IStackDataFrameHandle> CreateStackWalkCore(ThreadData threadData, bool skipInitialFrames)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        FillContextFromThread(context, threadData);
        StackWalkState state = IsManaged(context.InstructionPointer, out _) ? StackWalkState.SW_FRAMELESS : StackWalkState.SW_FRAME;
        FrameIterator frameIterator = new(_target, threadData);

        if (skipInitialFrames)
        {
            TargetPointer skipBelowSP;
            if (state == StackWalkState.SW_FRAMELESS)
            {
                IPlatformAgnosticContext callerCtx = context.Clone();
                callerCtx.Unwind(_target);
                skipBelowSP = callerCtx.StackPointer;
            }
            else
            {
                skipBelowSP = context.StackPointer;
            }
            while (frameIterator.IsValid() && frameIterator.CurrentFrameAddress.Value < skipBelowSP.Value)
            {
                frameIterator.Next();
            }
        }

        // if the next Frame is not valid and we are not in managed code, there is nothing to return
        if (state == StackWalkState.SW_FRAME && !frameIterator.IsValid())
        {
            yield break;
        }

        StackWalkData stackWalkData = new(context, state, frameIterator, threadData);

        // Mirror native Init() -> ProcessCurrentFrame() -> CheckForSkippedFrames():
        // When the initial frame is managed (SW_FRAMELESS), check if there are explicit
        // Frames below the caller SP that should be reported first. The native walker
        // yields skipped frames BEFORE the containing managed frame on non-x86.
        if (state == StackWalkState.SW_FRAMELESS && CheckForSkippedFrames(stackWalkData))
        {
            stackWalkData.State = StackWalkState.SW_SKIPPED_FRAME;
        }

        yield return stackWalkData.ToDataFrame();
        stackWalkData.AdvanceIsFirst();

        while (Next(stackWalkData))
        {
            yield return stackWalkData.ToDataFrame();
            stackWalkData.AdvanceIsFirst();
        }
    }

    IReadOnlyList<StackReferenceData> IStackWalk.WalkStackReferences(ThreadData threadData)
    {
        IEnumerable<IStackDataFrameHandle> stackFrames = CreateStackWalkCore(threadData, skipInitialFrames: true);
        IEnumerable<StackDataFrameHandle> frames = stackFrames.Select(AssertCorrectHandle);
        IEnumerable<GCFrameData> gcFrames = Filter(frames);

        GcScanContext scanContext = new(_target, resolveInteriorPointers: false);

        foreach (GCFrameData gcFrame in gcFrames)
        {
            try
            {
                _ = ((IStackWalk)this).GetMethodDescPtr(gcFrame.Frame);

                bool reportGcReferences = gcFrame.ShouldCrawlFrameReportGCReferences;

                TargetPointer pFrame = ((IStackWalk)this).GetFrameAddress(gcFrame.Frame);
                scanContext.UpdateScanContext(
                    gcFrame.Frame.Context.StackPointer,
                    gcFrame.Frame.Context.InstructionPointer,
                    pFrame);

                if (reportGcReferences)
                {
                    if (gcFrame.Frame.State == StackWalkState.SW_FRAMELESS)
                    {
                        if (!IsManaged(gcFrame.Frame.Context.InstructionPointer, out CodeBlockHandle? cbh))
                            throw new InvalidOperationException("Expected managed code");

                        CodeManagerFlags codeManagerFlags = gcFrame.Frame.IsActiveFrame
                            ? CodeManagerFlags.ActiveStackFrame
                            : 0;

                        if (gcFrame.ShouldParentToFuncletSkipReportingGCReferences)
                            codeManagerFlags |= CodeManagerFlags.ParentOfFuncletStackFrame;

                        // TODO: When ShouldParentFrameUseUnwindTargetPCforGCReporting is set,
                        // use FindFirstInterruptiblePoint on the catch handler clause range
                        // to override the relOffset for GC liveness lookup. This mirrors
                        // native gcenv.ee.common.cpp behavior for catch-handler resumption.

                        GcScanner gcScanner = new(_target);
                        gcScanner.EnumGcRefs(gcFrame.Frame.Context, cbh.Value, codeManagerFlags, scanContext);
                    }
                    else
                    {
                        // TODO: Frame-based GC root scanning (ScanFrameRoots) not yet implemented.
                        // Frames that call PromoteCallerStack (StubDispatchFrame, ExternalMethodFrame,
                        // DynamicHelperFrame, etc.) will be handled in a follow-up PR.
                    }
                }
            }
            catch (System.Exception ex)
            {
                // Per-frame exceptions are intentionally swallowed to provide partial results
                // rather than failing the entire stack walk. This matches the resilience model
                // of the legacy DAC. Callers can detect incomplete results by comparing counts.
                Debug.WriteLine($"Exception during WalkStackReferences at IP=0x{gcFrame.Frame.Context.InstructionPointer:X}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        return scanContext.StackRefs.Select(r => new StackReferenceData
        {
            HasRegisterInformation = r.HasRegisterInformation,
            Register = r.Register,
            Offset = r.Offset,
            Address = r.Address,
            Object = r.Object,
            Flags = (uint)r.Flags,
            IsStackSourceFrame = r.SourceType == StackRefData.SourceTypes.StackSourceFrame,
            Source = r.Source,
            StackPointer = r.StackPointer,
        }).ToList();
    }

    private record GCFrameData
    {
        public GCFrameData(StackDataFrameHandle frame)
        {
            Frame = frame;
        }

        public StackDataFrameHandle Frame { get; }
        public bool ShouldParentToFuncletSkipReportingGCReferences { get; set; }
        public bool ShouldCrawlFrameReportGCReferences { get; set; } // required
        public bool ShouldParentFrameUseUnwindTargetPCforGCReporting { get; set; }
    }

    private IEnumerable<GCFrameData> Filter(IEnumerable<StackDataFrameHandle> handles)
    {
        // StackFrameIterator::Filter assuming GC_FUNCLET_REFERENCE_REPORTING is defined

        // global tracking variables
        bool processNonFilterFunclet = false;
        bool processIntermediaryNonFilterFunclet = false;
        bool didFuncletReportGCReferences = true;
        TargetPointer parentStackFrame = TargetPointer.Null;
        TargetPointer funcletParentStackFrame = TargetPointer.Null;
        TargetPointer intermediaryFuncletParentStackFrame;

        foreach (StackDataFrameHandle handle in handles)
        {
            GCFrameData gcFrame = new(handle);

            // per-frame tracking variables
            bool stop = false;
            bool skippingFunclet = false;
            bool recheckCurrentFrame = false;
            bool skipFuncletCallback = true;

            TargetPointer pExInfo = GetCurrentExceptionTracker(handle);

            // by default, there is no funclet for the current frame
            // that reported GC references
            gcFrame.ShouldParentToFuncletSkipReportingGCReferences = false;

            // by default, assume that we are going to report GC references
            gcFrame.ShouldCrawlFrameReportGCReferences = true;

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
                                            }
                                            else if (!IsFunclet(handle))
                                            {
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
                                if (parentStackFrame != TargetPointer.Null)
                                {
                                    // Skip intermediate frames between funclet and parent.
                                    // The native runtime unconditionally skips these frames.
                                    break;
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

    TargetPointer IStackWalk.GetInstructionPointer(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        return handle.Context.InstructionPointer;
    }

    string IStackWalk.GetFrameName(TargetPointer frameIdentifier)
        => FrameIterator.GetFrameName(_target, frameIdentifier);

    TargetPointer IStackWalk.GetMethodDescPtr(TargetPointer framePtr)
        => FrameIterator.GetMethodDescPtr(_target, framePtr);

    TargetPointer IStackWalk.GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);

        // if we are at a capital F Frame, we can get the method desc from the frame
        TargetPointer framePtr = ((IStackWalk)this).GetFrameAddress(handle);
        if (framePtr != TargetPointer.Null)
        {
            // reportInteropMD if
            // 1) we are an InlinedCallFrame
            // 2) the StackDataFrame is at a SW_SKIPPED_FRAME state
            // 3) the return address is managed
            // 4) the return address method has a MDContext arg
            bool reportInteropMD = false;

            if (FrameIterator.IsInlinedCallFrame(_target, framePtr) &&
                handle.State == StackWalkState.SW_SKIPPED_FRAME)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

                // FrameIterator.GetReturnAddress is currently only implemented for InlinedCallFrame
                // This is fine as this check is only needed for that frame type
                TargetPointer returnAddress = FrameIterator.GetReturnAddress(_target, framePtr);
                if (_eman.GetCodeBlockHandle(returnAddress.Value) is CodeBlockHandle cbh)
                {
                    MethodDescHandle returnMethodDesc = rts.GetMethodDescHandle(_eman.GetMethodDesc(cbh));
                    reportInteropMD = rts.HasMDContextArg(returnMethodDesc);
                }
            }

            if (reportInteropMD)
            {
                // Special reportInteropMD case
                // This can't be handled in the GetMethodDescPtr(TargetPointer) because it relies on
                // the state of the stack walk (SW_SKIPPED_FRAME) which is not available there.
                // The MethodDesc pointer immediately follows the InlinedCallFrame
                TargetPointer methodDescPtr = framePtr + _target.GetTypeInfo(DataType.InlinedCallFrame).Size
                    ?? throw new InvalidOperationException("InlinedCallFrame type size is not defined.");
                return _target.ReadPointer(methodDescPtr);
            }
            else
            {
                // Standard case
                return ((IStackWalk)this).GetMethodDescPtr(framePtr);
            }
        }

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

    private void FillContextFromThread(IPlatformAgnosticContext context, ThreadData threadData)
    {
        byte[] bytes = new byte[context.Size];
        Span<byte> buffer = new Span<byte>(bytes);

        // Match the native DacStackReferenceWalker behavior: if the thread has a
        // FilterContext or ProfilerFilterContext set, use that instead of calling
        // GetThreadContext. During debugger breaks, GC stress redirection, or
        // profiler stack walks, these contexts hold the correct managed frame state.
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadData.ThreadAddress);

        TargetPointer filterContext = thread.DebuggerFilterContext;
        if (filterContext == TargetPointer.Null)
            filterContext = thread.ProfilerFilterContext;

        if (filterContext != TargetPointer.Null)
        {
            _target.ReadBuffer(filterContext.Value, buffer);
            context.FillFromBuffer(buffer);
            return;
        }

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
}
