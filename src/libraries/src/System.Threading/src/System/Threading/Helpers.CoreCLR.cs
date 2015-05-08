// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Threading
{
    /// <summary>
    /// Contains Core-CLR specific Sleep and Spin-wait logic
    /// </summary>
    internal static class Helpers
    {
        internal static void Sleep(int milliseconds)
        {
            Thread.Sleep(milliseconds);
        }

        internal static void Spin(int iterations)
        {
            Thread.SpinWait(iterations);
        }
    }
}
