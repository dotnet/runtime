// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Threading
{
    /// <summary>
    /// Contains Core-CLR specific Sleep and Spin-wait logic
    /// </summary>
    internal static class Helpers
    {
        private static readonly WaitHandle s_sleepHandle = new System.Threading.ManualResetEvent(false);

        internal static void Sleep(uint milliseconds)
        {
            s_sleepHandle.WaitOne((int)milliseconds);
        }

        internal static void Spin(int iterations)
        {
            Thread.SpinWait(iterations);
        }
    }
}
