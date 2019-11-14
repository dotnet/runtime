// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        /// <summary>Sets the processor that this thread would ideally like to run on.</summary>
        public int IdealProcessor
        {
            set
            {
                // Nop.  This is a hint, and there's no good match for the Windows concept.
            }
        }

        /// <summary>
        /// Resets the ideal processor so there is no ideal processor for this thread (e.g.
        /// any processor is ideal).
        /// </summary>
        public void ResetIdealProcessor()
        {
            // Nop.  This is a hint, and there's no good match for the Windows concept.
        }

        /// <summary>
        /// Returns or sets whether this thread would like a priority boost if the user interacts
        /// with user interface associated with this thread.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get { return false; }
            set { } // Nop
        }

        /// <summary>
        /// Sets which processors the associated thread is allowed to be scheduled to run on.
        /// Each processor is represented as a bit: bit 0 is processor one, bit 1 is processor
        /// two, etc.  For example, the value 1 means run on processor one, 2 means run on
        /// processor two, 3 means run on processor one or two.
        /// </summary>
        public IntPtr ProcessorAffinity
        {
            set { throw new PlatformNotSupportedException(); } // No ability to change the affinity of a thread in an arbitrary process
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
