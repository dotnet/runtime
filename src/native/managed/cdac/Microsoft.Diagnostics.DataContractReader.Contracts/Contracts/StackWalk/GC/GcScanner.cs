// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts.StackWalkHelpers;

internal class GcScanner
{
    public enum CodeManagerFlags : uint
    {
        ActiveStackFrame = 0x1,
        ExecutionAborted = 0x2,
        ParentOfFuncletStackFrame = 0x40,
        NoReportUntracked = 0x80,
        ReportFPBasedSlotsOnly = 0x200,
    }

    private readonly Target _target;
    private readonly IExecutionManager _eman;
    private readonly IGCInfo _gcInfo;

    internal GcScanner(Target target)
    {
        _target = target;
        _eman = target.Contracts.ExecutionManager;
        _gcInfo = target.Contracts.GCInfo;
    }

    public bool EnumGcRefs(
        IPlatformAgnosticContext context,
        CodeBlockHandle cbh,
        CodeManagerFlags flags,
        GcScanContext scanContext)
    {
        TargetNUInt curOffs = _eman.GetRelativeOffset(cbh);

        _eman.GetGCInfo(cbh, out TargetPointer pGcInfo, out uint gcVersion);

        if (_eman.IsFilterFunclet(cbh))
        {
            // Filters are the only funclet that run during the 1st pass, and must have
            // both the leaf and the parent frame reported.  In order to avoid double
            // reporting of the untracked variables, do not report them for the filter.
            flags |= CodeManagerFlags.NoReportUntracked;
        }

        return false;
    }
}
