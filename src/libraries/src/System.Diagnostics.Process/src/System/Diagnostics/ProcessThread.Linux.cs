// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            get { return false; } // Nop
            set { } // Nop
        }

        /// <summary>
        /// Returns or sets the priority level of the associated thread.  The priority level is
        /// not an absolute level, but instead contributes to the actual thread priority by
        /// considering the priority class of the process.
        /// </summary>
        private ThreadPriorityLevel PriorityLevelCore
        {
            // This mapping is relatively arbitrary.  0 is normal based on the man page,
            // and the other values above and below are simply distributed evenly.
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                Debug.Assert(stat.nice >= -20 && stat.nice <= 20);
                return
                    stat.nice < -15 ? ThreadPriorityLevel.TimeCritical :
                    stat.nice < -10 ? ThreadPriorityLevel.Highest :
                    stat.nice < -5 ? ThreadPriorityLevel.AboveNormal :
                    stat.nice == 0 ? ThreadPriorityLevel.Normal :
                    stat.nice <= 5 ? ThreadPriorityLevel.BelowNormal :
                    stat.nice <= 10 ? ThreadPriorityLevel.Lowest :
                    ThreadPriorityLevel.Idle;
            }
            set
            {
                throw new PlatformNotSupportedException();
            }
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

        /// <summary>
        /// Returns the amount of time the thread has spent running code inside the operating
        /// system core.
        /// </summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Process.TicksToTimeSpan(stat.stime);
            }
        }

        /// <summary>Returns the time the associated thread was started.</summary>
        public DateTime StartTime
        {
            get { return Process.BootTimeToDateTime(GetStat().starttime); }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent utilizing the CPU.
        /// It is the sum of the System.Diagnostics.ProcessThread.UserProcessorTime and
        /// System.Diagnostics.ProcessThread.PrivilegedProcessorTime.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Process.TicksToTimeSpan(stat.utime + stat.stime);
            }
        }

        /// <summary>
        /// Returns the amount of time the associated thread has spent running code
        /// inside the application (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return Process.TicksToTimeSpan(stat.utime);
            }
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private Interop.procfs.ParsedStat GetStat()
        {
            return Interop.procfs.ReadStatFile(pid: _processId, tid: Id);
        }
    }
}
