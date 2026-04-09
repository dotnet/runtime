// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics.Tests
{
    public partial class ProcessHandlesTests
    {
        private static partial string GetSafeFileHandleId(SafeFileHandle handle)
        {
            Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status);
            return status.Ino.ToString();
        }
    }
}
