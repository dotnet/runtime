// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;

namespace System.Diagnostics
{
    public partial class ProcessThread
    {
        /// <summary>Sets the processor that this thread would ideally like to run on.</summary>
        public int IdealProcessor
        {
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Resets the ideal processor so there is no ideal processor for this thread (e.g.
        /// any processor is ideal).
        /// </summary>
        public void ResetIdealProcessor()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>
        /// Returns or sets whether this thread would like a priority boost if the user interacts
        /// with user interface associated with this thread.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        private ThreadPriorityLevel PriorityLevelCore
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Sets which processors the associated thread is allowed to be scheduled to run on.
        /// Each processor is represented as a bit: bit 0 is processor one, bit 1 is processor
        /// two, etc.  For example, the value 1 means run on processor one, 2 means run on
        /// processor two, 3 means run on processor one or two.
        /// </summary>
        public IntPtr ProcessorAffinity
        {
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>Returns the time the associated thread was started.</summary>
        public DateTime StartTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

    }
}
