// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

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
                using (SafeProcessHandle processHandle = Interop.Kernel32.OpenProcess(Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION, false, processId))
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
            string baseName;

            if (machineName.StartsWith("\\", StringComparison.Ordinal))
                baseName = machineName.Substring(2);
            else
                baseName = machineName;
            if (baseName.Equals(".")) return false;

            return !string.Equals(Interop.Kernel32.GetComputerName(), baseName, StringComparison.OrdinalIgnoreCase);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------

        static ProcessManager()
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

                Interop.Advapi32.TokenPrivileges tp = new Interop.Advapi32.TokenPrivileges();
                tp.Luid = luid;
                tp.Attributes = Interop.Advapi32.SEPrivileges.SE_PRIVILEGE_ENABLED;

                // AdjustTokenPrivileges can return true even if it didn't succeed (when ERROR_NOT_ALL_ASSIGNED is returned).
                Interop.Advapi32.AdjustTokenPrivileges(tokenHandle, false, tp, 0, IntPtr.Zero, IntPtr.Zero);
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
                    throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, processId.ToString(CultureInfo.CurrentCulture)));
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
                    throw new InvalidOperationException(SR.Format(SR.ThreadExited, threadId.ToString(CultureInfo.CurrentCulture)));
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

        private static readonly Dictionary<String, ValueId> s_valueIds = new Dictionary<string, ValueId>(19)
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

        static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library)
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

        static ProcessInfo[] GetProcessInfos(PerformanceCounterLib library, int processIndex, int threadIndex, byte[] data)
        {
#if FEATURE_TRACESWITCH
            Debug.WriteLineIf(Process._processTracing.TraceVerbose, "GetProcessInfos()");
#endif
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>();
            List<ThreadInfo> threadInfos = new List<ThreadInfo>();

            GCHandle dataHandle = new GCHandle();
            try
            {
                dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                IntPtr dataBlockPtr = dataHandle.AddrOfPinnedObject();
                Interop.Advapi32.PERF_DATA_BLOCK dataBlock = new Interop.Advapi32.PERF_DATA_BLOCK();
                Marshal.PtrToStructure(dataBlockPtr, dataBlock);
                IntPtr typePtr = (IntPtr)((long)dataBlockPtr + dataBlock.HeaderLength);
                Interop.Advapi32.PERF_INSTANCE_DEFINITION instance = new Interop.Advapi32.PERF_INSTANCE_DEFINITION();
                Interop.Advapi32.PERF_COUNTER_BLOCK counterBlock = new Interop.Advapi32.PERF_COUNTER_BLOCK();
                for (int i = 0; i < dataBlock.NumObjectTypes; i++)
                {
                    Interop.Advapi32.PERF_OBJECT_TYPE type = new Interop.Advapi32.PERF_OBJECT_TYPE();
                    Marshal.PtrToStructure(typePtr, type);
                    IntPtr instancePtr = (IntPtr)((long)typePtr + type.DefinitionLength);
                    IntPtr counterPtr = (IntPtr)((long)typePtr + type.HeaderLength);
                    List<Interop.Advapi32.PERF_COUNTER_DEFINITION> counterList = new List<Interop.Advapi32.PERF_COUNTER_DEFINITION>();

                    for (int j = 0; j < type.NumCounters; j++)
                    {
                        Interop.Advapi32.PERF_COUNTER_DEFINITION counter = new Interop.Advapi32.PERF_COUNTER_DEFINITION();
                        Marshal.PtrToStructure(counterPtr, counter);
                        string counterName = library.GetCounterName(counter.CounterNameTitleIndex);

                        if (type.ObjectNameTitleIndex == processIndex)
                            counter.CounterNameTitlePtr = (int)GetValueId(counterName);
                        else if (type.ObjectNameTitleIndex == threadIndex)
                            counter.CounterNameTitlePtr = (int)GetValueId(counterName);
                        counterList.Add(counter);
                        counterPtr = (IntPtr)((long)counterPtr + counter.ByteLength);
                    }

                    Interop.Advapi32.PERF_COUNTER_DEFINITION[] counters = counterList.ToArray();

                    for (int j = 0; j < type.NumInstances; j++)
                    {
                        Marshal.PtrToStructure(instancePtr, instance);
                        IntPtr namePtr = (IntPtr)((long)instancePtr + instance.NameOffset);
                        string instanceName = Marshal.PtrToStringUni(namePtr);
                        if (instanceName.Equals("_Total")) continue;
                        IntPtr counterBlockPtr = (IntPtr)((long)instancePtr + instance.ByteLength);
                        Marshal.PtrToStructure(counterBlockPtr, counterBlock);
                        if (type.ObjectNameTitleIndex == processIndex)
                        {
                            ProcessInfo processInfo = GetProcessInfo(type, (IntPtr)((long)instancePtr + instance.ByteLength), counters);
                            if (processInfo.ProcessId == 0 && string.Compare(instanceName, "Idle", StringComparison.OrdinalIgnoreCase) != 0)
                            {
                                // Sometimes we'll get a process structure that is not completely filled in.
                                // We can catch some of these by looking for non-"idle" processes that have id 0
                                // and ignoring those.
#if FEATURE_TRACESWITCH
                                Debug.WriteLineIf(Process._processTracing.TraceVerbose, "GetProcessInfos() - found a non-idle process with id 0; ignoring.");
#endif
                            }
                            else
                            {
                                if (processInfos.ContainsKey(processInfo.ProcessId))
                                {
                                    // We've found two entries in the perfcounters that claim to be the
                                    // same process.  We throw an exception.  Is this really going to be
                                    // helpful to the user?  Should we just ignore?
#if FEATURE_TRACESWITCH
                                    Debug.WriteLineIf(Process._processTracing.TraceVerbose, "GetProcessInfos() - found a duplicate process id");
#endif
                                }
                                else
                                {
                                    // the performance counters keep a 15 character prefix of the exe name, and then delete the ".exe",
                                    // if it's in the first 15.  The problem is that sometimes that will leave us with part of ".exe"
                                    // at the end.  If instanceName ends in ".", ".e", or ".ex" we remove it.
                                    string processName = instanceName;
                                    if (processName.Length == 15)
                                    {
                                        if (instanceName.EndsWith(".", StringComparison.Ordinal)) processName = instanceName.Substring(0, 14);
                                        else if (instanceName.EndsWith(".e", StringComparison.Ordinal)) processName = instanceName.Substring(0, 13);
                                        else if (instanceName.EndsWith(".ex", StringComparison.Ordinal)) processName = instanceName.Substring(0, 12);
                                    }
                                    processInfo.ProcessName = processName;
                                    processInfos.Add(processInfo.ProcessId, processInfo);
                                }
                            }
                        }
                        else if (type.ObjectNameTitleIndex == threadIndex)
                        {
                            ThreadInfo threadInfo = GetThreadInfo(type, (IntPtr)((long)instancePtr + instance.ByteLength), counters);
                            if (threadInfo._threadId != 0) threadInfos.Add(threadInfo);
                        }
                        instancePtr = (IntPtr)((long)instancePtr + instance.ByteLength + counterBlock.ByteLength);
                    }

                    typePtr = (IntPtr)((long)typePtr + type.TotalByteLength);
                }
            }
            finally
            {
                if (dataHandle.IsAllocated) dataHandle.Free();
            }

            for (int i = 0; i < threadInfos.Count; i++)
            {
                ThreadInfo threadInfo = (ThreadInfo)threadInfos[i];
                ProcessInfo processInfo;
                if (processInfos.TryGetValue(threadInfo._processId, out processInfo))
                {
                    processInfo._threadInfoList.Add(threadInfo);
                }
            }

            ProcessInfo[] temp = new ProcessInfo[processInfos.Values.Count];
            processInfos.Values.CopyTo(temp, 0);
            return temp;
        }

        static ThreadInfo GetThreadInfo(Interop.Advapi32.PERF_OBJECT_TYPE type, IntPtr instancePtr, Interop.Advapi32.PERF_COUNTER_DEFINITION[] counters)
        {
            ThreadInfo threadInfo = new ThreadInfo();
            for (int i = 0; i < counters.Length; i++)
            {
                Interop.Advapi32.PERF_COUNTER_DEFINITION counter = counters[i];
                long value = ReadCounterValue(counter.CounterType, (IntPtr)((long)instancePtr + counter.CounterOffset));
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

        static ProcessInfo GetProcessInfo(Interop.Advapi32.PERF_OBJECT_TYPE type, IntPtr instancePtr, Interop.Advapi32.PERF_COUNTER_DEFINITION[] counters)
        {
            ProcessInfo processInfo = new ProcessInfo();
            for (int i = 0; i < counters.Length; i++)
            {
                Interop.Advapi32.PERF_COUNTER_DEFINITION counter = counters[i];
                long value = ReadCounterValue(counter.CounterType, (IntPtr)((long)instancePtr + counter.CounterOffset));
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

        static ValueId GetValueId(string counterName)
        {
            if (counterName != null)
            {
                ValueId id;
                if (s_valueIds.TryGetValue(counterName, out id))
                    return id;
            }

            return ValueId.Unknown;
        }

        static long ReadCounterValue(int counterType, IntPtr dataPtr)
        {
            if ((counterType & Interop.Advapi32.PerfCounterOptions.NtPerfCounterSizeLarge) != 0)
                return Marshal.ReadInt64(dataPtr);
            else
                return (long)Marshal.ReadInt32(dataPtr);
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

    internal static partial class NtProcessInfoHelper
    {
        private static int GetNewBufferSize(int existingBufferSize, int requiredSize)
        {
            if (requiredSize == 0)
            {
                //
                // On some old OS like win2000, requiredSize will not be set if the buffer
                // passed to NtQuerySystemInformation is not enough.
                //
                int newSize = existingBufferSize * 2;
                if (newSize < existingBufferSize)
                {
                    // In reality, we should never overflow.
                    // Adding the code here just in case it happens.    
                    throw new OutOfMemoryException();
                }
                return newSize;
            }
            else
            {
                // allocating a few more kilo bytes just in case there are some new process
                // kicked in since new call to NtQuerySystemInformation
                int newSize = requiredSize + 1024 * 10;
                if (newSize < requiredSize)
                {
                    throw new OutOfMemoryException();
                }
                return newSize;
            }
        }

        // Use a smaller buffer size on debug to ensure we hit the retry path.
#if DEBUG
        private const int DefaultCachedBufferSize = 1024;
#else
        private const int DefaultCachedBufferSize = 128 * 1024;
#endif

        private static unsafe ProcessInfo[] GetProcessInfos(IntPtr dataPtr, Predicate<int> processIdFilter)
        {
            // Use a dictionary to avoid duplicate entries if any
            // 60 is a reasonable number for processes on a normal machine.
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>(60);

            long totalOffset = 0;

            while (true)
            {
                IntPtr currentPtr = (IntPtr)((long)dataPtr + totalOffset);
                ref SystemProcessInformation pi = ref *(SystemProcessInformation *)(currentPtr);

                // Process ID shouldn't overflow. OS API GetCurrentProcessID returns DWORD.
                var processInfoProcessId = pi.UniqueProcessId.ToInt32();
                if (processIdFilter == null || processIdFilter(processInfoProcessId))
                {
                    // get information for a process
                    ProcessInfo processInfo = new ProcessInfo();
                    processInfo.ProcessId = processInfoProcessId;
                    processInfo.SessionId = (int)pi.SessionId;
                    processInfo.PoolPagedBytes = (long)pi.QuotaPagedPoolUsage;
                    processInfo.PoolNonPagedBytes = (long)pi.QuotaNonPagedPoolUsage;
                    processInfo.VirtualBytes = (long)pi.VirtualSize;
                    processInfo.VirtualBytesPeak = (long)pi.PeakVirtualSize;
                    processInfo.WorkingSetPeak = (long)pi.PeakWorkingSetSize;
                    processInfo.WorkingSet = (long)pi.WorkingSetSize;
                    processInfo.PageFileBytesPeak = (long)pi.PeakPagefileUsage;
                    processInfo.PageFileBytes = (long)pi.PagefileUsage;
                    processInfo.PrivateBytes = (long)pi.PrivatePageCount;
                    processInfo.BasePriority = pi.BasePriority;
                    processInfo.HandleCount = (int)pi.HandleCount;

                    if (pi.ImageName.Buffer == IntPtr.Zero)
                    {
                        if (processInfo.ProcessId == NtProcessManager.SystemProcessID)
                        {
                            processInfo.ProcessName = "System";
                        }
                        else if (processInfo.ProcessId == NtProcessManager.IdleProcessID)
                        {
                            processInfo.ProcessName = "Idle";
                        }
                        else
                        {
                            // for normal process without name, using the process ID. 
                            processInfo.ProcessName = processInfo.ProcessId.ToString(CultureInfo.InvariantCulture);
                        }
                    }
                    else
                    {
                        string processName = GetProcessShortName(Marshal.PtrToStringUni(pi.ImageName.Buffer, pi.ImageName.Length / sizeof(char)));
                        processInfo.ProcessName = processName;
                    }

                    // get the threads for current process
                    processInfos[processInfo.ProcessId] = processInfo;

                    currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(pi));
                    int i = 0;
                    while (i < pi.NumberOfThreads)
                    {
                        ref SystemThreadInformation ti = ref *(SystemThreadInformation *)(currentPtr);
                        ThreadInfo threadInfo = new ThreadInfo();

                        threadInfo._processId = (int)ti.ClientId.UniqueProcess;
                        threadInfo._threadId = (ulong)ti.ClientId.UniqueThread;
                        threadInfo._basePriority = ti.BasePriority;
                        threadInfo._currentPriority = ti.Priority;
                        threadInfo._startAddress = ti.StartAddress;
                        threadInfo._threadState = (ThreadState)ti.ThreadState;
                        threadInfo._threadWaitReason = NtProcessManager.GetThreadWaitReason((int)ti.WaitReason);

                        processInfo._threadInfoList.Add(threadInfo);
                        currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(ti));
                        i++;
                    }
                }

                if (pi.NextEntryOffset == 0)
                {
                    break;
                }
                totalOffset += pi.NextEntryOffset;
            }

            ProcessInfo[] temp = new ProcessInfo[processInfos.Values.Count];
            processInfos.Values.CopyTo(temp, 0);
            return temp;
        }

        // This function generates the short form of process name. 
        //
        // This is from GetProcessShortName in NT code base. 
        // Check base\screg\winreg\perfdlls\process\perfsprc.c for details.
        internal static string GetProcessShortName(String name)
        {
            if (String.IsNullOrEmpty(name))
            {
#if FEATURE_TRACESWITCH
                Debug.WriteLineIf(Process._processTracing.TraceVerbose, "GetProcessInfos() - unexpected blank ProcessName");
#endif
                return String.Empty;
            }

            int slash = -1;
            int period = -1;

            for (int i = 0; i < name.Length; i++)
            {
                if (name[i] == '\\')
                    slash = i;
                else if (name[i] == '.')
                    period = i;
            }

            if (period == -1)
                period = name.Length - 1; // set to end of string
            else
            {
                // if a period was found, then see if the extension is
                // .EXE, if so drop it, if not, then use end of string
                // (i.e. include extension in name)
                String extension = name.Substring(period);

                if (String.Equals(".exe", extension, StringComparison.OrdinalIgnoreCase))
                    period--;                 // point to character before period
                else
                    period = name.Length - 1; // set to end of string
            }

            if (slash == -1)
                slash = 0;     // set to start of string
            else
                slash++;       // point to character next to slash

            // copy characters between period (or end of string) and
            // slash (or start of string) to make image name
            return name.Substring(slash, period - slash + 1);
        }

        // native struct defined in ntexapi.h
        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SystemProcessInformation
        {
            internal uint NextEntryOffset;
            internal uint NumberOfThreads;
            private fixed byte Reserved1[48];
            internal Interop.UNICODE_STRING ImageName;
            internal int BasePriority;
            internal IntPtr UniqueProcessId;
            private UIntPtr Reserved2;
            internal uint HandleCount;
            internal uint SessionId;
            private UIntPtr Reserved3;
            internal UIntPtr PeakVirtualSize;  // SIZE_T
            internal UIntPtr VirtualSize;
            private uint Reserved4;
            internal UIntPtr PeakWorkingSetSize;  // SIZE_T
            internal UIntPtr WorkingSetSize;  // SIZE_T
            private UIntPtr Reserved5;
            internal UIntPtr QuotaPagedPoolUsage;  // SIZE_T
            private UIntPtr Reserved6;
            internal UIntPtr QuotaNonPagedPoolUsage;  // SIZE_T
            internal UIntPtr PagefileUsage;  // SIZE_T
            internal UIntPtr PeakPagefileUsage;  // SIZE_T
            internal UIntPtr PrivatePageCount;  // SIZE_T
            private fixed long Reserved7[6];
        }

        [StructLayout(LayoutKind.Sequential)]
        internal unsafe struct SystemThreadInformation
        {
            private fixed long Reserved1[3];
            private uint Reserved2;
            internal IntPtr StartAddress;
            internal CLIENT_ID ClientId;
            internal int Priority;
            internal int BasePriority;
            private uint Reserved3;
            internal uint ThreadState;
            internal uint WaitReason;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct CLIENT_ID
        {
            internal IntPtr UniqueProcess;
            internal IntPtr UniqueThread;
        }
    }
}
