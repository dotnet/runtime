// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Threading
{
    internal partial struct Win32ThreadPoolNativeOverlapped
    {
        internal sealed class OverlappedData
        {
            internal GCHandle[]? _pinnedData;
            internal IOCompletionCallback? _callback;
            internal object? _state;
            internal ExecutionContext? _executionContext;
            internal ThreadPoolBoundHandle? _boundHandle;
            internal PreAllocatedOverlapped? _preAllocated;
            internal bool _completed;
        }
    }
}
