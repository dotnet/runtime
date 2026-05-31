// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets process infos for each process on the local machine.</summary>
        /// <param name="builder">The builder to add found process infos to.</param>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        public static void GetProcessInfos(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter)
        {
            int cookie = 0;
            Interop.OS.TeamInfo info;

            while ((Interop.OS.GetNextTeamInfo(ref cookie, out info)) == 0)
            {
                ProcessInfo? pi = GetProcessInfoFromTeamInfo(ref info, processNameFilter);
                if (pi != null)
                {
                    builder.Add(pi);
                }
            }
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        internal static unsafe ProcessModuleCollection GetModules(int processId)
        {
            ProcessModuleCollection modules = new ProcessModuleCollection(0);
            int cookie = 0;
            Interop.Image.ImageInfo info;

            while ((Interop.Image.GetNextImageInfo(processId, ref cookie, out info)) == 0)
            {
                string modulePath = GetString(info.name, Interop.Image.MAXPATHLEN);

                var procModule = new ProcessModule(modulePath, Path.GetFileName(modulePath))
                {
                    BaseAddress = (nint)info.text,
                    ModuleMemorySize = info.text_size + info.data_size,
                    EntryPointAddress = IntPtr.Zero // unknown
                };

                if (info.type == Interop.Image.ImageType.B_APP_IMAGE)
                {
                    modules.Insert(0, procModule);
                }
                else
                {
                    modules.Add(procModule);
                }
            }

            return modules;
        }

        internal static string? GetProcessName(int processId, string _ /* machineName */, bool __ /* isRemoteMachine */, ref ProcessInfo? processInfo)
        {
            if (processInfo is not null)
            {
                return processInfo.ProcessName;
            }

            processInfo = CreateProcessInfo(processId);
            return processInfo?.ProcessName;
        }

        internal static ProcessInfo? CreateProcessInfo(int pid, string? processNameFilter = null)
        {
            // Negative PIDs aren't valid
            ArgumentOutOfRangeException.ThrowIfNegative(pid);

            Interop.OS.TeamInfo info;
            int status = Interop.OS.GetTeamInfo(pid, out info);

            if (status != 0)
            {
                return null;
            }

            return GetProcessInfoFromTeamInfo(ref info, processNameFilter);
        }

        // ----------------------------------
        // ---- Unix PAL layer ends here ----
        // ----------------------------------

        private static unsafe ProcessInfo? GetProcessInfoFromTeamInfo(ref Interop.OS.TeamInfo info, string? processNameFilter = null)
        {
            string processName;

            fixed (byte* p = info.name)
            {
                processName = GetString(p, Interop.OS.B_OS_NAME_LENGTH);
            }

            if (!string.IsNullOrEmpty(processNameFilter) &&
                !processNameFilter.Equals(processName, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var procInfo = new ProcessInfo()
            {
                ProcessId = info.team,
                ProcessName = processName,
                SessionId = info.session_id
            };

            {
                nint cookie = 0;
                Interop.OS.AreaInfo areaInfo;

                while (Interop.OS.GetNextAreaInfo(info.team, ref cookie, out areaInfo) == 0)
                {
                    procInfo.VirtualBytes += (long)areaInfo.size;
                    procInfo.WorkingSet += areaInfo.ram_size;
                }
            }

            {
                int cookie = 0;
                Interop.OS.ThreadInfo threadInfo;
                bool first = true;

                while (Interop.OS.GetNextThreadInfo(info.team, ref cookie, out threadInfo) == 0)
                {
                    if (first)
                    {
                        procInfo.BasePriority = threadInfo.priority;
                        first = false;
                    }

                    procInfo._threadInfoList.Add(new ThreadInfo()
                    {
                        _processId = threadInfo.team,
                        _threadId = (ulong)threadInfo.thread,
                        _basePriority = procInfo.BasePriority,
                        _currentPriority = threadInfo.priority,
                        _startAddress = null,
                        _threadState = GetThreadStateFromBThreadState(threadInfo.state),
                        _threadWaitReason = GetThreadWaitReasonFromBThreadState(threadInfo.state),
                    });
                }
            }

            return procInfo;
        }

        private static ThreadState GetThreadStateFromBThreadState(Interop.OS.ThreadState state)
        {
            switch (state)
            {
                case Interop.OS.ThreadState.B_THREAD_RUNNING:
                    return ThreadState.Running;
                case Interop.OS.ThreadState.B_THREAD_READY:
                    return ThreadState.Ready;
                case Interop.OS.ThreadState.B_THREAD_RECEIVING:
                case Interop.OS.ThreadState.B_THREAD_ASLEEP:
                case Interop.OS.ThreadState.B_THREAD_SUSPENDED:
                case Interop.OS.ThreadState.B_THREAD_WAITING:
                    return ThreadState.Wait;
                default:
                    return ThreadState.Unknown;
            }
        }

        private static ThreadWaitReason GetThreadWaitReasonFromBThreadState(Interop.OS.ThreadState state)
        {
            switch (state)
            {
                case Interop.OS.ThreadState.B_THREAD_ASLEEP:
                    return ThreadWaitReason.ExecutionDelay;
                case Interop.OS.ThreadState.B_THREAD_SUSPENDED:
                    return ThreadWaitReason.Suspended;
                default:
                    return ThreadWaitReason.Unknown;
            }
        }

        private static unsafe string GetString(byte* ptr, int maxLength)
        {
            int length = new ReadOnlySpan<byte>(ptr, maxLength).IndexOf((byte)0);
            return new string((sbyte*)ptr, 0, (length < 0) ? maxLength : length);
        }
    }
}
