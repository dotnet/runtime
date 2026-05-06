// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    [CLSCompliant(false)]
    public unsafe delegate void IOCompletionCallback(uint errorCode, uint numBytes, NativeOverlapped* pOVERLAP);
}
