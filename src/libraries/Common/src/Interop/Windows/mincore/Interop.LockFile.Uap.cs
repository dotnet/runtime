// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;

internal partial class Interop
{
    internal partial class mincore
    {
        internal static bool LockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh)
        {
            throw new PlatformNotSupportedException();
        }

        internal static bool UnlockFile(SafeFileHandle handle, int offsetLow, int offsetHigh, int countLow, int countHigh)
        {
            throw new PlatformNotSupportedException();
        }
    }
}
