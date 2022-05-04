// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        private static void SetIdealProcessor(int value)
        {
            // Nop. This is a hint, and there's no good match for the Windows concept.
        }

        private static void ResetIdealProcessorCore()
        {
            // Nop. This is a hint, and there's no good match for the Windows concept.
        }

        private static bool PriorityBoostEnabledCore
        {
            get { return false; }
            set { } // Nop
        }

        private static void SetProcessorAffinity(IntPtr value)
            => throw new PlatformNotSupportedException(); // No ability to change the affinity of a thread in an arbitrary process
    }
}
