// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace Microsoft.Diagnostics.DataContractReader.Legacy;

internal sealed class StackWalkHandleData
{
    private readonly IStackWalk _stackWalk;
    private readonly ThreadData _threadData;
    private IEnumerator<IStackDataFrameHandle>? _enumerator;

    public StackWalkHandleData(IStackWalk stackWalk, ThreadData threadData)
    {
        _stackWalk = stackWalk;
        _threadData = threadData;
    }

    public IStackDataFrameHandle? Current { get; private set; }
    public bool IsValid => Current is not null;
    public nuint LegacyHandle { get; set; }
    public long PendingStackPointerDelta { get; set; }
    public void Reset(byte[] contextBuffer, bool isFirst)
    {
        _enumerator?.Dispose();
        _enumerator = _stackWalk.CreateStackWalk(_threadData, contextBuffer, isFirst, false).GetEnumerator();
        PendingStackPointerDelta = 0;
        Advance();
        if (Current is null)
            throw Marshal.GetExceptionForHR(HResults.E_FAIL)!;
    }

    public void Advance()
    {
        PendingStackPointerDelta = 0;
        Current = _enumerator is not null && _enumerator.MoveNext() ? _enumerator.Current : null;
    }

    /// <summary>
    /// Advance to the next non-Frame, non-SkippedFrame stop.
    /// </summary>
    public void AdvanceForUnwind()
    {
        while (true)
        {
            Advance();
            if (Current is null)
                return;
            if (Current.State is StackWalkState.Frame or StackWalkState.SkippedFrame)
                continue;
            return;
        }
    }

    public nuint GetHandle()
    {
        GCHandle gcHandle = GCHandle.Alloc(this);
        return (nuint)GCHandle.ToIntPtr(gcHandle).ToInt64();
    }

    public void Dispose() => _enumerator?.Dispose();
}
