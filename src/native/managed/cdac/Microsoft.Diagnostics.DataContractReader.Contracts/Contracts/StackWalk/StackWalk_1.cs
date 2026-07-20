// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;
using Microsoft.Diagnostics.DataContractReader.Data;
using System.Linq;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class StackWalk_1 : IStackWalk
{
    private readonly Target _target;
    private readonly IExecutionManager _eman;
    private readonly GcScanner _gcScanner;
    private readonly FrameHelpers _frameHelpers;

    internal StackWalk_1(Target target)
    {
        _target = target;
        _eman = target.Contracts.ExecutionManager;
        _gcScanner = new GcScanner(target);
        _frameHelpers = new FrameHelpers(target);
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

        // Track isInterrupted like native CrawlFrame::isInterrupted.
        // Set in UpdateState when transitioning to Frameless after processing a Frame
        // with FRAME_ATTR_EXCEPTION (e.g., FaultingExceptionFrame). When true, the managed
        // frame reached via that Frame's return address was interrupted by an exception,
        // and EnumGcRefs should use ExecutionAborted to skip live slot reporting at
        // non-interruptible offsets.
        public bool IsInterrupted { get; set; }

        // The frame type of the last Frame processed by Next().
        // Used by UpdateState to detect exception frames (FRAME_ATTR_EXCEPTION) and
        // set IsInterrupted when transitioning to a managed frame.
        public FrameType? LastProcessedFrameType { get; set; }

        public bool IsCurrentFrameResumable()
        {
            if (State is not (StackWalkState.Frame or StackWalkState.SkippedFrame))
                return false;

            var ft = FrameIter.GetCurrentFrameType();
            // Only frame types with FRAME_ATTR_RESUMABLE set isFirst=true.
            // FaultingExceptionFrame has FRAME_ATTR_FAULTED (sets hasFaulted)
            // but NOT FRAME_ATTR_RESUMABLE, so it must not be included here.
            // Note: HijackFrame only has FRAME_ATTR_RESUMABLE on non-x86 platforms
            // (see frames.h). On x86 it uses GcScanRoots_Impl instead of the
            // resumable frame pattern. When x86 cDAC stack walking is supported,
            // HijackFrame should be conditioned on the target architecture.
            return ft is FrameType.ResumableFrame
                      or FrameType.RedirectedThreadFrame
                      or FrameType.HijackFrame;
        }

        /// <summary>
        /// Update the IsFirst state for the NEXT frame, matching native stackwalk.cpp:
        /// - After a frameless frame: isFirst = false
        /// - After a ResumableFrame: isFirst = true
        /// - After other Frames: isFirst = false
        /// - After a skipped frame: isFirst unchanged (native never modifies isFirst
        ///   in the SFITER_SKIPPED_FRAME_FUNCTION path -- it keeps the value from Init)
        /// </summary>
        public void AdvanceIsFirst()
        {
            if (State == StackWalkState.Frameless)
            {
                IsFirst = false;
            }
            else if (State == StackWalkState.SkippedFrame)
            {
                // Native SFITER_SKIPPED_FRAME_FUNCTION in stackwalk.cpp does NOT
                // modify isFirst. It stays true from Init() so the subsequent managed frame
                // gets IsActiveFunc()=true. This is important because skipped frames are
                // explicit Frames embedded within the active managed frame (e.g. InlinedCallFrame
                // from PInvoke), and the managed frame should still be treated as the leaf.
            }
            else
            {
                IsFirst = IsCurrentFrameResumable();
            }
        }

        public StackDataFrameHandle ToDataFrame()
        {
            bool isResumable = IsCurrentFrameResumable();
            bool isActiveFrame = IsFirst && State == StackWalkState.Frameless;
            return new(Context.Clone(), State, FrameIter.CurrentFrameAddress, ThreadData, isResumable, isActiveFrame);
        }
    }

    private enum ContextFlags
    {
        Full = 0x1,
        All = 0x2,
    }

    IEnumerable<IStackDataFrameHandle> IStackWalk.CreateStackWalk(ThreadData threadData)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        uint contextFlags = context.AllContextFlags;
        FillContextFromThread(context, threadData, contextFlags);
        StackWalkState state = IsManaged(context.InstructionPointer, out _) ? StackWalkState.Frameless : StackWalkState.InitialNativeContext;
        FrameIterator frameIterator = new(_target, threadData);

        return RunStackWalk(context, state, frameIterator, threadData);
    }

    private void SetupContext(IPlatformAgnosticContext context, FrameIterator frameIterator, StackWalkState state, ref bool isFirst, out bool matchedIsInterrupted)
    {
        TargetPointer curSP = context.StackPointer;
        TargetCodePointer curPc = context.InstructionPointer;
        TargetPointer curFP = context.FramePointer;
        if (state == StackWalkState.Frameless)
        {
            IPlatformAgnosticContext tmpContext = context.Clone();
            tmpContext.Unwind(_target);
            curSP = tmpContext.StackPointer;
        }
        bool isX86 = _target.Contracts.RuntimeInfo.GetTargetArchitecture() == RuntimeInfoArchitecture.X86;
        bool matched = false;
        FrameType matchedType = FrameType.Unknown;
        while (frameIterator.IsValid())
        {
            if (frameIterator.CurrentFrameAddress.Value >= curSP.Value)
            {
                if (!isX86)
                {
                    break;
                }

                // See https://github.com/dotnet/runtime/blob/ad50b412069ee7f274c585d191df797ac5548525/src/coreclr/vm/stackwalk.cpp#L1238
                if (frameIterator.GetCurrentReturnAddress() != curPc)
                    break;

                IPlatformAgnosticContext tmpContext = context.Clone();
                frameIterator.UpdateContextFromCurrentFrame(tmpContext);
                if (tmpContext.FramePointer != curFP)
                    break;
            }

            if (frameIterator.GetCurrentReturnAddress() == curPc)
            {
                matched = true;
                matchedType = frameIterator.GetCurrentFrameType();
                frameIterator.UpdateContextFromCurrentFrame(context);
            }

            frameIterator.Next();
        }

        matchedIsInterrupted = false;
        if (matched)
        {
            isFirst = matchedType is FrameType.ResumableFrame
                                         or FrameType.RedirectedThreadFrame
                          || (matchedType is FrameType.HijackFrame && !isX86);
            matchedIsInterrupted = matchedType is FrameType.FaultingExceptionFrame
                                                or FrameType.SoftwareExceptionFrame;
        }
    }

    IEnumerable<IStackDataFrameHandle> IStackWalk.CreateStackWalk(ThreadData threadData, byte[] contextBuffer, bool isFirst)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        context.FillFromBuffer(contextBuffer);
        FrameIterator frameIterator = new(_target, threadData);
        StackWalkState state = IsManaged(context.InstructionPointer, out _) ? StackWalkState.Frameless : StackWalkState.InitialNativeContext;
        SetupContext(context, frameIterator, state, ref isFirst, out bool matchedIsInterrupted);
        return RunStackWalk(context, state, frameIterator, threadData, isFirst, matchedIsInterrupted);
    }

    private IEnumerable<IStackDataFrameHandle> RunStackWalk(
        IPlatformAgnosticContext context,
        StackWalkState state,
        FrameIterator frameIterator,
        ThreadData threadData,
        bool isFirst = true,
        bool isInterrupted = false)
    {
        // Skip the head InterpreterFrame when entering with a context already
        // inside an interpreter execution (e.g. a managed-debugger breakpoint
        // synthesized callback context). Without this, Frame would later
        // re-process it and re-walk the same InterpMethodContextFrame chain.
        // Mirrors the native walker fix in dotnet/runtime#126953.
        if (state == StackWalkState.Frameless
            && IsInterpreterCode(context.InstructionPointer)
            && frameIterator.IsValid()
            && frameIterator.GetCurrentFrameType() == FrameType.InterpreterFrame)
        {
            frameIterator.Next();
        }

        StackWalkData stackWalkData = new(context, state, frameIterator, threadData)
        {
            IsFirst = isFirst,
            IsInterrupted = isInterrupted
        };

        // Mirror native Init() -> ProcessCurrentFrame() -> CheckForSkippedFrames():
        // When the initial frame is managed (Frameless), check if there are explicit
        // Frames below the caller SP that should be reported first. The native walker
        // yields skipped frames BEFORE the containing managed frame.
        if (stackWalkData.State == StackWalkState.Frameless && CheckForSkippedFrames(stackWalkData))
        {
            stackWalkData.State = StackWalkState.SkippedFrame;
        }

        yield return stackWalkData.ToDataFrame();
        stackWalkData.AdvanceIsFirst();

        while (Next(stackWalkData))
        {
            yield return stackWalkData.ToDataFrame();
            stackWalkData.AdvanceIsFirst();
        }
    }

    IReadOnlyList<StackReferenceData> IStackWalk.WalkStackReferences(ThreadData threadData, bool resolveInteriorPointers)
    {
        // Initialize the walk data directly
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        FillContextFromThread(context, threadData, context.FullContextFlags);
        StackWalkState state = IsManaged(context.InstructionPointer, out _) ? StackWalkState.Frameless : StackWalkState.InitialNativeContext;
        FrameIterator frameIterator = new(_target, threadData);

        // See CreateStackWalk: skip the head InterpreterFrame when entering
        // already inside an interpreter execution to avoid double-walking.
        if (state == StackWalkState.Frameless
            && IsInterpreterCode(context.InstructionPointer)
            && frameIterator.IsValid()
            && frameIterator.GetCurrentFrameType() == FrameType.InterpreterFrame)
        {
            frameIterator.Next();
        }

        StackWalkData walkData = new(context, state, frameIterator, threadData);

        // Mirror native Init() -> ProcessCurrentFrame() -> CheckForSkippedFrames():
        // When the initial frame is managed (Frameless), check if there are explicit
        // Frames below the caller SP that should be reported first. The native walker
        // yields skipped frames BEFORE the containing managed frame.
        if (walkData.State == StackWalkState.Frameless && CheckForSkippedFrames(walkData))
            walkData.State = StackWalkState.SkippedFrame;

        GcScanContext scanContext = new(_target, resolveInteriorPointers);

        // Filter drives Next() directly, matching native Filter()+NextRaw() integration.
        // This prevents funclet-to-parent transitions from re-visiting already-walked frames.
        foreach (GCFrameData gcFrame in Filter(walkData))
        {
            try
            {
                bool reportGcReferences = gcFrame.ShouldCrawlFrameReportGCReferences;

                TargetPointer pFrame = ((IStackWalk)this).GetFrameAddress(gcFrame.Frame);
                scanContext.UpdateScanContext(
                    gcFrame.Frame.Context.StackPointer,
                    gcFrame.Frame.Context.InstructionPointer,
                    pFrame);

                if (reportGcReferences)
                {
                    if (gcFrame.Frame.State == StackWalkState.Frameless)
                    {
                        if (!IsManaged(gcFrame.Frame.Context.InstructionPointer, out CodeBlockHandle? cbh))
                            throw new InvalidOperationException("Expected managed code");

                        GcSlotEnumerationOptions gcOptions = new()
                        {
                            IsActiveFrame = gcFrame.Frame.IsActiveFrame,

                            // If the frame was interrupted by an exception (reached via a
                            // FaultingExceptionFrame), set ExecutionAborted so the GcInfoDecoder
                            // skips live slot reporting at non-interruptible offsets. This matches
                            // native CrawlFrame::GetCodeManagerFlags (stackwalk.h).
                            IsExecutionAborted = gcFrame.IsInterrupted,
                            IsParentOfFuncletStackFrame = gcFrame.ShouldParentToFuncletSkipReportingGCReferences,
                            SuppressUntrackedSlots = _eman.IsFilterFunclet(cbh.Value),
                        };

                        uint? relOffsetOverride = null;
                        if (gcFrame.ShouldParentFrameUseUnwindTargetPCforGCReporting)
                        {
                            _eman.GetGCInfo(cbh.Value, out TargetPointer gcInfoAddr, out uint gcVersion);
                            IGCInfoHandle gcHandle = _target.Contracts.GCInfo.DecodePlatformSpecificGCInfo(gcInfoAddr, gcVersion);
                            uint startPC = gcFrame.ClauseForCatchHandlerStartPC;
                            uint endPC = gcFrame.ClauseForCatchHandlerEndPC;
                            foreach (var range in _target.Contracts.GCInfo.GetInterruptibleRanges(gcHandle))
                            {
                                if (range.EndOffset <= startPC)
                                    continue;
                                if (startPC >= range.StartOffset && startPC < range.EndOffset)
                                {
                                    relOffsetOverride = startPC;
                                    break;
                                }
                                if (range.StartOffset < endPC)
                                {
                                    relOffsetOverride = range.StartOffset;
                                    break;
                                }
                            }
                        }

                        _gcScanner.EnumGcRefsForManagedFrame(gcFrame.Frame.Context, cbh.Value, gcOptions, scanContext, relOffsetOverride);
                    }
                    else
                    {
                        _gcScanner.GcScanRoots(gcFrame.Frame.FrameAddress, scanContext);
                    }
                }
            }
            catch (NotImplementedException ex)
            {
                // The calling convention or frame type is not yet supported (e.g., VarArgs,
                // SystemV struct-in-registers). Skip this frame -- the DSO will have partial
                // results but won't fail the entire stack walk.
                Debug.WriteLine($"Skipping frame at IP=0x{gcFrame.Frame.Context.InstructionPointer:X}: {ex.Message}");
            }
            catch (System.Exception ex)
            {
                // Unexpected per-frame exceptions are swallowed to provide partial results
                // rather than failing the entire stack walk. This matches the resilience model
                // of the legacy DAC. Callers can detect incomplete results by comparing counts.
                Debug.WriteLine($"Exception during WalkStackReferences at IP=0x{gcFrame.Frame.Context.InstructionPointer:X}: {ex.GetType().Name}: {ex.Message}");
            }
        }

        // Report the thread's GCFrame (GCPROTECT) chain: each GCFrame keeps a set of object
        // references alive across a runtime operation, so report them as roots.
        ReportGCFrameRoots(threadData, scanContext);

        // Report the thread's exception-tracking (ExInfo) chain: the current in-flight exception
        // and any superseded/nested ones are kept alive by the runtime, so report them as roots.
        ReportExceptionTrackerRoots(threadData, scanContext);

        return scanContext.StackRefs.Select(r => new StackReferenceData
        {
            HasRegisterInformation = r.HasRegisterInformation,
            IsInteriorPointer = r.IsInteriorPointer,
            Register = r.Register,
            Offset = r.Offset,
            Address = r.Address,
            Object = r.Object,
            Flags = (uint)r.Flags,
            SourceType = r.SourceType switch
            {
                StackRefData.SourceTypes.StackSourceIP => StackSourceType.InstructionPointer,
                StackRefData.SourceTypes.StackSourceFrame => StackSourceType.Frame,
                _ => StackSourceType.Other,
            },
            Source = r.Source,
            StackPointer = r.StackPointer,
        }).ToList();
    }

    // Reports each in-flight exception object held on the thread's exception-tracking (ExInfo)
    // chain: the current exception and any superseded/nested ones. The GC reports the same set in
    // gcenv.ee.cpp ScanStackRoots.
    private void ReportExceptionTrackerRoots(ThreadData threadData, GcScanContext scanContext)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadData.ThreadAddress);
        TargetPointer pExInfo = _target.ReadPointer(thread.ExceptionTracker);
        if (pExInfo == TargetPointer.Null)
            return;

        IException exceptionContract = _target.Contracts.Exception;
        HashSet<TargetPointer> seen = new();
        while (pExInfo != TargetPointer.Null)
        {
            if (!seen.Add(pExInfo))
                throw new InvalidOperationException($"Found a cycle when processing ExInfo.");

            // GetNestedExceptionInfo yields the address of the thrown-object slot (ExInfo::m_exception)
            // and the previous (nested) ExInfo; GCReportCallback reads the object through that slot.
            // ExInfo lives on the stack but is not a Frame, so it is treated specially here.
            exceptionContract.GetNestedExceptionInfo(pExInfo, out TargetPointer previous, out TargetPointer thrownObjectSlot);
            scanContext.UpdateScanContext(pExInfo, TargetCodePointer.Null, pExInfo, StackRefData.SourceTypes.StackSourceOther);
            scanContext.GCReportCallback(thrownObjectSlot, GcScanFlags.None);
            pExInfo = previous;
        }
    }

    // Reports each object reference protected by the thread's GCFrame (GCPROTECT) chain.
    // GCFrame::GcScanRoots reports m_pObjRefs[0..m_numObjRefs), using an interior promotion when
    // m_gcFlags != 0; the GC reports the same set in gcenv.ee.cpp ScanStackRoots.
    private void ReportGCFrameRoots(ThreadData threadData, GcScanContext scanContext)
    {
        ulong pointerSize = (ulong)_target.PointerSize;
        HashSet<TargetPointer> seen = [];
        TargetPointer pGCFrame = threadData.GCFrame;
        while (pGCFrame != TargetPointer.Null)
        {
            if (!seen.Add(pGCFrame))
                throw new InvalidOperationException($"Found a cycle when processing ThreadData.GCFrame list.");

            Data.GCFrame gcFrame = _target.ProcessedData.GetOrAdd<Data.GCFrame>(pGCFrame);

            // A GCFrame node lives on the stack but is a separate chain from the explicit Frame chain.
            scanContext.UpdateScanContext(pGCFrame, TargetCodePointer.Null, pGCFrame, StackRefData.SourceTypes.StackSourceOther);
            GcScanFlags flags = (GcScanFlags)gcFrame.GCFlags;
            for (uint i = 0; i < gcFrame.NumObjRefs; i++)
            {
                TargetPointer slot = new(gcFrame.ObjRefs.Value + (ulong)i * pointerSize);
                scanContext.GCReportCallback(slot, flags);
            }
            pGCFrame = gcFrame.Next;
        }
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
        public uint ClauseForCatchHandlerStartPC { get; set; }
        public uint ClauseForCatchHandlerEndPC { get; set; }
        // Set when the frame was reached via an exception Frame (FRAME_ATTR_EXCEPTION).
        // Causes ExecutionAborted to be passed to EnumGcRefs.
        public bool IsInterrupted { get; set; }
    }

    /// <summary>
    /// Port of native StackFrameIterator::Filter (GC_FUNCLET_REFERENCE_REPORTING mode).
    /// Unlike the previous implementation that passively consumed pre-generated frames,
    /// this version drives Next() directly — matching native Filter() which calls NextRaw()
    /// internally to skip frames. This prevents funclet-to-parent transitions from
    /// re-visiting already-walked frames.
    /// </summary>
