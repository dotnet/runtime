// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal readonly struct GCCover_1 : IGCCover
{
    private readonly Target _target;

    internal GCCover_1(Target target)
    {
        _target = target;
    }

    TargetPointer? IGCCover.GetGCCoverageInfo(NativeCodeVersionHandle codeVersionHandle)
    {
        if (!codeVersionHandle.Valid)
        {
            return null;
        }

        if (!codeVersionHandle.IsExplicit)
        {
            // NativeCodeVersion::GetGCCoverageInfo
            IRuntimeTypeSystem rts = _target.Contracts.RuntimeTypeSystem;
            MethodDescHandle md = rts.GetMethodDescHandle(codeVersionHandle.MethodDescAddress);
            return rts.GetGCCoverageInfo(md);
        }
        else
        {
            // NativeCodeVersionNode::GetGCCoverageInfo
            NativeCodeVersionNode codeVersionNode = AsNode(codeVersionHandle);
            return codeVersionNode.GCCoverageInfo;
        }
    }

    private NativeCodeVersionNode AsNode(NativeCodeVersionHandle handle)
    {
        if (handle.CodeVersionNodeAddress == TargetPointer.Null)
        {
            throw new InvalidOperationException("Synthetic NativeCodeVersion does not have a backing node.");
        }

        return _target.ProcessedData.GetOrAdd<NativeCodeVersionNode>(handle.CodeVersionNodeAddress);
    }
}
