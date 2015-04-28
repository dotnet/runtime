// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class mincore
    {
        [DllImport(Libraries.Synch, BestFitMapping = false, CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern SafeWaitHandle OpenSemaphore(int desiredAccess, bool inheritHandle, string name);
    }
}