#pragma warning disable IDE0059 // Unnecessary assignment — false positives from goto case + do/while pattern
    private IEnumerable<GCFrameData> Filter(StackWalkData walkData)
    {
        // Process the initial frame, then loop calling Next() for subsequent frames.
        // This matches native: Init() produces the first frame, then Filter()+NextRaw() loop.

        // global tracking variables
        bool processNonFilterFunclet = false;
        bool processIntermediaryNonFilterFunclet = false;
        bool didFuncletReportGCReferences = true;
        TargetPointer parentStackFrame = TargetPointer.Null;
        TargetPointer funcletParentStackFrame = TargetPointer.Null;
        TargetPointer intermediaryFuncletParentStackFrame = TargetPointer.Null;

        // Process the initial frame, then advance with Next()
        bool isValid = walkData.State is not (StackWalkState.Error or StackWalkState.Complete);
        while (isValid)
        {
            StackDataFrameHandle handle = walkData.ToDataFrame();
            walkData.AdvanceIsFirst();

            GCFrameData gcFrame = new(handle)
            {
                IsInterrupted = walkData.IsInterrupted,
            };

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
                case StackWalkState.Frameless:
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
                                        goto case StackWalkState.Frameless;
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

                                                gcFrame.ClauseForCatchHandlerStartPC = exInfo.ClauseForCatchHandlerStartPC;
                                                gcFrame.ClauseForCatchHandlerEndPC = exInfo.ClauseForCatchHandlerEndPC;
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
                                goto case StackWalkState.Frameless;
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

                case StackWalkState.Frame:
                case StackWalkState.SkippedFrame:
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
                case StackWalkState.InitialNativeContext:
                case StackWalkState.NativeMarker:
                    break;
                default:
                    stop = true;
                    break;
            }

            if (stop)
                yield return gcFrame;

            // Advance the iterator - matching native Filter() calling NextRaw()
            // When a frame was skipped (stop=false), this advances past it.
            // When a frame was yielded (stop=true), this advances to the next frame.
            isValid = Next(walkData);
        }
    }
