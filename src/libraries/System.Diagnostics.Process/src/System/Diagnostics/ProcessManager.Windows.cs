// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
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
            // First try to OpenProcess by id, if valid handle is returned, the process is definitely running
            // Otherwise enumerate all processes and compare ids
            if (!IsRemoteMachine(machineName))
            {
                using (SafeProcessHandle processHandle = Interop.Kernel32.OpenProcess(ProcessOptions.PROCESS_QUERY_INFORMATION, false, processId))
                {
                    if (!processHandle.IsInvalid)
                    {
                        return true;
                    }
                }
            }

            return Array.IndexOf(GetProcessIds(machineName), processId) >= 0;
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            return IsRemoteMachine(machineName) ?
                NtProcessManager.GetProcessInfos(machineName, isRemoteMachine: true) :
                NtProcessInfoHelper.GetProcessInfos();
        }

        /// <summary>Gets the ProcessInfo for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The ProcessInfo for the process if it could be found; otherwise, null.</returns>
        public static ProcessInfo GetProcessInfo(int processId, string machineName)
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
                ProcessInfo[] processInfos = NtProcessInfoHelper.GetProcessInfos(pid => pid == processId);
                if (processInfos.Length == 1)
                {
                    return processInfos[0];
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
            ReadOnlySpan<char> baseName = machineName.AsSpan(machineName.StartsWith("\\", StringComparison.Ordinal) ? 2 : 0);
            return
                !baseName.Equals(".", StringComparison.Ordinal) &&
                !baseName.Equals(Interop.Kernel32.GetComputerName(), StringComparison.OrdinalIgnoreCase);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        static unsafe ProcessManager()
        {
            // In order to query information (OpenProcess) on some protected processes
            // like csrss, we need SeDebugPrivilege privilege.
            // After removing the dependency on Performance Counter, we don't have a chance
            // to run the code in CLR performance counter to ask for this privilege.
            // So we will try to get the privilege here.
            // We could fail if the user account doesn't have right to do this, but that's fair.

            Interop.Advapi32.LUID luid = new Interop.Advapi32.LUID();
            if (!Interop.Advapi32.LookupPrivilegeValue(null, Interop.Advapi32.SeDebugPrivilege, out luid))
            {
                return;
            }

            SafeTokenHandle tokenHandle = null;
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
                throw new Win32Exception(5);
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
            int size;
            for (; ; )
            {
                if (!Interop.Kernel32.EnumProcesses(processIds, processIds.Length * 4, out size))
                    throw new Win32Exception();
                if (size == processIds.Length * 4)
                {
                    processIds = new int[processIds.Length * 2];
                    continue;
                }
                break;
            }
            int[] ids = new int[size / 4];
            Array.Copy(processIds, 0, ids, 0, ids.Length);
            return ids;
        }

        public static ProcessModuleCollection GetModules(int processId)
        {
            return GetModules(processId, firstModuleOnly: false);
        }

        public static ProcessModule GetFirstModule(int processId)
        {
            ProcessModuleCollection modules = GetModules(processId, firstModuleOnly: true);
            return modules.Count == 0 ? null : modules[0];
        }

        private static void HandleError()
        {
            int lastError = Marshal.GetLastWin32Error();
            switch (lastError)
            {
                case Interop.Errors.ERROR_INVALID_HANDLE:
                case Interop.Errors.ERROR_PARTIAL_COPY:
                    // It's possible that another thread caused this module to become
                    // unloaded (e.g FreeLibrary was called on the module).  Ignore it and
                    // move on.
                    break;
                default:
                    throw new Win32Exception(lastError);
            }
        }

        public static int GetProcessIdFromHandle(SafeProcessHandle processHandle)
        {
            return Interop.Kernel32.GetProcessId(processHandle);
        }

        public static ProcessInfo[] GetProcessInfos(string machineName, bool isRemoteMachine)
        {
            PerformanceCounterLib library = null;
            try
            {
                library = PerformanceCounterLib.GetPerformanceCounterLib(machineName, new CultureInfo("en"));
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
                    throw e;
                }
            }
            // We don't want to call library.Close() here because that would cause us to unload all of the perflibs.
            // On the next call to GetProcessInfos, we'd have to load them all up again, which is SLOW!
        }

        private static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library)
        {
            ProcessInfo[] processInfos;

            int retryCount = 5;
            do
            {
                try
                {
                    byte[] dataPtr = library.GetPerformanceData(PerfCounterQueryString);
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
                        ProcessInfo processInfo = GetProcessInfo(in type, data.Slice(instancePos + instance.ByteLength), counters);
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
                                    if (instanceName.EndsWith(".", StringComparison.Ordinal)) instanceName = instanceName.Slice(0, 14);
                                    else if (instanceName.EndsWith(".e", StringComparison.Ordinal)) instanceName = instanceName.Slice(0, 13);
                                    else if (instanceName.EndsWith(".ex", StringComparison.Ordinal)) instanceName = instanceName.Slice(0, 12);
                                }
                                processInfo.ProcessName = instanceName.ToString();
                                processInfos.Add(processInfo.ProcessId, processInfo);
                            }
                        }
                    }
                    else if (type.ObjectNameTitleIndex == threadIndex)
                    {
                        ThreadInfo threadInfo = GetThreadInfo(in type, data.Slice(instancePos + instance.ByteLength), counters);
                        if (threadInfo._threadId != 0) threadInfos.Add(threadInfo);
                    }

                    instancePos += instance.ByteLength;

                    instancePos += MemoryMarshal.AsRef<PERF_COUNTER_BLOCK>(data.Slice(instancePos)).ByteLength;
                }

                typePos += type.TotalByteLength;
            }

            for (int i = 0; i < threadInfos.Count; i++)
            {
                ThreadInfo threadInfo = threadInfos[i];
                if (processInfos.TryGetValue(threadInfo._processId, out ProcessInfo processInfo))
                {
                    processInfo._threadInfoList.Add(threadInfo);
                }
            }

            ProcessInfo[] temp = new ProcessInfo[processInfos.Values.Count];
            processInfos.Values.CopyTo(temp, 0);
            return temp;
        }

        private static ThreadInfo GetThreadInfo(in PERF_OBJECT_TYPE type, ReadOnlySpan<byte> instanceData, PERF_COUNTER_DEFINITION[] counters)
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
                case 14: return ThreadWaitReason.EventPairHigh; ;
                case 15: return ThreadWaitReason.EventPairLow;
                case 16: return ThreadWaitReason.LpcReceive;
                case 17: return ThreadWaitReason.LpcReply;
                case 18: return ThreadWaitReason.VirtualMemory;
                case 19: return ThreadWaitReason.PageOut;
                default: return ThreadWaitReason.Unknown;
            }
        }

        private static ProcessInfo GetProcessInfo(in PERF_OBJECT_TYPE type, ReadOnlySpan<byte> instanceData, PERF_COUNTER_DEFINITION[] counters)
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
                return (long)MemoryMarshal.Read<int>(data);
        }

        enum ValueId
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
