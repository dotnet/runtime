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
        _ = context;
        _ = scanContext;
        _ = _eman.GetRelativeOffset(cbh);

        _eman.GetGCInfo(cbh, out _, out _);

        if (_eman.IsFilterFunclet(cbh))
        {
            flags |= CodeManagerFlags.NoReportUntracked;
        }
        _ = flags;

        // TODO(stackref): Use GCInfoDecoder.EnumerateLiveSlots to enumerate live slots,
        // translate slot descriptors into target addresses using the context,
        // and report them via scanContext.GCEnumCallback / scanContext.GCReportCallback.

        return false;
    }
}
