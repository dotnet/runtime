// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;


internal class GCInfo_1<TTraits> : IGCInfo where TTraits : IGCInfoTraits
{
    private readonly Target _target;

    internal GCInfo_1(Target target)
    {
        _target = target;
    }

    IGCInfoHandle IGCInfo.DecodeGCInfo(TargetPointer gcInfoAddress, uint gcVersion)
        => new GcInfoDecoder<TTraits>(_target, gcInfoAddress, gcVersion);

    uint IGCInfo.GetCodeLength(IGCInfoHandle gcInfoHandle)
    {
        GcInfoDecoder<TTraits> decoder = AssertCorrectHandle(gcInfoHandle);
        return decoder.GetCodeLength();
    }

    private static GcInfoDecoder<TTraits> AssertCorrectHandle(IGCInfoHandle gcInfoHandle)
    {
        if (gcInfoHandle is not GcInfoDecoder<TTraits> handle)
        {
            throw new ArgumentException("Invalid GC info handle", nameof(gcInfoHandle));
        }

        return handle;
    }
}
