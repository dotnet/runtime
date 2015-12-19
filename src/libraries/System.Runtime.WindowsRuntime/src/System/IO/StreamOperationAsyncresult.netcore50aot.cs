// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Internal.Interop;
using System.Runtime.InteropServices.WindowsRuntime;

namespace System.IO
{
    internal partial class StreamOperationAsyncResult
    {
        private void ThrowWithIOExceptionDispatchInfo(Exception e)
        {
            WinRtIOHelper.NativeExceptionToIOExceptionInfo(_completedOperation.ErrorCode.AttachRestrictedErrorInfo()).Throw();
        }
    }
}
