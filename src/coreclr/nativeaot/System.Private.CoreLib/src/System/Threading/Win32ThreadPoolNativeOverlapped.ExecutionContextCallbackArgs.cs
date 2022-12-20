// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal partial struct Win32ThreadPoolNativeOverlapped
    {
        private unsafe class ExecutionContextCallbackArgs
        {
            internal uint _errorCode;
            internal uint _bytesWritten;
            internal Win32ThreadPoolNativeOverlapped* _overlapped;
            internal OverlappedData _data;
        }
    }
}
