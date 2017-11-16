// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return EnumerableHelpers.ToArray(EnumerateProcessIds());
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            ThrowIfRemoteMachine(machineName);
            int[] procIds = GetProcessIds(machineName);

            // Iterate through all process IDs to load information about each process
            var reusableReader = new ReusableTextReader();
            var processes = new List<ProcessInfo>(procIds.Length);
            foreach (int pid in procIds)
            {
                ProcessInfo pi = CreateProcessInfo(pid, reusableReader);
                if (pi != null)
                {
                    processes.Add(pi);
                }
            }

            return processes.ToArray();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static ProcessModuleCollection GetModules(int processId)
        {
            var modules = new ProcessModuleCollection(0);

            // Process from the parsed maps file each entry representing a module
            foreach (Interop.procfs.ParsedMapsModule entry in Interop.procfs.ParseMapsModules(processId))
            {
                int sizeOfImage = (int)(entry.AddressRange.Value - entry.AddressRange.Key);

                // A single module may be split across multiple map entries; consolidate based on
                // the name and address ranges of sequential entries.
                if (modules.Count > 0)
                {
                    ProcessModule module = modules[modules.Count - 1];
                    if (module.FileName == entry.FileName &&
                        ((long)module.BaseAddress + module.ModuleMemorySize == entry.AddressRange.Key))
                    {
                        // Merge this entry with the previous one
                        module.ModuleMemorySize += sizeOfImage;
                        continue;
                    }
                }

                // It's not a continuation of a previous entry but a new one: add it.
                unsafe
                {
                    modules.Add(new ProcessModule()
                    {
                        FileName = entry.FileName,
                        ModuleName = Path.GetFileName(entry.FileName),
                        BaseAddress = new IntPtr(unchecked((void*)entry.AddressRange.Key)),
                        ModuleMemorySize = sizeOfImage,
                        EntryPointAddress = IntPtr.Zero // unknown
                    });
                }
            }

            // Move the main executable module to be the first in the list if it's not already
            string exePath = Process.GetExePath(processId);
            for (int i = 0; i < modules.Count; i++)
            {
                ProcessModule module = modules[i];
                if (module.FileName == exePath)
                {
                    if (i > 0)
                    {
                        modules.RemoveAt(i);
                        modules.Insert(0, module);
                    }
                    break;
                }
            }

            // Return the set of modules found
            return modules;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        /// <summary>
        /// Creates a ProcessInfo from the specified process ID.
        /// </summary>
        internal static ProcessInfo CreateProcessInfo(int pid, ReusableTextReader reusableReader = null)
        {
            if (reusableReader == null)
            {
                reusableReader = new ReusableTextReader();
            }

            Interop.procfs.ParsedStat stat;
            return Interop.procfs.TryReadStatFile(pid, out stat, reusableReader) ?
                CreateProcessInfo(stat, reusableReader) :
                null;
        }

        /// <summary>
        /// Creates a ProcessInfo from the data parsed from a /proc/pid/stat file and the associated tasks directory.
        /// </summary>
        internal static ProcessInfo CreateProcessInfo(Interop.procfs.ParsedStat procFsStat, ReusableTextReader reusableReader)
        {
            int pid = procFsStat.pid;

            var pi = new ProcessInfo()
            {
                ProcessId = pid,
                ProcessName = procFsStat.comm,
                BasePriority = (int)procFsStat.nice,
                VirtualBytes = (long)procFsStat.vsize,
                WorkingSet = procFsStat.rss * Environment.SystemPageSize,
                SessionId = procFsStat.session,

                // We don't currently fill in the other values.
                // A few of these could probably be filled in from getrusage,
                // but only for the current process or its children, not for
                // arbitrary other processes.
            };

            // Then read through /proc/pid/task/ to find each thread in the process...
            string tasksDir = Interop.procfs.GetTaskDirectoryPathForProcess(pid);
            try
            {
                foreach (string taskDir in Directory.EnumerateDirectories(tasksDir))
                {
                    // ...and read its associated /proc/pid/task/tid/stat file to create a ThreadInfo
                    string dirName = Path.GetFileName(taskDir);
                    int tid;
                    Interop.procfs.ParsedStat stat;
                    if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out tid) &&
                        Interop.procfs.TryReadStatFile(pid, tid, out stat, reusableReader))
                    {
                        unsafe
                        {
                            pi._threadInfoList.Add(new ThreadInfo()
                            {
                                _processId = pid,
                                _threadId = (ulong)tid,
                                _basePriority = pi.BasePriority,
                                _currentPriority = (int)stat.nice,
                                _startAddress = IntPtr.Zero,
                                _threadState = ProcFsStateToThreadState(stat.state),
                                _threadWaitReason = ThreadWaitReason.Unknown
                            });
                        }
                    }
                }
            }
            catch (IOException)
            {
                // Between the time that we get an ID and the time that we try to read the associated 
                // directories and files in procfs, the process could be gone.
            }

            // Finally return what we've built up
            return pi;
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        /// <summary>Enumerates the IDs of all processes on the current machine.</summary>
        internal static IEnumerable<int> EnumerateProcessIds()
        {
            // Parse /proc for any directory that's named with a number.  Each such
            // directory represents a process.
            foreach (string procDir in Directory.EnumerateDirectories(Interop.procfs.RootPath))
            {
                string dirName = Path.GetFileName(procDir);
                int pid;
                if (int.TryParse(dirName, NumberStyles.Integer, CultureInfo.InvariantCulture, out pid))
                {
                    Debug.Assert(pid >= 0);
                    yield return pid;
                }
            }
        }

        /// <summary>Gets a ThreadState to represent the value returned from the status field of /proc/pid/stat.</summary>
        /// <param name="c">The status field value.</param>
        /// <returns></returns>
        private static ThreadState ProcFsStateToThreadState(char c)
        {
            switch (c)
            {
                case 'R': // Running
                    return ThreadState.Running;

                case 'D': // Waiting on disk
                case 'P': // Parked
                case 'S': // Sleeping in a wait
                case 't': // Tracing/debugging
                case 'T': // Stopped on a signal
                    return ThreadState.Wait;

                case 'x': // dead
                case 'X': // Dead
                case 'Z': // Zombie
                    return ThreadState.Terminated;

                case 'W': // Paging or waking
                case 'K': // Wakekill
                    return ThreadState.Transition;

                default:
                    Debug.Fail($"Unexpected status character: {c}");
                    return ThreadState.Unknown;
            }
        }

    }
}
