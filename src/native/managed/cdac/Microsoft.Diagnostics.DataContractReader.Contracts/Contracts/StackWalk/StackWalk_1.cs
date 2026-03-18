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

    private class StackWalkData(IPlatformAgnosticContext context, StackWalkState state, FrameIterator frameIter, ThreadData threadData, bool skipDuplicateActiveICF = false)
    {
        public IPlatformAgnosticContext Context { get; set; } = context;
        public StackWalkState State { get; set; } = state;
        public FrameIterator FrameIter { get; set; } = frameIter;
        public ThreadData ThreadData { get; set; } = threadData;

        // When true, CheckForSkippedFrames will skip past an active InlinedCallFrame
        // that was just processed as SW_FRAME without advancing the FrameIterator.
        // This prevents a duplicate SW_SKIPPED_FRAME yield for the same managed IP.
        //
        // Must be false for ClrDataStackWalk (which needs exact DAC frame parity)
        // and true for WalkStackReferences (which matches native DacStackReferenceWalker
        // behavior of not re-enumerating the same InlinedCallFrame).
        public bool SkipDuplicateActiveICF { get; } = skipDuplicateActiveICF;


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
            // Skip Frames below the initial managed frame's caller SP. All Frames
            // below this SP belong to the current managed frame or frames pushed more
            // recently (e.g., RedirectedThreadFrame from GC stress, active
            // InlinedCallFrames from P/Invoke calls within the method).
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

        StackWalkData stackWalkData = new(context, state, frameIterator, threadData, skipDuplicateActiveICF: skipInitialFrames);

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
        // TODO(stackref): This isn't quite right. We need to check if the FilterContext or ProfilerFilterContext
        // is set and prefer that if either is not null.
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

                        // IsActiveFrame was computed during CreateStackWalk, matching native
                        // CrawlFrame::IsActiveFunc() semantics. Active frames report scratch
                        // registers; non-active frames skip them.
                        CodeManagerFlags codeManagerFlags = gcFrame.Frame.IsActiveFrame
                            ? CodeManagerFlags.ActiveStackFrame
                            : 0;

                        // TODO(stackref): Wire up funclet parent frame flags from Filter:
                        // - ShouldParentToFuncletSkipReportingGCReferences → ParentOfFuncletStackFrame
                        //   (tells GCInfoDecoder to skip reporting since funclet already reported)
                        // - ShouldParentFrameUseUnwindTargetPCforGCReporting → use exception's
                        //   unwind target IP instead of current IP for GC liveness lookup
                        // - ShouldParentToFuncletReportSavedFuncletSlots → report funclet's
                        //   callee-saved register slots from the parent frame
                        // These require careful validation to ensure Filter sets them correctly
                        // for all stack configurations before wiring them into EnumGcRefs.

                        GcScanner gcScanner = new(_target);
                        gcScanner.EnumGcRefs(gcFrame.Frame.Context, cbh.Value, codeManagerFlags, scanContext);
                    }
                    else
                    {
                        // Non-frameless: capital "F" Frame GcScanRoots dispatch.
                        // The base Frame::GcScanRoots_Impl is a no-op for most frame types.
                        // Frame types that override it (StubDispatchFrame, ExternalMethodFrame,
                        // CallCountingHelperFrame, DynamicHelperFrame, CLRToCOMMethodFrame,
                        // HijackFrame, ProtectValueClassFrame) call PromoteCallerStack to
                        // report method arguments from the transition block.
                        //
                        // GCFrame is NOT part of the Frame chain — it has its own linked list
                        // that the GC scans separately. The DAC's DacStackReferenceWalker
                        // does not scan GCFrame roots.
                        //
                        // For now, this is a no-op matching the base Frame behavior.
                        // TODO(stackref): Implement PromoteCallerStack for stub frames that
                        // report caller arguments (StubDispatchFrame, ExternalMethodFrame, etc.)
                        try
                        {
                            ScanFrameRoots(gcFrame.Frame, scanContext);
                        }
                        catch (System.Exception)
                        {
                            // Don't let one bad frame abort the entire stack walk
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.WriteLine($"Exception during WalkStackReferences: {ex}");
                // Matching native DAC behavior: capture errors, don't propagate
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
                    // TODO: The native StackFrameIterator::Filter checks pExInfo->m_lastReportedFunclet.IP
                    // to handle the case where a finally funclet was reported in a previous GC run.
                    // This requires runtime support to persist LastReportedFuncletInfo on ExInfo,
                    // which is not yet implemented. Until then this block is unreachable.
                    if (exInfo.PassNumber == 2 &&
                        exInfo.CSFEnclosingClause != TargetPointer.Null &&
                        funcletParentStackFrame == TargetPointer.Null &&
                        false) // TODO: check lastReportedFunclet.IP != 0 when runtime support is added
                    {
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
                                    // TODO(stackref): Implement marker frame detection. The native code checks
                                    // if the caller IP is within DispatchManagedException / RhThrowEx to
                                    // transition back to Off. Without this, force-reporting stays active
                                    // indefinitely during funclet skipping.
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

        // If the current Frame was already processed as SW_FRAME (active InlinedCallFrame
        // that wasn't advanced), skip past it to avoid a duplicate SW_SKIPPED_FRAME yield.
        // Only applies to WalkStackReferences (SkipDuplicateActiveICF=true).
        if (handle.SkipDuplicateActiveICF && handle.FrameIter.IsInlineCallFrameWithActiveCall())
        {
            handle.FrameIter.Next();
            if (!handle.FrameIter.IsValid())
            {
                return false;
            }
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

    /// <summary>
    /// Scans GC roots for a non-frameless (capital "F" Frame) stack frame.
    /// Dispatches based on frame type identifier. Most frame types have a no-op
    /// GcScanRoots (the base Frame implementation does nothing).
    ///
    /// Frame types with meaningful GcScanRoots that call PromoteCallerStack:
    /// StubDispatchFrame, ExternalMethodFrame, CallCountingHelperFrame,
    /// DynamicHelperFrame, CLRToCOMMethodFrame, HijackFrame, ProtectValueClassFrame.
    /// </summary>
    private void ScanFrameRoots(StackDataFrameHandle frame, GcScanContext scanContext)
    {
        TargetPointer frameAddress = frame.FrameAddress;
        if (frameAddress == TargetPointer.Null)
            return;

        // Read the frame's VTable pointer (Identifier) to determine its type.
        // GetFrameName expects a VTable identifier, not a frame address.
        Data.Frame frameData = _target.ProcessedData.GetOrAdd<Data.Frame>(frameAddress);
        string frameName = ((IStackWalk)this).GetFrameName(frameData.Identifier);

        switch (frameName)
        {
            case "StubDispatchFrame":
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.StubDispatchFrame sdf = _target.ProcessedData.GetOrAdd<Data.StubDispatchFrame>(frameAddress);
                if (sdf.GCRefMap != TargetPointer.Null)
                {
                    PromoteCallerStackUsingGCRefMap(fmf.TransitionBlockPtr, sdf.GCRefMap, scanContext);
                }
                else
                {
                    PromoteCallerStackUsingMetaSig(frameAddress, fmf.TransitionBlockPtr, scanContext);
                }
                break;
            }

            case "ExternalMethodFrame":
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.ExternalMethodFrame emf = _target.ProcessedData.GetOrAdd<Data.ExternalMethodFrame>(frameAddress);
                if (emf.GCRefMap != TargetPointer.Null)
                {
                    PromoteCallerStackUsingGCRefMap(fmf.TransitionBlockPtr, emf.GCRefMap, scanContext);
                }
                break;
            }

            case "DynamicHelperFrame":
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                Data.DynamicHelperFrame dhf = _target.ProcessedData.GetOrAdd<Data.DynamicHelperFrame>(frameAddress);
                ScanDynamicHelperFrame(fmf.TransitionBlockPtr, dhf.DynamicHelperFrameFlags, scanContext);
                break;
            }

            case "CallCountingHelperFrame":
            case "PrestubMethodFrame":
            {
                Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
                PromoteCallerStackUsingMetaSig(frameAddress, fmf.TransitionBlockPtr, scanContext);
                break;
            }

            case "CLRToCOMMethodFrame":
            case "ComPrestubMethodFrame":
                // These frames call PromoteCallerStack to report method arguments.
                // TODO(stackref): Implement PromoteCallerStack for COM interop frames
                break;

            case "HijackFrame":
                // Reports return value registers (X86 only with FEATURE_HIJACK)
                // TODO(stackref): Implement HijackFrame scanning
                break;

            case "ProtectValueClassFrame":
                // Scans value types in linked list
                // TODO(stackref): Implement ProtectValueClassFrame scanning
                break;

            default:
                // Base Frame::GcScanRoots_Impl is a no-op — nothing to report.
                break;
        }
    }

    /// <summary>
    /// Decodes a GCRefMap bitstream and reports GC references in the transition block.
    /// Port of native TransitionFrame::PromoteCallerStackUsingGCRefMap (frames.cpp).
    /// </summary>
    private void PromoteCallerStackUsingGCRefMap(
        TargetPointer transitionBlock,
        TargetPointer gcRefMapBlob,
        GcScanContext scanContext)
    {
        GCRefMapDecoder decoder = new(_target, gcRefMapBlob);

        // x86: skip stack pop count
        if (_target.PointerSize == 4)
            decoder.ReadStackPop();

        while (!decoder.AtEnd)
        {
            int pos = decoder.CurrentPos;
            GCRefMapToken token = decoder.ReadToken();
            uint offset = OffsetFromGCRefMapPos(pos);
            TargetPointer slotAddress = new(transitionBlock.Value + offset);

            switch (token)
            {
                case GCRefMapToken.Skip:
                    break;

                case GCRefMapToken.Ref:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
                    break;

                case GCRefMapToken.Interior:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    break;

                case GCRefMapToken.MethodParam:
                case GCRefMapToken.TypeParam:
                    // The DAC skips these (guarded by #ifndef DACCESS_COMPILE in native).
                    // They represent loader allocator references, not managed GC refs.
                    break;

                case GCRefMapToken.VASigCookie:
                    // VASigCookie requires MetaSig parsing — not yet implemented.
                    // TODO(stackref): Implement VASIG_COOKIE handling
                    break;
            }
        }
    }

    /// <summary>
    /// Converts a GCRefMap position to a byte offset within the transition block.
    /// Port of native OffsetFromGCRefMapPos (frames.cpp:1624-1633).
    /// </summary>
    private uint OffsetFromGCRefMapPos(int pos)
    {
        uint firstSlotOffset = _target.ReadGlobal<uint>(Constants.Globals.TransitionBlockOffsetOfFirstGCRefMapSlot);

        return firstSlotOffset + (uint)(pos * _target.PointerSize);
    }

    /// <summary>
    /// Scans GC roots for a DynamicHelperFrame based on its flags.
    /// Port of native DynamicHelperFrame::GcScanRoots_Impl (frames.cpp:1071-1105).
    /// </summary>
    private void ScanDynamicHelperFrame(
        TargetPointer transitionBlock,
        int dynamicHelperFrameFlags,
        GcScanContext scanContext)
    {
        const int DynamicHelperFrameFlags_ObjectArg = 1;
        const int DynamicHelperFrameFlags_ObjectArg2 = 2;

        uint argRegOffset = _target.ReadGlobal<uint>(Constants.Globals.TransitionBlockOffsetOfArgumentRegisters);

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg) != 0)
        {
            TargetPointer argAddr = new(transitionBlock.Value + argRegOffset);
            // On x86, this would need offsetof(ArgumentRegisters, ECX) adjustment.
            // For AMD64/ARM64, the first argument register is at the base offset.
            scanContext.GCReportCallback(argAddr, GcScanFlags.None);
        }

        if ((dynamicHelperFrameFlags & DynamicHelperFrameFlags_ObjectArg2) != 0)
        {
            TargetPointer argAddr = new(transitionBlock.Value + argRegOffset + (uint)_target.PointerSize);
            // On x86, this would need offsetof(ArgumentRegisters, EDX) adjustment.
            // For AMD64/ARM64, the second argument is pointer-size after the first.
            scanContext.GCReportCallback(argAddr, GcScanFlags.None);
        }
    }

    /// <summary>
    /// Promotes caller stack GC references by parsing the method signature via MetaSig.
    /// Used when a frame has no precomputed GCRefMap (e.g., dynamic/LCG methods).
    /// Port of native TransitionFrame::PromoteCallerStack + PromoteCallerStackHelper (frames.cpp).
    /// </summary>
    private void PromoteCallerStackUsingMetaSig(
        TargetPointer frameAddress,
        TargetPointer transitionBlock,
        GcScanContext scanContext)
    {
        Data.FramedMethodFrame fmf = _target.ProcessedData.GetOrAdd<Data.FramedMethodFrame>(frameAddress);
        TargetPointer methodDescPtr = fmf.MethodDescPtr;
        if (methodDescPtr == TargetPointer.Null)
            return;

        ReadOnlySpan<byte> signature;
        try
        {
            signature = GetMethodSignatureBytes(methodDescPtr);
        }
        catch (System.Exception)
        {
            return;
        }

        if (signature.IsEmpty)
            return;

        CorSigParser parser = new(signature, _target.PointerSize);

        // Parse calling convention
        byte callingConvByte = parser.ReadByte();
        bool hasThis = (callingConvByte & 0x20) != 0; // IMAGE_CEE_CS_CALLCONV_HASTHIS
        bool isGeneric = (callingConvByte & 0x10) != 0;

        if (isGeneric)
            parser.ReadCompressedUInt(); // skip generic param count

        uint paramCount = parser.ReadCompressedUInt();

        // Skip return type
        parser.SkipType();

        // Walk through GCRefMap positions.
        // The position numbering matches how GCRefMap encodes slots:
        //   ARM64: pos 0 = RetBuf (x8), pos 1+ = argument registers (x0-x7), then stack
        //   Others: pos 0 = first argument register/slot, etc.
        int pos = 0;

        // On ARM64, position 0 is the return buffer register (x8).
        // Methods without a return buffer skip this slot.
        // TODO: detect HasRetBuf from the signature's return type when needed.
        // For now, we skip the retbuf slot on ARM64 since the common case
        // (dynamic invoke stubs) doesn't use return buffers.
        bool isArm64 = IsTargetArm64();
        if (isArm64)
            pos++;

        // Promote 'this' if present
        if (hasThis)
        {
            uint offset = OffsetFromGCRefMapPos(pos);
            TargetPointer slotAddress = new(transitionBlock.Value + offset);
            // 'this' is a GC reference for reference types, interior for value types.
            // The runtime checks methodDesc.GetMethodTable().IsValueType() && !IsUnboxingStub().
            // For safety, treat as a regular GC reference (correct for reference type methods,
            // and conservative for value type methods which would need interior promotion).
            scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
            pos++;
        }

        // Walk each parameter
        for (uint i = 0; i < paramCount; i++)
        {
            uint offset = OffsetFromGCRefMapPos(pos);
            TargetPointer slotAddress = new(transitionBlock.Value + offset);

            GcTypeKind kind = parser.ReadTypeAndClassify();

            switch (kind)
            {
                case GcTypeKind.Ref:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.None);
                    break;

                case GcTypeKind.Interior:
                    scanContext.GCReportCallback(slotAddress, GcScanFlags.GC_CALL_INTERIOR);
                    break;

                case GcTypeKind.Other:
                    // Value types may contain embedded GC references.
                    // Full scanning requires reading the MethodTable's GCDesc.
                    // TODO(stackref): Implement value type GCDesc scanning for MetaSig path.
                    break;

                case GcTypeKind.None:
                    break;
            }

            pos++;
        }
    }

    /// <summary>
    /// Gets the raw signature bytes for a MethodDesc.
    /// For StoredSigMethodDesc (dynamic, array, EEImpl methods), reads the embedded signature.
    /// For normal IL methods, reads from module metadata.
    /// </summary>
    private ReadOnlySpan<byte> GetMethodSignatureBytes(TargetPointer methodDescPtr)
    {
        IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
        MethodDescHandle mdh = rts.GetMethodDescHandle(methodDescPtr);

        // Try StoredSigMethodDesc first (dynamic/LCG/array methods)
        if (rts.IsStoredSigMethodDesc(mdh, out ReadOnlySpan<byte> storedSig))
            return storedSig;

        // Normal IL methods: get signature from metadata
        uint methodToken = rts.GetMethodToken(mdh);
        if (methodToken == 0x06000000) // mdtMethodDef with RID 0 = no token
            return default;

        TargetPointer methodTablePtr = rts.GetMethodTable(mdh);
        TypeHandle typeHandle = rts.GetTypeHandle(methodTablePtr);
        TargetPointer modulePtr = rts.GetModule(typeHandle);

        ILoader loader = _target.Contracts.Loader;
        ModuleHandle moduleHandle = loader.GetModuleHandleFromModulePtr(modulePtr);

        IEcmaMetadata ecmaMetadata = _target.Contracts.EcmaMetadata;
        MetadataReader? mdReader = ecmaMetadata.GetMetadata(moduleHandle);
        if (mdReader is null)
            return default;

        MethodDefinitionHandle methodDefHandle = MetadataTokens.MethodDefinitionHandle((int)(methodToken & 0x00FFFFFF));
        MethodDefinition methodDef = mdReader.GetMethodDefinition(methodDefHandle);
        BlobReader blobReader = mdReader.GetBlobReader(methodDef.Signature);
        return blobReader.ReadBytes(blobReader.Length);
    }

    /// <summary>
    /// Detects if the target architecture is ARM64 based on TransitionBlock layout.
    /// On ARM64, GetOffsetOfFirstGCRefMapSlot != GetOffsetOfArgumentRegisters
    /// (because the first GCRefMap slot is the x8 RetBuf register, not x0).
    /// </summary>
    private bool IsTargetArm64()
    {
        uint firstGCRefMapSlot = _target.ReadGlobal<uint>(Constants.Globals.TransitionBlockOffsetOfFirstGCRefMapSlot);
        uint argRegsOffset = _target.ReadGlobal<uint>(Constants.Globals.TransitionBlockOffsetOfArgumentRegisters);
        return firstGCRefMapSlot != argRegsOffset;
    }
}