#pragma warning restore IDE0059

    private bool IsUnwoundToTargetParentFrame(StackDataFrameHandle handle, TargetPointer targetParentFrame)
    {
        Debug.Assert(handle.State is StackWalkState.Frameless);

        IPlatformAgnosticContext callerContext = handle.Context.Clone();
        callerContext.Unwind(_target);

        return callerContext.StackPointer == targetParentFrame;
    }

    private bool Next(StackWalkData handle)
    {
        switch (handle.State)
        {
            case StackWalkState.Frameless:
                // Native assertion (stackwalk.cpp): current SP must be below the next Frame.
                // FaultingExceptionFrame is a special case where it gets pushed after the frame is running.
                Debug.Assert(
                    !handle.FrameIter.IsValid() ||
                    handle.Context.StackPointer.Value < handle.FrameIter.CurrentFrameAddress.Value ||
                    handle.FrameIter.GetCurrentFrameType() == FrameType.FaultingExceptionFrame,
                    $"SP (0x{handle.Context.StackPointer:X}) should be below next Frame (0x{handle.FrameIter.CurrentFrameAddress:X})");

                // Reset interrupted state after processing a managed frame.
                // Native stackwalk.cpp: isInterrupted = false; hasFaulted = false;
                handle.IsInterrupted = false;

                // Check if the current frame is interpreter code -- if so, use
                // interpreter virtual unwind instead of OS-level unwind.
                // This mirrors VirtualUnwindInterpreterCallFrame in eetwain.cpp.
                if (IsInterpreterCode(handle.Context.InstructionPointer))
                {
                    _frameHelpers.InterpreterVirtualUnwind(handle.Context);
                }
                else
                {
                    try
                    {
                        handle.Context.Unwind(_target);
                    }
                    catch
                    {
                        handle.State = StackWalkState.Error;
                        throw;
                    }
                }
                break;
            case StackWalkState.SkippedFrame:
                // Advance past the skipped frame, then let UpdateState detect
                // whether there are more skipped frames or we've reached the managed method.
                handle.FrameIter.Next();
                break;
            case StackWalkState.InitialNativeContext:
            case StackWalkState.NativeMarker:
            {
                TargetCodePointer ip = handle.Context.InstructionPointer;
                HijackKind hijackKind = _target.Contracts.Debugger.GetHijackKind(ip);
                if (hijackKind != HijackKind.None)
                {
                    IPlatformAgnosticContext recoveredContext = RetrieveHijackedContext(handle.Context, hijackKind == HijackKind.UnhandledException);
                    bool isFirst = true;
                    handle.State = IsManaged(recoveredContext.InstructionPointer, out _)
                        ? StackWalkState.Frameless
                        : StackWalkState.InitialNativeContext;
                    FrameIterator frameIterator = new(_target, handle.ThreadData);
                    SetupContext(recoveredContext, frameIterator, handle.State, ref isFirst, out bool matchedIsInterrupted);
                    handle.IsFirst = isFirst;
                    handle.IsInterrupted = matchedIsInterrupted;
                    handle.FrameIter = frameIterator;
                    handle.Context = recoveredContext;
                    handle.LastProcessedFrameType = null;
                }
                break;
            }
            case StackWalkState.Frame:
                // Native SFITER_FRAME_FUNCTION gates ProcessIp + UpdateRegDisplay on
                // GetReturnAddress() != 0, and gates GotoNextFrame on !pInlinedFrame.
                // pInlinedFrame is set only for active InlinedCallFrames.
                {
                    var frameType = handle.FrameIter.GetCurrentFrameType();
                    TargetCodePointer returnAddress = handle.FrameIter.GetCurrentReturnAddress();
                    bool isActiveICF = frameType == FrameType.InlinedCallFrame
                                       && returnAddress != TargetCodePointer.Null;

                    // Record the frame type so UpdateState can detect exception frames
                    // and set IsInterrupted when transitioning to the managed frame.
                    handle.LastProcessedFrameType = frameType;

                    // For InterpreterFrame the FrameIterator has no GetReturnAddress
                    // (interpreter virtual unwind manages the IP), but we still need
                    // UpdateContextFromFrame to transition to Frameless in the
                    // interpreted method.
                    if (returnAddress != TargetPointer.Null
                        || frameType == FrameType.InterpreterFrame)
                    {
                        handle.FrameIter.UpdateContextFromCurrentFrame(handle.Context);
                    }
                    if (!isActiveICF)
                    {
                        handle.FrameIter.Next();
                    }
                }
                break;
            case StackWalkState.Error:
            case StackWalkState.Complete:
                return false;
        }
        UpdateState(handle);

        return handle.State is not (StackWalkState.Error or StackWalkState.Complete);
    }

    private void UpdateState(StackWalkData handle)
    {
        // If we are complete or in a bad state, no updating is required.
        if (handle.State is StackWalkState.Error or StackWalkState.Complete)
        {
            return;
        }

        bool validFrame = handle.FrameIter.IsValid();

        switch (handle.State)
        {
            // The step that just ran moved Context (an unwind out of a
            // managed frame, or an advance past a Frame). Reclassify from the
            // observed Context+FrameIter.
            case StackWalkState.Frameless:
            case StackWalkState.Frame:
            case StackWalkState.SkippedFrame:
            {
                bool isManaged = IsManaged(handle.Context.InstructionPointer, out _);

                if (isManaged)
                {
                    handle.State = StackWalkState.Frameless;

                    // Detect exception frames (FRAME_ATTR_EXCEPTION) when transitioning to managed.
                    // Both FaultingExceptionFrame (hardware) and SoftwareExceptionFrame (managed throw)
                    // have FRAME_ATTR_EXCEPTION set. The resulting managed frame gets ExecutionAborted,
                    // causing GcInfoDecoder to skip live slot reporting at non-interruptible offsets.
                    if (handle.LastProcessedFrameType is FrameType.FaultingExceptionFrame
                                                      or FrameType.SoftwareExceptionFrame)
                    {
                        handle.IsInterrupted = true;
                    }
                    handle.LastProcessedFrameType = null;

                    if (CheckForSkippedFrames(handle))
                    {
                        handle.State = StackWalkState.SkippedFrame;
                    }
                }
                else
                {
                    if (handle.State == StackWalkState.Frame)
                    {
                        handle.State = validFrame ? StackWalkState.Frame : StackWalkState.Complete;
                        return;
                    }
                    handle.State = (validFrame || handle.State == StackWalkState.Frameless) ? StackWalkState.NativeMarker : StackWalkState.Complete;
                }
                return;
            }

            // The step that just ran bridged through a Frame. Yield Frame
            // so the consumer sees the bridged Frame; if there was no Frame, terminate.
            case StackWalkState.InitialNativeContext:
            case StackWalkState.NativeMarker:
                handle.State = validFrame ? StackWalkState.Frame : StackWalkState.Complete;
                return;
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

    byte[] IStackWalk.GetRawContext(IStackDataFrameHandle stackDataFrameHandle, StackwalkFlag flags)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        return handle.Context.GetBytes();
    }

    private IPlatformAgnosticContext RetrieveHijackedContext(IPlatformAgnosticContext ctx, bool isUnhandledException)
    {
        TargetPointer slotAddress = isUnhandledException
            ? ctx.StackPointer
            : ComputeRedirectStubSlot(ctx);

        TargetPointer contextAddress = _target.ReadPointer(slotAddress.Value);

        IPlatformAgnosticContext recovered = IPlatformAgnosticContext.GetContextForPlatform(_target);
        recovered.ReadFromAddress(_target, contextAddress);
        return recovered;
    }

    private TargetPointer ComputeRedirectStubSlot(IPlatformAgnosticContext context)
    {
        // Per-architecture offset of the saved PTR_CONTEXT in the redirect-stub stack frame.
        // Mirrors REDIRECTSTUB_{SP,EBP,RBP}_OFFSET_CONTEXT in
        // src/coreclr/vm/{i386,amd64,arm,arm64,riscv64,loongarch64}/asmconstants.h
        (bool useFramePointer, long offset) = _target.Contracts.RuntimeInfo.GetTargetArchitecture() switch
        {
            RuntimeInfoArchitecture.X86 => (true, -4L),
            RuntimeInfoArchitecture.X64 => (true, 0x20L),
            RuntimeInfoArchitecture.Arm => (false, 0L),
            RuntimeInfoArchitecture.Arm64 => (false, 0L),
            RuntimeInfoArchitecture.RiscV64 => (false, 0L),
            RuntimeInfoArchitecture.LoongArch64 => (false, 0L),
            var arch => throw new InvalidOperationException(
                $"Hijack-stub CONTEXT recovery is not supported on {arch}"),
        };

        ulong baseValue = (useFramePointer ? context.FramePointer : context.StackPointer).Value;
        // offset is signed (negative on x86). Two's-complement add via unchecked.
        return new TargetPointer(unchecked(baseValue + (ulong)offset));
    }

    TargetPointer IStackWalk.GetFrameAddress(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        if (handle.State is StackWalkState.Frame or StackWalkState.SkippedFrame)
        {
            return handle.FrameAddress;
        }
        return TargetPointer.Null;
    }

    TargetCodePointer IStackWalk.GetInstructionPointer(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);
        return handle.Context.InstructionPointer;
    }

    string IStackWalk.GetFrameName(TargetPointer frameIdentifier)
        => _frameHelpers.GetFrameName(frameIdentifier);

    TargetPointer IStackWalk.GetMethodDescPtr(TargetPointer framePtr)
        => _frameHelpers.GetMethodDescPtr(framePtr);

    TargetPointer IStackWalk.GetMethodDescPtr(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);

        // if we are at a capital F Frame, we can get the method desc from the frame
        TargetPointer framePtr = ((IStackWalk)this).GetFrameAddress(handle);
        if (framePtr != TargetPointer.Null)
        {
            // reportInteropMD if
            // 1) we are an InlinedCallFrame
            // 2) the StackDataFrame is at a SkippedFrame state
            // 3) the return address is managed
            // 4) the return address method has a MDContext arg
            bool reportInteropMD = false;

            Data.Frame frameData = _target.ProcessedData.GetOrAdd<Data.Frame>(framePtr);
            FrameType frameType = _frameHelpers.GetFrameType(frameData.Identifier);

            if (frameType == FrameType.InlinedCallFrame &&
                handle.State == StackWalkState.SkippedFrame)
            {
                IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;

                Data.InlinedCallFrame icf = _target.ProcessedData.GetOrAdd<Data.InlinedCallFrame>(framePtr);
                TargetCodePointer returnAddress = icf.CallerReturnAddress;
                if (returnAddress != TargetCodePointer.Null && _eman.GetCodeBlockHandle(returnAddress) is CodeBlockHandle cbh)
                {
                    MethodDescHandle returnMethodDesc = rts.GetMethodDescHandle(_eman.GetMethodDesc(cbh));
                    reportInteropMD = rts.HasMDContextArg(returnMethodDesc);
                }
            }

            if (reportInteropMD)
            {
                // Special reportInteropMD case
                // This can't be handled in the GetMethodDescPtr(TargetPointer) because it relies on
                // the state of the stack walk (SkippedFrame) which is not available there.
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

    IEnumerable<StackFrameData> IStackWalk.GetFrames(TargetPointer threadPointer)
    {
        ThreadData threadData = _target.Contracts.Thread.GetThreadData(threadPointer);
        FrameIterator iterator = new FrameIterator(_target, threadData);
        while (iterator.IsValid())
        {
            yield return new StackFrameData(
                    iterator.CurrentFrameAddress,
                    iterator.CurrentFrame.Identifier,
                    iterator.GetCurrentInternalFrameType());
            iterator.Next();
        }
    }

    bool IStackWalk.IsExceptionHandlingHelperInlinedCallFrame(TargetPointer frameAddress) => _frameHelpers.IsExceptionHandlingHelperInlinedCallFrame(frameAddress);

    DebuggerEvalData IStackWalk.GetDebuggerEvalData(TargetPointer funcEvalFrameAddress)
    {
        Data.FuncEvalFrame funcEvalFrame = _target.ProcessedData.GetOrAdd<Data.FuncEvalFrame>(funcEvalFrameAddress);
        Data.DebuggerEval debuggerEval = _target.ProcessedData.GetOrAdd<Data.DebuggerEval>(funcEvalFrame.DebuggerEvalPtr);
        return new DebuggerEvalData(debuggerEval.MethodToken, debuggerEval.AssemblyPtr);
    }

    byte[] IStackWalk.GetContext(ThreadData threadData, ThreadContextSource contextSource, uint contextFlags)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);
        byte[] bytes = new byte[context.Size];
        Span<byte> buffer = new Span<byte>(bytes);

        TargetPointer filterContext = TargetPointer.Null;

        if (contextSource.HasFlag(ThreadContextSource.Debugger))
            filterContext = threadData.DebuggerFilterContext;

        if (filterContext != TargetPointer.Null)
        {
            _target.ReadBuffer(filterContext.Value, buffer);
            return bytes;
        }

        bool success = _target.TryGetThreadContext(threadData.OSId.Value, contextFlags, buffer);
        if (!success)
        {
            return GetContextFromFrames(threadData);
        }
        return bytes;
    }

    private byte[] GetContextFromFrames(ThreadData threadData)
    {
        IPlatformAgnosticContext context = IPlatformAgnosticContext.GetContextForPlatform(_target);

        FrameIterator iterator = new FrameIterator(_target, threadData);
        while (iterator.IsValid())
        {
            // For InterpreterFrame, fill the context from the top InterpMethodContextFrame
            // (matches native InterpreterFrame::SetContextToInterpMethodContextFrame).
            if (iterator.GetCurrentFrameType() == FrameType.InterpreterFrame)
            {
                context.Clear();
                _frameHelpers.UpdateContextFromFrame(iterator.CurrentFrame, context);
                return context.GetBytes();
            }

            // For other frames, look for the first (deepest) frame that yields a context
            // with both SP and PC set (e.g. RedirectedThreadFrame, InlinedCallFrame,
            // DynamicHelperFrame).
            context.Clear();
            _frameHelpers.UpdateContextFromFrame(iterator.CurrentFrame, context);
            if (context.StackPointer.Value != 0 && context.InstructionPointer.Value != 0)
            {
                context.RawContextFlags = context.FullContextFlags;
                return context.GetBytes();
            }

            iterator.Next();
        }

        // The thread is not running managed code: return a zeroed context.
        context.Clear();
        return context.GetBytes();
    }

    TargetPointer IStackWalk.GetRedirectedContextPointer(ThreadData threadData)
    {
        FrameIterator iterator = new FrameIterator(_target, threadData);
        if (iterator.IsValid() && iterator.GetCurrentFrameType() == FrameType.RedirectedThreadFrame)
        {
            Data.ResumableFrame rf = _target.ProcessedData.GetOrAdd<Data.ResumableFrame>(iterator.CurrentFrameAddress);
            return rf.TargetContextPtr;
        }

        return TargetPointer.Null;
    }

    private bool IsManaged(TargetCodePointer ip, [NotNullWhen(true)] out CodeBlockHandle? codeBlockHandle)
    {
        if (_eman.GetCodeBlockHandle(ip) is CodeBlockHandle cbh && cbh.Address != TargetPointer.Null)
        {
            codeBlockHandle = cbh;
            return true;
        }
        codeBlockHandle = default;
        return false;
    }

    private void FillContextFromThread(IPlatformAgnosticContext context, ThreadData threadData, uint flags)
    {
        byte[] bytes = ((IStackWalk)this).GetContext(
            threadData,
            ThreadContextSource.Debugger,
            flags);
        context.FillFromBuffer(bytes);
    }

    private static StackDataFrameHandle AssertCorrectHandle(IStackDataFrameHandle stackDataFrameHandle)
    {
        if (stackDataFrameHandle is not StackDataFrameHandle handle)
        {
            throw new ArgumentException("Invalid stack data frame handle", nameof(stackDataFrameHandle));
        }

        return handle;
    }

    #region Interpreter

    // Interpreter-specific stack walk logic. Interpreted methods do not have OS-level
    // unwind info; the helpers below implement the cDAC equivalent of native
    // VirtualUnwindInterpreterCallFrame so the walker can step through interpreted
    // call chains and transition cleanly back to the native caller of InterpExecMethod
    // when the chain is exhausted.

    /// <summary>
    /// Checks if the given IP is in interpreter-managed code (CodeKind.Interpreter).
    /// </summary>
    private bool IsInterpreterCode(TargetCodePointer ip)
    {
        return _eman.GetCodeKind(ip) == CodeKind.Interpreter;
    }

    #endregion Interpreter
}
