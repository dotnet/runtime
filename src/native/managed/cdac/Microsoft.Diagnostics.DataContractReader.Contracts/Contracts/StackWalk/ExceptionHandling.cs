// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal partial class StackWalk_1 : IStackWalk
{
    /// <summary>
    /// Flags from the ExceptionFlags class (exstatecommon.h).
    /// These are bit flags stored in ExInfo.m_ExceptionFlags.m_flags.
    /// </summary>
    [Flags]
    private enum ExceptionFlagsEnum : uint
    {
        // See Ex_UnwindHasStarted in src/coreclr/vm/exstatecommon.h
        UnwindHasStarted = 0x00000004,
    }

    /// <summary>
    /// Given the CrawlFrame for a funclet frame, return the frame pointer of the enclosing funclet frame.
    /// For filter funclet frames and normal method frames, this function returns a NULL StackFrame.
    /// </summary>
    /// <returns>
    /// StackFrame.IsNull()   - no skipping is necessary
    /// StackFrame.IsMaxVal() - skip one frame and then ask again
    /// Anything else         - skip to the method frame indicated by the return value and ask again
    /// </returns>
    private TargetPointer FindParentStackFrameForStackWalk(StackDataFrameHandle handle, bool forGCReporting = false)
    {
        if (!forGCReporting && IsFilterFunclet(handle))
        {
            return TargetPointer.Null;
        }
        else
        {
            return FindParentStackFrameHelper(handle, forGCReporting);
        }
    }

    private TargetPointer FindParentStackFrameHelper(
        StackDataFrameHandle handle,
        bool forGCReporting = false)
    {
        IPlatformAgnosticContext callerContext = handle.Context.Clone();
        callerContext.Unwind(_target);
        TargetPointer callerStackFrame = callerContext.StackPointer;

        bool isFilterFunclet = IsFilterFunclet(handle);

        // Check for out-of-line finally funclets.  Filter funclets can't be out-of-line.
        if (!isFilterFunclet)
        {
            TargetPointer callerIp = callerContext.InstructionPointer;

            // In the runtime, on Windows, we check with that the IP is in the runtime
            // TODO(stackref): make sure this difference doesn't matter
            bool isCallerInVM = !IsManaged(callerIp, out CodeBlockHandle? _);

            if (!isCallerInVM)
            {
                if (!forGCReporting)
                {
                    return TargetPointer.PlatformMaxValue(_target);
                }
                else
                {
                    // ExInfo::GetCallerSPOfParentOfNonExceptionallyInvokedFunclet
                    IPlatformAgnosticContext callerCallerContext = callerContext.Clone();
                    callerCallerContext.Unwind(_target);
                    return callerCallerContext.StackPointer;
                }
            }
        }

        TargetPointer pExInfo = GetCurrentExceptionTracker(handle);
        while (pExInfo != TargetPointer.Null)
        {
            Data.ExceptionInfo exInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(pExInfo);
            pExInfo = exInfo.PreviousNestedInfo;

            // ExInfo::StackRange::IsEmpty
            if (exInfo.StackLowBound == TargetPointer.PlatformMaxValue(_target) &&
                exInfo.StackHighBound == TargetPointer.Null)
            {
                // This is ExInfo has just been created, skip it.
                continue;
            }

            if (callerStackFrame == exInfo.CSFEHClause)
            {
                return exInfo.CSFEnclosingClause;
            }
        }

        return TargetPointer.Null;
    }


    private bool IsFunclet(StackDataFrameHandle handle)
    {
        if (handle.State is StackWalkState.SW_FRAME or StackWalkState.SW_SKIPPED_FRAME)
        {
            return false;
        }

        if (!IsManaged(handle.Context.InstructionPointer, out CodeBlockHandle? cbh))
            return false;

        return _eman.IsFunclet(cbh.Value);
    }

    private bool IsFilterFunclet(StackDataFrameHandle handle)
    {
        if (handle.State is StackWalkState.SW_FRAME or StackWalkState.SW_SKIPPED_FRAME)
        {
            return false;
        }

        if (!IsManaged(handle.Context.InstructionPointer, out CodeBlockHandle? cbh))
            return false;

        return _eman.IsFilterFunclet(cbh.Value);
    }

    private TargetPointer GetCurrentExceptionTracker(StackDataFrameHandle handle)
    {
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(handle.ThreadData.ThreadAddress);
        // ExceptionTracker is the address of the field on the Thread object.
        // Dereference to get the actual ExInfo pointer.
        return _target.ReadPointer(thread.ExceptionTracker);
    }

    private bool HasFrameBeenUnwoundByAnyActiveException(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);

        TargetPointer callerStackPointer;
        if (handle.State is StackWalkState.SW_FRAMELESS)
        {
            IPlatformAgnosticContext callerContext = handle.Context.Clone();
            callerContext.Unwind(_target);
            callerStackPointer = callerContext.StackPointer;
        }
        else
        {
            callerStackPointer = handle.FrameAddress;
        }

        TargetPointer pExInfo = GetCurrentExceptionTracker(handle);
        while (pExInfo != TargetPointer.Null)
        {
            Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(pExInfo);
            pExInfo = exceptionInfo.PreviousNestedInfo;

            if (IsInStackRegionUnwoundBySpecifiedException(callerStackPointer, exceptionInfo))
                return true;
        }
        return false;
    }

    private bool IsInStackRegionUnwoundBySpecifiedException(TargetPointer callerStackPointer, Data.ExceptionInfo exceptionInfo)
    {
        // The tracker must be in the second pass (unwind has started), and its stack range must not be empty.
        if ((exceptionInfo.ExceptionFlags & (uint)ExceptionFlagsEnum.UnwindHasStarted) == 0)
            return false;

        // Check for empty range
        if (exceptionInfo.StackLowBound == TargetPointer.PlatformMaxValue(_target)
            && exceptionInfo.StackHighBound == TargetPointer.Null)
        {
            return false;
        }

        return (exceptionInfo.StackLowBound < callerStackPointer) && (callerStackPointer <= exceptionInfo.StackHighBound);
    }

}
