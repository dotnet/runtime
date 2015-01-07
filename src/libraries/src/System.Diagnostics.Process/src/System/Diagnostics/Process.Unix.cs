// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Win32.SafeHandles;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>
        /// Puts a Process component in state to interact with operating system processes that run in a 
        /// special mode by enabling the native property SeDebugPrivilege on the current thread.
        /// </summary>
        public static void EnterDebugMode()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>
        /// Takes a Process component out of the state that lets it interact with operating system processes 
        /// that run in a special mode.
        /// </summary>
        public static void LeaveDebugMode()
        {
            throw new PlatformNotSupportedException();
        }

        /// <summary>Stops the associated process immediately.</summary>
        public void Kill()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>
        /// Instructs the Process component to wait the specified number of milliseconds for the associated process to exit.
        /// </summary>
        public bool WaitForExit(int milliseconds)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets the main module for the associated process.</summary>
        public ProcessModule MainModule
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>Gets a value indicating whether the associated process has been terminated.</summary>
        /// <param name="exitCode">The exit code, if it could be determined; otherwise, null.</param>
        private bool GetHasExited(out int? exitCode)
        {
            var tmp = _signaled; // dummy code just to suppress unused field warning for now
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets the time that the associated process exited.</summary>
        private DateTime ExitTimeCore
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>Gets the time the associated process was started.</summary>
        public DateTime StartTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Gets or sets a value indicating whether the associated process priority
        /// should be temporarily boosted by the operating system when the main window
        /// has focus.
        /// </summary>
        private bool PriorityBoostEnabledCore
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Gets or sets the overall priority category for the associated process.
        /// </summary>
        private ProcessPriorityClass PriorityClassCore
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private IntPtr ProcessorAffinityCore
        {
            get { throw NotImplemented.ByDesign; } // TODO: Implement this
            set { throw NotImplemented.ByDesign; } // TODO: Implement this
        }

        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Gets the ID of the current process.</summary>
        private static int GetCurrentProcessId()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>
        /// Opens a long-term handle to the process, with all access.  If a handle exists,
        /// then it is reused.  If the process has exited, it throws an exception.
        /// </summary>
        private SafeProcessHandle OpenProcessHandle()
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        /// <summary>Starts the process using the supplied start info.</summary>
        /// <param name="startInfo">The start info with which to start the process.</param>
        private bool StartCore(ProcessStartInfo startInfo)
        {
            throw NotImplemented.ByDesign; // TODO: Implement this
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
        

    }
}
