// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Threading
{
    partial struct Win32ThreadPoolNativeOverlapped
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
