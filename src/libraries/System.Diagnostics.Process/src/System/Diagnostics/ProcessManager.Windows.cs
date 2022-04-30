// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

using static Interop.Advapi32;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        /// <summary>Gets whether the process with the specified ID is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId)
        {
            return IsProcessRunning(processId, ".");
        }

        /// <summary>Gets whether the process with the specified ID on the specified machine is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId, string machineName)
        {
            // Performance optimization for the local machine:
            // First try to OpenProcess by id, if valid handle is returned verify that process is running
            // When the attempt to open a handle fails due to lack of permissions enumerate all processes and compare ids
            // Attempt to open handle for Idle process (processId == 0) fails with ERROR_INVALID_PARAMETER
            if (processId != 0 && !IsRemoteMachine(machineName))
            {
                using (SafeProcessHandle processHandle = Interop.Kernel32.OpenProcess(ProcessOptions.PROCESS_QUERY_LIMITED_INFORMATION | ProcessOptions.SYNCHRONIZE, false, processId))
                {
                    if (processHandle.IsInvalid)
                    {
                        int error = Marshal.GetLastWin32Error();
                        if (error == Interop.Errors.ERROR_INVALID_PARAMETER)
                        {
                            Debug.Assert(processId != 0, "OpenProcess fails with ERROR_INVALID_PARAMETER for Idle Process");
                            return false;
                        }
                    }
                    else
                    {
                        bool signaled = false;
                        return !HasExited(processHandle, ref signaled, out _);
                    }
                }
            }

            return Array.IndexOf(GetProcessIds(machineName), processId) >= 0;
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string? processNameFilter, string machineName)
        {
            if (!IsRemoteMachine(machineName))
            {
                return NtProcessInfoHelper.GetProcessInfos(processNameFilter: processNameFilter);
            }

            ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
            if (string.IsNullOrEmpty(processNameFilter))
            {
                return processInfos;
            }

            ArrayBuilder<ProcessInfo> results = default;
            foreach (ProcessInfo pi in processInfos)
            {
                if (string.Equals(processNameFilter, pi.ProcessName, StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(pi);
                }
            }

            return results.ToArray();
        }

        /// <summary>Gets the ProcessInfo for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The ProcessInfo for the process if it could be found; otherwise, null.</returns>
        public static ProcessInfo? GetProcessInfo(int processId, string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                // remote case: we take the hit of looping through all results
                ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
                foreach (ProcessInfo processInfo in processInfos)
                {
                    if (processInfo.ProcessId == processId)
                    {
                        return processInfo;
                    }
                }
            }
            else
            {
                // local case: do not use performance counter and also attempt to get the matching (by pid) process only
                ProcessInfo[] processInfos = NtProcessInfoHelper.GetProcessInfos(processId);
                if (processInfos.Length == 1)
                {
                    return processInfos[0];
                }
            }

            return null;
        }

        /// <summary>Gets the process name for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The process name for the process if it could be found; otherwise, null.</returns>
        public static string? GetProcessName(int processId, string machineName)
        {
            if (IsRemoteMachine(machineName))
            {
                // remote case: we take the hit of looping through all results
                ProcessInfo[] processInfos = NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true);
                foreach (ProcessInfo processInfo in processInfos)
                {
                    if (processInfo.ProcessId == processId)
                    {
                        return processInfo.ProcessName;
                    }
                }
            }
            else
            {
                // local case: do not use performance counter and also attempt to get the matching (by pid) process only

                string? processName = Interop.Kernel32.GetProcessName((uint)processId);
                if (processName is not null)
                {
                    ReadOnlySpan<char> newName = NtProcessInfoHelper.GetProcessShortName(processName);
                    return newName.SequenceEqual(processName) ?
                        processName :
                        newName.ToString();
                }
            }

            return null;
        }


        /// <summary>Gets the IDs of all processes on the specified machine.</summary>
        /// <param name="machineName">The machine to examine.</param>
        /// <returns>An array of process IDs from the specified machine.</returns>
        public static int[] GetProcessIds(string machineName)
        {
            // Due to the lack of support for EnumModules() on coresysserver, we rely
            // on PerformanceCounters to get the ProcessIds for both remote desktop
            // and the local machine, unlike Desktop on which we rely on PCs only for
            // remote machines.
            return IsRemoteMachine(machineName) ?
                NtProcessManager.GetProcessIds(machineName, true) :
                GetProcessIds();
        }

        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return NtProcessManager.GetProcessIds();
        }

        /// <summary>Gets the ID of a process from a handle to the process.</summary>
        /// <param name="processHandle">The handle.</param>
        /// <returns>The process ID.</returns>
        public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
        {
            return NtProcessManager.GetProcessIdFromHandle(processHandle);
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        public static ProcessModuleCollection GetModules(int processId)
        {
            return NtProcessManager.GetModules(processId);
        }

        private static bool IsRemoteMachineCore(string machineName)
        {
            ReadOnlySpan<char> baseName = machineName.AsSpan(machineName.StartsWith('\\') ? 2 : 0);
            return
                !baseName.Equals(".", StringComparison.Ordinal) &&
                !baseName.Equals(Interop.Kernel32.GetComputerName(), StringComparison.OrdinalIgnoreCase);
        }

        static unsafe ProcessManager()
        {
            // In order to query information (OpenProcess) on some protected processes
            // like csrss, we need SeDebugPrivilege privilege.
            // After removing the dependency on Performance Counter, we don't have a chance
            // to run the code in CLR performance counter to ask for this privilege.
            // So we will try to get the privilege here.
            // We could fail if the user account doesn't have right to do this, but that's fair.

            Interop.Advapi32.LUID luid;
            if (!Interop.Advapi32.LookupPrivilegeValue(null, Interop.Advapi32.SeDebugPrivilege, out luid))
            {
                return;
            }

            SafeTokenHandle? tokenHandle = null;
            try
            {
                if (!Interop.Advapi32.OpenProcessToken(
                        Interop.Kernel32.GetCurrentProcess(),
                        Interop.Kernel32.HandleOptions.TOKEN_ADJUST_PRIVILEGES,
                        out tokenHandle))
                {
                    return;
                }

                Interop.Advapi32.TOKEN_PRIVILEGE tp;
                tp.PrivilegeCount = 1;
                tp.Privileges.Luid = luid;
                tp.Privileges.Attributes = Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED;

                // AdjustTokenPrivileges can return true even if it didn't succeed (when ERROR_NOT_ALL_ASSIGNED is returned).
                Interop.Advapi32.AdjustTokenPrivileges(tokenHandle, false, &tp, 0, null, null);
            }
            finally
            {
                if (tokenHandle != null)
                {
                    tokenHandle.Dispose();
                }
            }
        }

        public static SafeProcessHandle OpenProcess(int processId, int access, bool throwIfExited)
        {
            SafeProcessHandle processHandle = Interop.Kernel32.OpenProcess(access, false, processId);
            int result = Marshal.GetLastWin32Error();
            if (!processHandle.IsInvalid)
            {
                return processHandle;
            }

            if (processId == 0)
            {
                throw new Win32Exception(Interop.Errors.ERROR_ACCESS_DENIED);
            }

            // If the handle is invalid because the process has exited, only throw an exception if throwIfExited is true.
            // Assume the process is still running if the error was ERROR_ACCESS_DENIED for better performance
            if (result != Interop.Errors.ERROR_ACCESS_DENIED && !IsProcessRunning(processId))
            {
                if (throwIfExited)
                {
                    throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, processId.ToString()));
                }
                else
                {
                    return SafeProcessHandle.InvalidHandle;
                }
            }
            throw new Win32Exception(result);
        }

        public static SafeThreadHandle OpenThread(int threadId, int access)
        {
            SafeThreadHandle threadHandle = Interop.Kernel32.OpenThread(access, false, threadId);
            int result = Marshal.GetLastWin32Error();
            if (threadHandle.IsInvalid)
            {
                if (result == Interop.Errors.ERROR_INVALID_PARAMETER)
                    throw new InvalidOperationException(SR.Format(SR.ThreadExited, threadId.ToString()));
                throw new Win32Exception(result);
            }
            return threadHandle;
        }

        // Handle should be valid and have PROCESS_QUERY_LIMITED_INFORMATION | SYNCHRONIZE access
        public static bool HasExited(SafeProcessHandle handle, ref bool signaled, out int exitCode)
        {
            // Although this is the wrong way to check whether the process has exited,
            // it was historically the way we checked for it, and a lot of code then took a dependency on
            // the fact that this would always be set before the pipes were closed, so they would read
            // the exit code out after calling ReadToEnd() or standard output or standard error. In order
            // to allow 259 to function as a valid exit code and to break as few people as possible that
            // took the ReadToEnd dependency, we check for an exit code before doing the more correct
            // check to see if we have been signaled.
            if (Interop.Kernel32.GetExitCodeProcess(handle, out exitCode) && exitCode != Interop.Kernel32.HandleOptions.STILL_ACTIVE)
            {
                return true;
            }

            // The best check for exit is that the kernel process object handle is invalid,
            // or that it is valid and signaled.  Checking if the exit code != STILL_ACTIVE
            // does not guarantee the process is closed,
            // since some process could return an actual STILL_ACTIVE exit code (259).
            if (!signaled) // if we just came from Process.WaitForExit, don't repeat
            {
                using (var wh = new Interop.Kernel32.ProcessWaitHandle(handle))
                {
                    signaled = wh.WaitOne(0);
                }
            }
            if (signaled)
            {
                if (!Interop.Kernel32.GetExitCodeProcess(handle, out exitCode))
                    throw new Win32Exception();

                return true;
            }

            return false;
        }
    }

    /// <devdoc>
    ///     This static class provides the process api for the WinNt platform.
    ///     We use the performance counter api to query process and thread
    ///     information.  Module information is obtained using PSAPI.
    /// </devdoc>
    /// <internalonly/>
    internal static partial class NtProcessManager
    {
        private const int ProcessPerfCounterId = 230;
        private const int ThreadPerfCounterId = 232;
        private const string PerfCounterQueryString = "230 232";
        internal const int IdleProcessID = 0;

        private static readonly Dictionary<string, ValueId> s_valueIds = new Dictionary<string, ValueId>(19)
        {
            { "Pool Paged Bytes", ValueId.PoolPagedBytes },
            { "Pool Nonpaged Bytes", ValueId.PoolNonpagedBytes },
            { "Elapsed Time", ValueId.ElapsedTime },
            { "Virtual Bytes Peak", ValueId.VirtualBytesPeak },
            { "Virtual Bytes", ValueId.VirtualBytes },
            { "Private Bytes", ValueId.PrivateBytes },
            { "Page File Bytes", ValueId.PageFileBytes },
            { "Page File Bytes Peak", ValueId.PageFileBytesPeak },
            { "Working Set Peak", ValueId.WorkingSetPeak },
            { "Working Set", ValueId.WorkingSet },
            { "ID Thread", ValueId.ThreadId },
            { "ID Process", ValueId.ProcessId },
            { "Priority Base", ValueId.BasePriority },
            { "Priority Current", ValueId.CurrentPriority },
            { "% User Time", ValueId.UserTime },
            { "% Privileged Time", ValueId.PrivilegedTime },
            { "Start Address", ValueId.StartAddress },
            { "Thread State", ValueId.ThreadState },
            { "Thread Wait Reason", ValueId.ThreadWaitReason }
        };

        internal static int SystemProcessID
        {
            get
            {
                const int systemProcessIDOnXP = 4;
                return systemProcessIDOnXP;
            }
        }

        public static int[] GetProcessIds(string machineName, bool isRemoteMachine)
        {
            ProcessInfo[] infos = GetProcessInfos(machineName, isRemoteMachine);
            int[] ids = new int[infos.Length];
            for (int i = 0; i < infos.Length; i++)
                ids[i] = infos[i].ProcessId;
            return ids;
        }

        public static int[] GetProcessIds()
        {
            int[] processIds = new int[256];
            int needed;
            while (true)
            {
                int size = processIds.Length * sizeof(int);
                if (!Interop.Kernel32.EnumProcesses(processIds, size, out needed))
                    throw new Win32Exception();

                if (needed == size)
                {
                    processIds = new int[processIds.Length * 2];
                    continue;
                }

                break;
            }

            int[] ids = new int[needed / sizeof(int)];
            Array.Copy(processIds, ids, ids.Length);
            return ids;
        }

        public static ProcessModuleCollection GetModules(int processId)
        {
            return GetModules(processId, firstModuleOnly: false);
        }

        public static ProcessModule? GetFirstModule(int processId)
        {
            ProcessModuleCollection modules = GetModules(processId, firstModuleOnly: true);
            return modules.Count == 0 ? null : modules[0];
        }

        public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
        {
            return Interop.Kernel32.GetProcessId(processHandle);
        }

        public static ProcessInfo[] GetProcessInfos(string machineName, bool isRemoteMachine)
        {
            try
            {
                // We don't want to call library.Close() here because that would cause us to unload all of the perflibs.
                // On the next call to GetProcessInfos, we'd have to load them all up again, which is SLOW!
                PerformanceCounterLib library = PerformanceCounterLib.GetPerformanceCounterLib(machineName, new CultureInfo("en"));
                return GetProcessInfos(library);
            }
            catch (Exception e)
            {
                if (isRemoteMachine)
                {
                    throw new InvalidOperationException(SR.CouldntConnectToRemoteMachine, e);
                }
                else
                {
                    throw;
                }
            }
        }

        private static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library)
        {
            ProcessInfo[] processInfos;

            int retryCount = 5;
            do
            {
                try
                {
                    byte[]? dataPtr = library.GetPerformanceData(PerfCounterQueryString);
                    processInfos = GetProcessInfos(library, ProcessPerfCounterId, ThreadPerfCounterId, dataPtr);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(SR.CouldntGetProcessInfos, e);
                }

                --retryCount;
            }
            while (processInfos.Length == 0 && retryCount != 0);

            if (processInfos.Length == 0)
                throw new InvalidOperationException(SR.ProcessDisabled);

            return processInfos;
        }

        private static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library, int processIndex, int threadIndex, ReadOnlySpan<byte> data)
        {
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>();
            List<ThreadInfo> threadInfos = new List<ThreadInfo>();

            ref readonly PERF_DATA_BLOCK dataBlock = ref MemoryMarshal.AsRef<PERF_DATA_BLOCK>(data);

            int typePos = dataBlock.HeaderLength;
            for (int i = 0; i < dataBlock.NumObjectTypes; i++)
            {
                ref readonly PERF_OBJECT_TYPE type = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(data.Slice(typePos));

                PERF_COUNTER_DEFINITION[] counters = new PERF_COUNTER_DEFINITION[type.NumCounters];

                int counterPos = typePos + type.HeaderLength;
                for (int j = 0; j < type.NumCounters; j++)
                {
                    ref readonly PERF_COUNTER_DEFINITION counter = ref MemoryMarshal.AsRef<PERF_COUNTER_DEFINITION>(data.Slice(counterPos));

                    string counterName = library.GetCounterName(counter.CounterNameTitleIndex);

                    counters[j] = counter;
                    if (type.ObjectNameTitleIndex == processIndex)
                        counters[j].CounterNameTitlePtr = (int)GetValueId(counterName);
                    else if (type.ObjectNameTitleIndex == threadIndex)
                        counters[j].CounterNameTitlePtr = (int)GetValueId(counterName);

                    counterPos += counter.ByteLength;
                }

                int instancePos = typePos + type.DefinitionLength;
                for (int j = 0; j < type.NumInstances; j++)
                {
                    ref readonly PERF_INSTANCE_DEFINITION instance = ref MemoryMarshal.AsRef<PERF_INSTANCE_DEFINITION>(data.Slice(instancePos));

                    ReadOnlySpan<char> instanceName = PERF_INSTANCE_DEFINITION.GetName(in instance, data.Slice(instancePos));

                    if (instanceName.Equals("_Total", StringComparison.Ordinal))
                    {
                        // continue
                    }
                    else if (type.ObjectNameTitleIndex == processIndex)
                    {
                        ProcessInfo processInfo = GetProcessInfo(data.Slice(instancePos + instance.ByteLength), counters);
                        if (processInfo.ProcessId == 0 && !instanceName.Equals("Idle", StringComparison.OrdinalIgnoreCase))
                        {
                            // Sometimes we'll get a process structure that is not completely filled in.
                            // We can catch some of these by looking for non-"idle" processes that have id 0
                            // and ignoring those.
                        }
                        else
                        {
                            if (processInfos.ContainsKey(processInfo.ProcessId))
                            {
                                // We've found two entries in the perfcounters that claim to be the
                                // same process.  We throw an exception.  Is this really going to be
                                // helpful to the user?  Should we just ignore?
                            }
                            else
                            {
                                // the performance counters keep a 15 character prefix of the exe name, and then delete the ".exe",
                                // if it's in the first 15.  The problem is that sometimes that will leave us with part of ".exe"
                                // at the end.  If instanceName ends in ".", ".e", or ".ex" we remove it.
                                if (instanceName.Length == 15)
                                {
                                    if (instanceName.EndsWith(".", StringComparison.Ordinal))
                                    {
                                        instanceName = instanceName.Slice(0, 14);
                                    }
                                    else if (instanceName.EndsWith(".e", StringComparison.Ordinal))
                                    {
                                        instanceName = instanceName.Slice(0, 13);
                                    }
                                    else if (instanceName.EndsWith(".ex", StringComparison.Ordinal))
                                    {
                                        instanceName = instanceName.Slice(0, 12);
                                    }
                                }
                                processInfo.ProcessName = instanceName.ToString();
                                processInfos.Add(processInfo.ProcessId, processInfo);
                            }
                        }
                    }
                    else if (type.ObjectNameTitleIndex == threadIndex)
                    {
                        ThreadInfo threadInfo = GetThreadInfo(data.Slice(instancePos + instance.ByteLength), counters);
                        if (threadInfo._threadId != 0)
                        {
                            threadInfos.Add(threadInfo);
                        }
                    }

                    instancePos += instance.ByteLength;

                    instancePos += MemoryMarshal.AsRef<PERF_COUNTER_BLOCK>(data.Slice(instancePos)).ByteLength;
                }

                typePos += type.TotalByteLength;
            }

            for (int i = 0; i < threadInfos.Count; i++)
            {
                ThreadInfo threadInfo = threadInfos[i];
                if (processInfos.TryGetValue(threadInfo._processId, out ProcessInfo? processInfo))
                {
                    processInfo._threadInfoList.Add(threadInfo);
                }
            }

            ProcessInfo[] temp = new ProcessInfo[processInfos.Values.Count];
            processInfos.Values.CopyTo(temp, 0);
            return temp;
        }

        private static ThreadInfo GetThreadInfo(ReadOnlySpan<byte> instanceData, PERF_COUNTER_DEFINITION[] counters)
        {
            ThreadInfo threadInfo = new ThreadInfo();
            for (int i = 0; i < counters.Length; i++)
            {
                PERF_COUNTER_DEFINITION counter = counters[i];
                long value = ReadCounterValue(counter.CounterType, instanceData.Slice(counter.CounterOffset));
                switch ((ValueId)counter.CounterNameTitlePtr)
                {
                    case ValueId.ProcessId:
                        threadInfo._processId = (int)value;
                        break;
                    case ValueId.ThreadId:
                        threadInfo._threadId = (ulong)value;
                        break;
                    case ValueId.BasePriority:
                        threadInfo._basePriority = (int)value;
                        break;
                    case ValueId.CurrentPriority:
                        threadInfo._currentPriority = (int)value;
                        break;
                    case ValueId.StartAddress:
                        threadInfo._startAddress = (IntPtr)value;
                        break;
                    case ValueId.ThreadState:
                        threadInfo._threadState = (ThreadState)value;
                        break;
                    case ValueId.ThreadWaitReason:
                        threadInfo._threadWaitReason = GetThreadWaitReason((int)value);
                        break;
                }
            }

            return threadInfo;
        }

        internal static ThreadWaitReason GetThreadWaitReason(int value)
        {
            switch (value)
            {
                case 0:
                case 7: return ThreadWaitReason.Executive;
                case 1:
                case 8: return ThreadWaitReason.FreePage;
                case 2:
                case 9: return ThreadWaitReason.PageIn;
                case 3:
                case 10: return ThreadWaitReason.SystemAllocation;
                case 4:
                case 11: return ThreadWaitReason.ExecutionDelay;
                case 5:
                case 12: return ThreadWaitReason.Suspended;
                case 6:
                case 13: return ThreadWaitReason.UserRequest;
                case 14: return ThreadWaitReason.EventPairHigh;
                case 15: return ThreadWaitReason.EventPairLow;
                case 16: return ThreadWaitReason.LpcReceive;
                case 17: return ThreadWaitReason.LpcReply;
                case 18: return ThreadWaitReason.VirtualMemory;
                case 19: return ThreadWaitReason.PageOut;
                default: return ThreadWaitReason.Unknown;
            }
        }

        private static ProcessInfo GetProcessInfo(ReadOnlySpan<byte> instanceData, PERF_COUNTER_DEFINITION[] counters)
        {
            ProcessInfo processInfo = new ProcessInfo();
            for (int i = 0; i < counters.Length; i++)
            {
                PERF_COUNTER_DEFINITION counter = counters[i];
                long value = ReadCounterValue(counter.CounterType, instanceData.Slice(counter.CounterOffset));
                switch ((ValueId)counter.CounterNameTitlePtr)
                {
                    case ValueId.ProcessId:
                        processInfo.ProcessId = (int)value;
                        break;
                    case ValueId.PoolPagedBytes:
                        processInfo.PoolPagedBytes = value;
                        break;
                    case ValueId.PoolNonpagedBytes:
                        processInfo.PoolNonPagedBytes = value;
                        break;
                    case ValueId.VirtualBytes:
                        processInfo.VirtualBytes = value;
                        break;
                    case ValueId.VirtualBytesPeak:
                        processInfo.VirtualBytesPeak = value;
                        break;
                    case ValueId.WorkingSetPeak:
                        processInfo.WorkingSetPeak = value;
                        break;
                    case ValueId.WorkingSet:
                        processInfo.WorkingSet = value;
                        break;
                    case ValueId.PageFileBytesPeak:
                        processInfo.PageFileBytesPeak = value;
                        break;
                    case ValueId.PageFileBytes:
                        processInfo.PageFileBytes = value;
                        break;
                    case ValueId.PrivateBytes:
                        processInfo.PrivateBytes = value;
                        break;
                    case ValueId.BasePriority:
                        processInfo.BasePriority = (int)value;
                        break;
                    case ValueId.HandleCount:
                        processInfo.HandleCount = (int)value;
                        break;
                }
            }
            return processInfo;
        }

        private static ValueId GetValueId(string counterName)
        {
            if (counterName != null)
            {
                ValueId id;
                if (s_valueIds.TryGetValue(counterName, out id))
                    return id;
            }

            return ValueId.Unknown;
        }

        private static long ReadCounterValue(int counterType, ReadOnlySpan<byte> data)
        {
            if ((counterType & PerfCounterOptions.NtPerfCounterSizeLarge) != 0)
                return MemoryMarshal.Read<long>(data);
            else
                return MemoryMarshal.Read<int>(data);
        }

        private enum ValueId
        {
            Unknown = -1,
            HandleCount,
            PoolPagedBytes,
            PoolNonpagedBytes,
            ElapsedTime,
            VirtualBytesPeak,
            VirtualBytes,
            PrivateBytes,
            PageFileBytes,
            PageFileBytesPeak,
            WorkingSetPeak,
            WorkingSet,
            ThreadId,
            ProcessId,
            BasePriority,
            CurrentPriority,
            UserTime,
            PrivilegedTime,
            StartAddress,
            ThreadState,
            ThreadWaitReason
        }
    }
}
