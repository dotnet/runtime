// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#if !FEATURE_SINGLE_THREADED
using System.Threading;
#endif

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected override bool ReleaseHandle()
        {
#if !FEATURE_SINGLE_THREADED
            WaitSubsystem.DeleteHandle(handle);
#endif
            return true;
        }
    }
}
