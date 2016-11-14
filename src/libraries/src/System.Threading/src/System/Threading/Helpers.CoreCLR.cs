// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Runtime.Augments;

namespace System.Threading
{
    /// <summary>
    /// Contains Core-CLR specific Sleep and Spin-wait logic
    /// </summary>
    internal static class Helpers
    {
        internal static void Sleep(int milliseconds)
        {
            RuntimeThread.Sleep(milliseconds);
        }

        internal static void Spin(int iterations)
        {
            RuntimeThread.SpinWait(iterations);
        }
    }
}
