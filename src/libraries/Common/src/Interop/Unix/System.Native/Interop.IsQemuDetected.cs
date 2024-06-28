// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;
using System.Threading;

internal static partial class Interop
{
    internal static partial class Sys
    {
        [LibraryImport(Libraries.SystemNative, EntryPoint = "SystemNative_IsQemuDetected")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool IsQemuDetectedImpl();

        private static int s_isQemuDetected;

        internal static bool IsQemuDetected()
        {
            int isQemuDetected = Interlocked.CompareExchange(ref s_isQemuDetected, 0, 0);
            if (isQemuDetected == 0)
            {
                isQemuDetected = IsQemuDetectedImpl() ? 1 : 2;
                int oldValue = Interlocked.CompareExchange(ref s_isQemuDetected, isQemuDetected, 0);
                if (oldValue != 0) // a different thread has managed to update the value
                {
                    isQemuDetected = oldValue;
                }
            }

            return isQemuDetected == 1;
        }
    }
}
