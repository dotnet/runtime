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
        return thread.ExceptionTracker;
    }

    private bool HasFrameBeenUnwoundByAnyActiveException(IStackDataFrameHandle stackDataFrameHandle)
    {
        StackDataFrameHandle handle = AssertCorrectHandle(stackDataFrameHandle);

        TargetPointer exInfo = GetCurrentExceptionTracker(handle);
        while (exInfo != TargetPointer.Null)
        {
            Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exInfo);
            exInfo = exceptionInfo.PreviousNestedInfo;

            TargetPointer stackPointer;
            if (handle.State is StackWalkState.SW_FRAMELESS)
            {
                IPlatformAgnosticContext callerContext = handle.Context.Clone();
                callerContext.Unwind(_target);
                stackPointer = callerContext.StackPointer;
            }
            else
            {
                stackPointer = handle.Context.FramePointer;
            }
            if (IsInStackRegionUnwoundBySpecifiedException(handle.ThreadData, stackPointer))
            {
                return true;
            }
        }
        return false;
    }

    private bool IsInStackRegionUnwoundBySpecifiedException(ThreadData threadData, TargetPointer stackPointer)
    {
        // See ExInfo::IsInStackRegionUnwoundBySpecifiedException for explanation
        Data.Thread thread = _target.ProcessedData.GetOrAdd<Data.Thread>(threadData.ThreadAddress);
        TargetPointer exInfo = thread.ExceptionTracker;
        while (exInfo != TargetPointer.Null)
        {
            Data.ExceptionInfo exceptionInfo = _target.ProcessedData.GetOrAdd<Data.ExceptionInfo>(exInfo);
            if (exceptionInfo.StackLowBound < stackPointer && stackPointer <= exceptionInfo.StackHighBound)
            {
                return true;
            }
            exInfo = exceptionInfo.PreviousNestedInfo;
        }
        return false;
    }

}
