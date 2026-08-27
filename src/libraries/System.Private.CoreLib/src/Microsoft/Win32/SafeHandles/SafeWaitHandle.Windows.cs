// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1416 // Windows-only interop; these call sites are Windows-gated.

namespace Microsoft.Win32.SafeHandles
{
    public sealed partial class SafeWaitHandle : SafeHandleZeroOrMinusOneIsInvalid
    {
        protected override bool ReleaseHandle() => Interop.Kernel32.CloseHandle(handle);
    }
}
