// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.InteropServices.WindowsRuntime;
using System.Runtime.InteropServices.WindowsRuntime;

namespace System.IO
{
    internal partial class StreamOperationAsyncResult
    {
        private void ThrowWithIOExceptionDispatchInfo(Exception e)
        {
            WinRtIOHelper.NativeExceptionToIOExceptionInfo(ExceptionSupport.AttachRestrictedErrorInfo(_completedOperation.ErrorCode)).Throw();
        }
    }
}
