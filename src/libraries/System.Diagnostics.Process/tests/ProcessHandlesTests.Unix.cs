// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics.Tests
{
    public partial class ProcessHandlesTests
    {
        private static string GetSafeFileHandleId(SafeFileHandle handle)
        {
            if (Interop.Sys.FStat(handle, out Interop.Sys.FileStatus status) != 0)
            {
                throw new Win32Exception();
            }
            return FormattableString.Invariant($"{status.Dev}:{status.Ino}");
        }
    }
}
