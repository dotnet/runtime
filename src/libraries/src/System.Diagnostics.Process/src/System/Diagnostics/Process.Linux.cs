// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace System.Diagnostics
{
    public partial class Process : IDisposable
    {
        /// <summary>
        /// Creates an array of <see cref="Process"/> components that are associated with process resources on a
        /// remote computer. These process resources share the specified process name.
        /// </summary>
        public static Process[] GetProcessesByName(string processName, string machineName)
        {
            ProcessManager.ThrowIfRemoteMachine(machineName);
            if (processName == null)
            {
                processName = string.Empty;
            }

            var reusableReader = new ReusableTextReader();
            var processes = new List<Process>();
            foreach (int pid in ProcessManager.EnumerateProcessIds())
            {
                Interop.procfs.ParsedStat parsedStat;
                if (Interop.procfs.TryReadStatFile(pid, out parsedStat, reusableReader) &&
                    string.Equals(processName, parsedStat.comm, StringComparison.OrdinalIgnoreCase))
                {
                    ProcessInfo processInfo = ProcessManager.CreateProcessInfo(parsedStat, reusableReader);
                    processes.Add(new Process(machineName, false, processInfo.ProcessId, processInfo));
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets the amount of time the process has spent running code inside the operating system core.</summary>
        public TimeSpan PrivilegedProcessorTime
        {
            get
            {
                return TicksToTimeSpan(GetStat().stime);
            }
        }

        /// <summary>Gets the time the associated process was started.</summary>
        public DateTime StartTime
        {
            get
            {
                return BootTimeToDateTime(TicksToTimeSpan(GetStat().starttime));
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent utilizing the CPU.
        /// It is the sum of the <see cref='System.Diagnostics.Process.UserProcessorTime'/> and
        /// <see cref='System.Diagnostics.Process.PrivilegedProcessorTime'/>.
        /// </summary>
        public TimeSpan TotalProcessorTime
        {
            get
            {
                Interop.procfs.ParsedStat stat = GetStat();
                return TicksToTimeSpan(stat.utime + stat.stime);
            }
        }

        /// <summary>
        /// Gets the amount of time the associated process has spent running code
        /// inside the application portion of the process (not the operating system core).
        /// </summary>
        public TimeSpan UserProcessorTime
        {
            get
            {
                return TicksToTimeSpan(GetStat().utime);
            }
        }

        partial void EnsureHandleCountPopulated()
        {
            if (_processInfo.HandleCount <= 0 && _haveProcessId)
            {
                string path = Interop.procfs.GetFileDescriptorDirectoryPathForProcess(_processId);
                if (Directory.Exists(path))
                {
                    try
                    {
                        _processInfo.HandleCount = Directory.GetFiles(path, "*", SearchOption.TopDirectoryOnly).Length;
                    }
                    catch (DirectoryNotFoundException) // Occurs when the process is deleted between the Exists check and the GetFiles call.
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets which processors the threads in this process can be scheduled to run on.
        /// </summary>
        private unsafe IntPtr ProcessorAffinityCore
        {
            get
            {
                EnsureState(State.HaveId);

                IntPtr set;
                if (Interop.Sys.SchedGetAffinity(_processId, out set) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }

                return set;
            }
            set
            {
                EnsureState(State.HaveId);

                if (Interop.Sys.SchedSetAffinity(_processId, ref value) != 0)
                {
                    throw new Win32Exception(); // match Windows exception
                }
            }
        }

        /// <summary>
        /// Make sure we have obtained the min and max working set limits.
        /// </summary>
        private void GetWorkingSetLimits(out IntPtr minWorkingSet, out IntPtr maxWorkingSet)
        {
            minWorkingSet = IntPtr.Zero; // no defined limit available
            ulong rsslim = GetStat().rsslim;

            // rsslim is a ulong, but maxWorkingSet is an IntPtr, so we need to cap rsslim
            // at the max size of IntPtr.  This often happens when there is no configured 
            // rsslim other than ulong.MaxValue, which without these checks would show up
            // as a maxWorkingSet == -1.
            switch (IntPtr.Size)
            {
                case 4:
                    if (rsslim > int.MaxValue)
                        rsslim = int.MaxValue;
                    break;
                case 8:
                    if (rsslim > long.MaxValue)
                        rsslim = long.MaxValue;
                    break;
            }

            maxWorkingSet = (IntPtr)rsslim;
        }

        /// <summary>Sets one or both of the minimum and maximum working set limits.</summary>
        /// <param name="newMin">The new minimum working set limit, or null not to change it.</param>
        /// <param name="newMax">The new maximum working set limit, or null not to change it.</param>
        /// <param name="resultingMin">The resulting minimum working set limit after any changes applied.</param>
        /// <param name="resultingMax">The resulting maximum working set limit after any changes applied.</param>
        private void SetWorkingSetLimitsCore(IntPtr? newMin, IntPtr? newMax, out IntPtr resultingMin, out IntPtr resultingMax)
        {
            // RLIMIT_RSS with setrlimit not supported on Linux > 2.4.30.
            throw new PlatformNotSupportedException();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>Gets the path to the current executable, or null if it could not be retrieved.</summary>
        private static string GetExePath()
        {
            // Determine the maximum size of a path
            int maxPath = Interop.Sys.MaxPath;

            // Start small with a buffer allocation, and grow only up to the max path
            for (int pathLen = 256; pathLen < maxPath; pathLen *= 2)
            {
                // Read from procfs the symbolic link to this process' executable
                byte[] buffer = new byte[pathLen + 1]; // +1 for null termination
                int resultLength = Interop.Sys.ReadLink(Interop.procfs.SelfExeFilePath, buffer, pathLen);

                // If we got one, null terminate it (readlink doesn't do this) and return the string
                if (resultLength > 0)
                {
                    buffer[resultLength] = (byte)'\0';
                    return Encoding.UTF8.GetString(buffer, 0, resultLength);
                }

                // If the buffer was too small, loop around again and try with a larger buffer.
                // Otherwise, bail.
                if (resultLength == 0 || Interop.Sys.GetLastError() != Interop.Error.ENAMETOOLONG)
                {
                    break;
                }
            }

            // Could not get a path
            return null;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Reads the stats information for this process from the procfs file system.</summary>
        private Interop.procfs.ParsedStat GetStat()
        {
            EnsureState(State.HaveId);
            Interop.procfs.ParsedStat stat;
            if (!Interop.procfs.TryReadStatFile(_processId, out stat, new ReusableTextReader()))
            {
                throw new Win32Exception(SR.ProcessInformationUnavailable);
            }
            return stat;
        }

    }
}
