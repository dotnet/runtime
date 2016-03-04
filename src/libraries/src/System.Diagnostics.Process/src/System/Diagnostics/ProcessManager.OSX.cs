// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return Interop.libproc.proc_listallpids();
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            int[] procIds = GetProcessIds(machineName);

            // Iterate through all process IDs to load information about each process
            var processes = new List<ProcessInfo>(procIds.Length);
            foreach (int pid in procIds)
            {
                ProcessInfo pi = CreateProcessInfo(pid);
                if (pi != null)
                {
                    processes.Add(pi);
                }
            }

            return processes.ToArray();
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        private static ProcessInfo CreateProcessInfo(int pid)
        {
            // Negative PIDs aren't valid
            if (pid < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pid));
            }

            ProcessInfo procInfo = new ProcessInfo()
            {
                ProcessId = pid
            };

            // Try to get the task info. This can fail if the user permissions don't permit
            // this user context to query the specified process
            Interop.libproc.proc_taskallinfo? info = Interop.libproc.GetProcessInfoById(pid);
            if (info.HasValue)
            {
                // Set the values we have; all the other values don't have meaning or don't exist on OSX
                Interop.libproc.proc_taskallinfo temp = info.Value;
                unsafe { procInfo.ProcessName = Marshal.PtrToStringAnsi(new IntPtr(temp.pbsd.pbi_comm)); }
                procInfo.BasePriority = temp.pbsd.pbi_nice;
                procInfo.VirtualBytes = (long)temp.ptinfo.pti_virtual_size;
                procInfo.WorkingSet = (long)temp.ptinfo.pti_resident_size;
            }

            // Get the sessionId for the given pid, getsid returns -1 on error
            int sessionId = Interop.Sys.GetSid(pid);
            if (sessionId != -1)
                procInfo.SessionId = sessionId;
            
            // Create a threadinfo for each thread in the process
            List<KeyValuePair<ulong, Interop.libproc.proc_threadinfo?>> lstThreads = Interop.libproc.GetAllThreadsInProcess(pid);
            foreach (KeyValuePair<ulong, Interop.libproc.proc_threadinfo?> t in lstThreads)
            {
                var ti = new ThreadInfo()
                {
                    _processId = pid,
                    _threadId = t.Key,
                    _basePriority = procInfo.BasePriority,
                    _startAddress = IntPtr.Zero
                };

                // Fill in additional info if we were able to retrieve such data about the thread
                if (t.Value.HasValue)
                {
                    ti._currentPriority = t.Value.Value.pth_curpri;
                    ti._threadState = ConvertOsxThreadRunStateToThreadState((Interop.libproc.ThreadRunState)t.Value.Value.pth_run_state);
                    ti._threadWaitReason = ConvertOsxThreadFlagsToWaitReason((Interop.libproc.ThreadFlags)t.Value.Value.pth_flags);
                }

                procInfo._threadInfoList.Add(ti);
            }

            return procInfo;
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ModuleInfo[] GetModuleInfos(int processId)
        {
            var modules = new List<ModuleInfo>();

            // On Linux we can read from procfs.  On OS X we can get similar data by getting the output from vmmap.
            // This is the same basic approach taken by the PAL in libcoreclr.
            IntPtr popenStdout = Interop.Sys.POpen($"/usr/bin/vmmap -interleaved {processId} -wide", "r");
            Debug.Assert(popenStdout != IntPtr.Zero, $"popen failed: {Interop.Sys.GetLastErrorInfo()}");
            if (popenStdout != IntPtr.Zero) // if this failed, we'll just return an empty set of modules
            {
                try
                {
                    string line;
                    while ((line = Interop.Sys.GetLine(popenStdout)) != null)
                    {
                        // Example line (amongst other lines that don't look like this)
                        // __TEXT   0000000104c85000-000000010513b000 [ 4824K] r-x/rwx SM=COW  /Users/mikem/coreclr/bin/Product/OSx.x64.Debug/libcoreclr.dylib
                        const string __TEXT = "__TEXT";
                        const int AddressLength = 16;
                        const string PathStart = " /";

                        // Only care about __TEXT entries
                        if (!line.StartsWith(__TEXT))
                        {
                            continue;
                        }

                        // Move to the address
                        int pos = __TEXT.Length;
                        for (; pos < line.Length && char.IsWhiteSpace(line[pos]); pos++) ;
                        int addressEndPos = checked(pos + AddressLength + 1 + AddressLength);
                        if (addressEndPos >= line.Length)
                        {
                            continue;
                        }

                        // Parse the address range
                        ulong startAddress, endAddress;
                        if (!ulong.TryParse(line.Substring(pos, AddressLength), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out startAddress) ||
                            !ulong.TryParse(line.Substring(pos + AddressLength + 1, AddressLength), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out endAddress))
                        {
                            continue;
                        }

                        // Find the first space-slash.  If it exists, that's the start of the path.
                        int filePathPos = line.IndexOf(PathStart, addressEndPos);
                        if (filePathPos < 0)
                        {
                            continue;
                        }
                        string filePath = line.Substring(filePathPos).Trim();

                        // Add the module
                        modules.Add(new ModuleInfo()
                        {
                            _fileName = filePath,
                            _baseName = Path.GetFileName(filePath),
                            _baseOfDll = new IntPtr((long)startAddress),
                            _sizeOfImage = (int)(endAddress - startAddress),
                            _entryPoint = IntPtr.Zero // unknown
                        });
                    }
                }
                finally
                {
                    int rv = Interop.Sys.PClose(popenStdout);
                    Debug.Assert(rv == 0, $"pclose failed: {Interop.Sys.GetLastErrorInfo()}"); // ignore any release failures from closing
                }
            }

            // Return the set of modules found.
            return modules.ToArray();
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private static System.Diagnostics.ThreadState ConvertOsxThreadRunStateToThreadState(Interop.libproc.ThreadRunState state)
        {
            switch (state)
            {
                case Interop.libproc.ThreadRunState.TH_STATE_RUNNING:
                    return System.Diagnostics.ThreadState.Running;
                case Interop.libproc.ThreadRunState.TH_STATE_STOPPED:
                    return System.Diagnostics.ThreadState.Terminated;
                case Interop.libproc.ThreadRunState.TH_STATE_HALTED:
                    return System.Diagnostics.ThreadState.Wait;
                case Interop.libproc.ThreadRunState.TH_STATE_UNINTERRUPTIBLE:
                    return System.Diagnostics.ThreadState.Running;
                case Interop.libproc.ThreadRunState.TH_STATE_WAITING:
                    return System.Diagnostics.ThreadState.Standby;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private static System.Diagnostics.ThreadWaitReason ConvertOsxThreadFlagsToWaitReason(Interop.libproc.ThreadFlags flags)
        {
            // Since ThreadWaitReason isn't a flag, we have to do a mapping and will lose some information.
            if ((flags & Interop.libproc.ThreadFlags.TH_FLAGS_SWAPPED) == Interop.libproc.ThreadFlags.TH_FLAGS_SWAPPED)
                return System.Diagnostics.ThreadWaitReason.PageOut;
            else
                return System.Diagnostics.ThreadWaitReason.Unknown; // There isn't a good mapping for anything else
        }
    }
}
