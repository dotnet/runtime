// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

using static Interop.Advapi32;

using SYSTEM_PROCESS_INFORMATION = Interop.NtDll.SYSTEM_PROCESS_INFORMATION;
using SYSTEM_THREAD_INFORMATION = Interop.NtDll.SYSTEM_THREAD_INFORMATION;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
        // Allows PerformanceCounterLib (and its dependencies) to be trimmed when remote machine
        // support is not used. s_getRemoteProcessInfos is only assigned in HandleRemoteMachineSupport,
        // which is only called from public APIs that accept a remote machine name.
        private delegate void GetRemoteProcessInfosDelegate(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter, string machineName);
        private static GetRemoteProcessInfosDelegate? s_getRemoteProcessInfos;

        /// <summary>
        /// Initializes remote machine support if necessary. This method should be called in all public
        /// entrypoints with machineName argument.
        /// </summary>
        /// <param name="machineName">The target machine name.</param>
        /// <returns>true if the machine is remote; otherwise, false.</returns>
        internal static bool HandleRemoteMachineSupport(string machineName)
        {
            ArgumentException.ThrowIfNullOrEmpty(machineName);
            if (IsRemoteMachine(machineName))
            {
                s_getRemoteProcessInfos ??= NtProcessManager.GetProcessInfos;
                return true;
            }
            return false;
        }

        /// <summary>Gets process infos for each process on the local machine.</summary>
        /// <param name="builder">The builder to add found process infos to.</param>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        internal static void GetProcessInfos(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter)
        {
            Dictionary<int, ProcessInfo> processInfos = NtProcessInfoHelper.GetProcessInfos(processNameFilter: processNameFilter);
            builder = new ArrayBuilder<ProcessInfo>(processInfos.Count);
            foreach (KeyValuePair<int, ProcessInfo> entry in processInfos)
            {
                builder.Add(entry.Value);
            }
        }

        /// <summary>Gets whether the process with the specified ID is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId)
        {
            // Performance optimization: First try to OpenProcess by id.
            // Attempt to open handle for Idle process (processId == 0) fails with ERROR_INVALID_PARAMETER.
            if (processId != 0)
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

            return Array.IndexOf(GetProcessIds(), processId) >= 0;
        }

        /// <summary>Gets whether the process with the specified ID on the specified machine is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <param name="isRemoteMachine">Whether the machine is remote; avoids a redundant <see cref="IsRemoteMachine"/> call.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId, string machineName, bool isRemoteMachine)
        {
            if (!isRemoteMachine)
            {
                return IsProcessRunning(processId);
            }

            return Array.IndexOf(GetProcessIds(machineName, isRemoteMachine), processId) >= 0;
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="builder">The builder to add found process infos to.</param>
        /// <param name="processNameFilter">Optional process name to use as an inclusion filter.</param>
        /// <param name="machineName">The target machine.</param>
        /// <param name="isRemoteMachine">Whether the machine is remote; avoids a redundant <see cref="IsRemoteMachine"/> call.</param>
        public static void GetProcessInfos(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter, string machineName, bool isRemoteMachine)
        {
            if (!isRemoteMachine)
            {
                GetProcessInfos(ref builder, processNameFilter);
                return;
            }

            s_getRemoteProcessInfos!(ref builder, processNameFilter, machineName);
        }

        /// <summary>Gets the ProcessInfo for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <param name="isRemoteMachine">Whether the machine is remote; avoids a redundant <see cref="IsRemoteMachine"/> call.</param>
        /// <returns>The ProcessInfo for the process if it could be found; otherwise, null.</returns>
        public static ProcessInfo? GetProcessInfo(int processId, string machineName, bool isRemoteMachine)
        {
            if (isRemoteMachine)
            {
                // remote case: we take the hit of looping through all results
                ArrayBuilder<ProcessInfo> builder = default;
                s_getRemoteProcessInfos!(ref builder, processNameFilter: null, machineName);
                for (int i = 0; i < builder.Count; i++)
                {
                    if (builder[i].ProcessId == processId)
                        return builder[i];
                }
            }
            else
            {
                // local case: do not use performance counter and also attempt to get the matching (by pid) process only
                Dictionary<int, ProcessInfo> processInfos = NtProcessInfoHelper.GetProcessInfos(processId);
                if (processInfos.TryGetValue(processId, out ProcessInfo? processInfo))
                    return processInfo;
            }

            return null;
        }

        internal static string? GetProcessName(int processId, string machineName, bool isRemoteMachine, ref ProcessInfo? processInfo)
        {
            if (processInfo is not null)
            {
                return processInfo.ProcessName;
            }

            if (!isRemoteMachine)
            {
                string? processName = Interop.Kernel32.GetProcessName((uint)processId);
                if (processName is not null)
                {
                    ReadOnlySpan<char> newName = NtProcessInfoHelper.GetProcessShortName(processName);
                    return newName.SequenceEqual(processName) ?
                        processName :
                        newName.ToString();
                }
            }

            processInfo = GetProcessInfo(processId, machineName, isRemoteMachine);
            return processInfo?.ProcessName;
        }

        /// <summary>Gets the IDs of all processes on the specified machine.</summary>
        /// <param name="machineName">The machine to examine.</param>
        /// <param name="isRemoteMachine">Whether the machine is remote; avoids a redundant <see cref="IsRemoteMachine"/> call.</param>
        /// <returns>An array of process IDs from the specified machine.</returns>
        public static int[] GetProcessIds(string machineName, bool isRemoteMachine)
        {
            if (!isRemoteMachine)
            {
                return GetProcessIds();
            }

            ArrayBuilder<ProcessInfo> builder = default;
            s_getRemoteProcessInfos!(ref builder, processNameFilter: null, machineName);

            int[] ids = new int[builder.Count];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = builder[i].ProcessId;
            }

            return ids;
        }

        /// <summary>Gets the IDs of all processes on the current machine.</summary>
        public static int[] GetProcessIds()
        {
            return NtProcessManager.GetProcessIds();
        }

        /// <summary>Gets an array of module infos for the specified process.</summary>
        /// <param name="processId">The ID of the process whose modules should be enumerated.</param>
        /// <returns>The array of modules.</returns>
        public static ProcessModuleCollection GetModules(int processId)
        {
            return NtProcessManager.GetModules(processId);
        }

        internal static bool IsRemoteMachine(string machineName)
        {
            ReadOnlySpan<char> baseName = machineName.AsSpan(machineName.StartsWith('\\') ? 2 : 0);
            return
                baseName is not "." &&
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
                tokenHandle?.Dispose();
            }
        }

        public static SafeProcessHandle OpenProcess(int processId, int access, bool throwOnError)
        {
            SafeProcessHandle processHandle = Interop.Kernel32.OpenProcess(access, false, processId);
            int result = Marshal.GetLastWin32Error();
            if (!processHandle.IsInvalid)
            {
                return processHandle;
            }

            processHandle.Dispose();

            if (!throwOnError)
            {
                return SafeProcessHandle.InvalidHandle;
            }

            if (processId == 0)
            {
                throw new Win32Exception(Interop.Errors.ERROR_ACCESS_DENIED);
            }

            if (!IsProcessRunning(processId))
            {
                throw new InvalidOperationException(SR.Format(SR.ProcessHasExited, processId.ToString()));
            }

            throw new Win32Exception(result);
        }

        public static SafeThreadHandle OpenThread(int threadId, int access)
        {
            SafeThreadHandle threadHandle = Interop.Kernel32.OpenThread(access, false, threadId);
            int result = Marshal.GetLastWin32Error();
            if (threadHandle.IsInvalid)
            {
                threadHandle.Dispose();
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

        public static IntPtr GetMainWindowHandle(int processId)
        {
            return MainWindowFinder.FindMainWindow(processId);
        }
    }

    internal struct MainWindowFinder
    {
        private const int GW_OWNER = 4;
        private IntPtr _bestHandle;
        private int _processId;

        public static unsafe IntPtr FindMainWindow(int processId)
        {
            MainWindowFinder instance;

            instance._bestHandle = IntPtr.Zero;
            instance._processId = processId;

            Interop.User32.EnumWindows(&EnumWindowsCallback, (IntPtr)(void*)&instance);

            return instance._bestHandle;
        }

        private static bool IsMainWindow(IntPtr handle)
        {
            return (Interop.User32.GetWindow(handle, GW_OWNER) == IntPtr.Zero) && Interop.User32.IsWindowVisible(handle) != Interop.BOOL.FALSE;
        }

        [UnmanagedCallersOnly]
        private static unsafe Interop.BOOL EnumWindowsCallback(IntPtr handle, IntPtr extraParameter)
        {
            MainWindowFinder* instance = (MainWindowFinder*)extraParameter;

            int processId = 0; // Avoid uninitialized variable if the window got closed in the meantime
            Interop.User32.GetWindowThreadProcessId(handle, &processId);

            if ((processId == instance->_processId) && IsMainWindow(handle))
            {
                instance->_bestHandle = handle;
                return Interop.BOOL.FALSE;
            }
            return Interop.BOOL.TRUE;
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

        public static int[] GetProcessIds()
        {
            int[] processIds = ArrayPool<int>.Shared.Rent(256);

            int needed;
            while (true)
            {
                int size = processIds.Length * sizeof(int);
                if (!Interop.Kernel32.EnumProcesses(processIds, size, out needed))
                {
                    throw new Win32Exception();
                }

                if (needed == size)
                {
                    int newLength = processIds.Length * 2;
                    ArrayPool<int>.Shared.Return(processIds);
                    processIds = ArrayPool<int>.Shared.Rent(newLength);
                    continue;
                }

                break;
            }

            int[] ids = new int[needed / sizeof(int)];
            Array.Copy(processIds, ids, ids.Length);

            ArrayPool<int>.Shared.Return(processIds);
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

        public static void GetProcessInfos(ref ArrayBuilder<ProcessInfo> builder, string? processNameFilter, string machineName)
        {
            PerformanceCounterLib library;
            try
            {
                // We don't want to call library.Close() here because that would cause us to unload all of the perflibs.
                // On the next call to GetProcessInfos, we'd have to load them all up again, which is SLOW!
                library = PerformanceCounterLib.GetPerformanceCounterLib(machineName, new CultureInfo("en"));
            }
            catch (Exception e)
            {
                throw new InvalidOperationException(SR.CouldntConnectToRemoteMachine, e);
            }

            int retryCount = 5;
            Dictionary<int, ProcessInfo> processInfos;
            do
            {
                try
                {
                    byte[]? dataPtr = library.GetPerformanceData(PerfCounterQueryString);
                    processInfos = GetProcessInfos(library, dataPtr);
                }
                catch (Exception e)
                {
                    throw new InvalidOperationException(SR.CouldntGetProcessInfos, e);
                }

                --retryCount;
            }
            while (processInfos.Count == 0 && retryCount != 0);

            if (processInfos.Count == 0)
                throw new InvalidOperationException(SR.ProcessDisabled);

            foreach (KeyValuePair<int, ProcessInfo> entry in processInfos)
            {
                if (processNameFilter is null || processNameFilter.Equals(entry.Value.ProcessName, StringComparison.OrdinalIgnoreCase))
                    builder.Add(entry.Value);
            }
        }

        private static Dictionary<int, ProcessInfo> GetProcessInfos(PerformanceCounterLib library, ReadOnlySpan<byte> data)
        {
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>();
            List<ThreadInfo> threadInfos = new List<ThreadInfo>();

            ref readonly PERF_DATA_BLOCK dataBlock = ref MemoryMarshal.AsRef<PERF_DATA_BLOCK>(data);
            dataBlock.Validate(data.Length);

            int typePos = dataBlock.HeaderLength;
            ReadOnlySpan<byte> dataSpan;

            for (int i = 0; i < dataBlock.NumObjectTypes; i++)
            {
                dataSpan = data.Slice(typePos);
                ref readonly PERF_OBJECT_TYPE type = ref MemoryMarshal.AsRef<PERF_OBJECT_TYPE>(dataSpan);
                type.Validate(dataSpan.Length);

                PERF_COUNTER_DEFINITION[] counters = new PERF_COUNTER_DEFINITION[type.NumCounters];

                int counterPos = typePos + type.HeaderLength;
                for (int j = 0; j < type.NumCounters; j++)
                {
                    dataSpan = data.Slice(counterPos);
                    ref readonly PERF_COUNTER_DEFINITION counter = ref MemoryMarshal.AsRef<PERF_COUNTER_DEFINITION>(dataSpan);
                    counter.Validate(dataSpan.Length);

                    string counterName = library.GetCounterName(counter.CounterNameTitleIndex);

                    counters[j] = counter;
                    if (type.ObjectNameTitleIndex == ProcessPerfCounterId)
                        counters[j].CounterNameTitlePtr = (int)GetValueId(counterName);
                    else if (type.ObjectNameTitleIndex == ThreadPerfCounterId)
                        counters[j].CounterNameTitlePtr = (int)GetValueId(counterName);

                    counterPos += counter.ByteLength;
                }

                int instancePos = typePos + type.DefinitionLength;
                for (int j = 0; j < type.NumInstances; j++)
                {
                    dataSpan = data.Slice(instancePos);
                    ref readonly PERF_INSTANCE_DEFINITION instance = ref MemoryMarshal.AsRef<PERF_INSTANCE_DEFINITION>(dataSpan);
                    instance.Validate(dataSpan.Length);

                    ReadOnlySpan<char> instanceName = PERF_INSTANCE_DEFINITION.GetName(in instance, data.Slice(instancePos));

                    if (instanceName is "_Total")
                    {
                        // continue
                    }
                    else if (type.ObjectNameTitleIndex == ProcessPerfCounterId)
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
                            if (!processInfos.TryAdd(processInfo.ProcessId, processInfo))
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
                                    if (instanceName[^1] == '.')
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
                            }
                        }
                    }
                    else if (type.ObjectNameTitleIndex == ThreadPerfCounterId)
                    {
                        ThreadInfo threadInfo = GetThreadInfo(data.Slice(instancePos + instance.ByteLength), counters);
                        if (threadInfo._threadId != 0)
                        {
                            threadInfos.Add(threadInfo);
                        }
                    }

                    instancePos += instance.ByteLength;

                    dataSpan = data.Slice(instancePos);
                    ref readonly PERF_COUNTER_BLOCK perfCounterBlock = ref MemoryMarshal.AsRef<PERF_COUNTER_BLOCK>(dataSpan);
                    perfCounterBlock.Validate(dataSpan.Length);

                    instancePos += perfCounterBlock.ByteLength;
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

            return processInfos;
        }

        private static unsafe ThreadInfo GetThreadInfo(ReadOnlySpan<byte> instanceData, PERF_COUNTER_DEFINITION[] counters)
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
                        threadInfo._startAddress = (void*)value;
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
                return BitConverter.ToInt64(data);
            else
                return BitConverter.ToInt32(data);
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

        private static ProcessModuleCollection GetModules(int processId, bool firstModuleOnly)
        {
            // preserving Everett behavior.
            if (processId == SystemProcessID || processId == IdleProcessID)
            {
                // system process and idle process doesn't have any modules
                throw new Win32Exception(HResults.E_FAIL, SR.EnumProcessModuleFailed);
            }

            SafeProcessHandle processHandle = SafeProcessHandle.InvalidHandle;
            try
            {
                processHandle = ProcessManager.OpenProcess(processId, Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION | Interop.Advapi32.ProcessOptions.PROCESS_VM_READ, true);

                bool succeeded = Interop.Kernel32.EnumProcessModulesEx(processHandle, null, 0, out int needed, Interop.Kernel32.LIST_MODULES_ALL);

                // The API we need to use to enumerate process modules differs on two factors:
                //   1) If our process is running in WOW64.
                //   2) The bitness of the process we wish to introspect.
                //
                // If we are not running in WOW64 or we ARE in WOW64 but want to inspect a 32 bit process
                // we can call psapi!EnumProcessModules.
                //
                // If we are running in WOW64 and we want to inspect the modules of a 64 bit process then
                // psapi!EnumProcessModules will return false with ERROR_PARTIAL_COPY (299).  In this case we can't
                // do the enumeration at all.  So we'll detect this case and bail out.
                if (!succeeded)
                {
                    if (!Interop.Kernel32.IsWow64Process(Interop.Kernel32.GetCurrentProcess(), out bool sourceProcessIsWow64))
                    {
                        throw new Win32Exception();
                    }

                    if (!Interop.Kernel32.IsWow64Process(processHandle, out bool targetProcessIsWow64))
                    {
                        throw new Win32Exception();
                    }

                    if (sourceProcessIsWow64 && !targetProcessIsWow64)
                    {
                        // Wow64 isn't going to allow this to happen, the best we can do is give a descriptive error to the user.
                        throw new Win32Exception(Interop.Errors.ERROR_PARTIAL_COPY, SR.EnumProcessModuleFailedDueToWow);
                    }

                    EnumProcessModulesUntilSuccess(processHandle, null, 0, out needed, Interop.Kernel32.LIST_MODULES_ALL);
                }

                int modulesCount = needed / IntPtr.Size;
                IntPtr[] moduleHandles = new IntPtr[modulesCount];
                while (true)
                {
                    int size = needed;
                    EnumProcessModulesUntilSuccess(processHandle, moduleHandles, size, out needed, Interop.Kernel32.LIST_MODULES_ALL);
                    if (size == needed)
                    {
                        break;
                    }

                    if (needed > size && needed / IntPtr.Size > modulesCount)
                    {
                        modulesCount = needed / IntPtr.Size;
                        moduleHandles = new IntPtr[modulesCount];
                    }
                }

                var modules = new ProcessModuleCollection(firstModuleOnly ? 1 : modulesCount);

                const int StartLength =
#if DEBUG
                    1; // in debug, validate ArrayPool growth
#else
                    Interop.Kernel32.MAX_PATH;
#endif
                char[]? chars = ArrayPool<char>.Shared.Rent(StartLength);
                try
                {
                    for (int i = 0; i < modulesCount; i++)
                    {
                        if (i > 0)
                        {
                            // If the user is only interested in the main module, break now.
                            // This avoid some waste of time. In addition, if the application unloads a DLL
                            // we will not get an exception.
                            if (firstModuleOnly)
                            {
                                break;
                            }
                        }

                        IntPtr moduleHandle = moduleHandles[i];
                        Interop.Kernel32.NtModuleInfo ntModuleInfo;
                        if (!Interop.Kernel32.GetModuleInformation(processHandle, moduleHandle, out ntModuleInfo))
                        {
                            HandleLastWin32Error();
                            continue;
                        }

                        int length = 0;
                        while ((length = Interop.Kernel32.GetModuleBaseName(processHandle, moduleHandle, chars, chars.Length)) == chars.Length)
                        {
                            char[] toReturn = chars;
                            chars = ArrayPool<char>.Shared.Rent(length * 2);
                            ArrayPool<char>.Shared.Return(toReturn);
                        }

                        if (length == 0)
                        {
                            HandleLastWin32Error();
                            continue;
                        }

                        string moduleName = new string(chars, 0, length);

                        while ((length = Interop.Kernel32.GetModuleFileNameEx(processHandle, moduleHandle, chars, chars.Length)) == chars.Length)
                        {
                            char[] toReturn = chars;
                            chars = ArrayPool<char>.Shared.Rent(length * 2);
                            ArrayPool<char>.Shared.Return(toReturn);
                        }

                        if (length == 0)
                        {
                            HandleLastWin32Error();
                            continue;
                        }

                        const string NtPathPrefix = @"\\?\";
                        ReadOnlySpan<char> charsSpan = chars.AsSpan(0, length);
                        if (charsSpan.StartsWith(NtPathPrefix))
                        {
                            charsSpan = charsSpan.Slice(NtPathPrefix.Length);
                        }

                        modules.Add(new ProcessModule(charsSpan.ToString(), moduleName)
                        {
                            ModuleMemorySize = ntModuleInfo.SizeOfImage,
                            EntryPointAddress = ntModuleInfo.EntryPoint,
                            BaseAddress = ntModuleInfo.BaseOfDll,
                        });
                    }
                }
                finally
                {
                    ArrayPool<char>.Shared.Return(chars);
                }

                return modules;
            }
            finally
            {
                if (!processHandle.IsInvalid)
                {
                    processHandle.Dispose();
                }
            }
        }

        private static void EnumProcessModulesUntilSuccess(SafeProcessHandle processHandle, IntPtr[]? modules, int size, out int needed, int filterFlag)
        {
            // When called on a running process, EnumProcessModules may fail with ERROR_PARTIAL_COPY
            // if the target process is not yet initialized or if the module list changes during the function call.
            // We just try to avoid the race by retrying 50 (an arbitrary number) times.
            int i = 0;
            while (true)
            {
                if (Interop.Kernel32.EnumProcessModulesEx(processHandle, modules, size, out needed, filterFlag))
                {
                    return;
                }

                if (i++ > 50)
                {
                    throw new Win32Exception();
                }

                Thread.Sleep(1);
            }
        }

        private static void HandleLastWin32Error()
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
    }

    internal static class NtProcessInfoHelper
    {
        // Use a smaller buffer size on debug to ensure we hit the retry path.
        private const uint DefaultCachedBufferSize = 1024 *
#if DEBUG
            8;
#else
            1024;
#endif
        private static uint MostRecentSize = DefaultCachedBufferSize;

        /// <summary>Gets <see cref="ProcessInfo"/> objects for each process on the local system.</summary>
        /// <param name="processIdFilter">Optional filter used to filter processes down to only those with the specified id.</param>
        /// <param name="processNameFilter">Optional filter used to filter processes down to only those with the specified name.</param>
        /// <remarks>All specified non-null filters are applied.</remarks>
        internal static unsafe Dictionary<int, ProcessInfo> GetProcessInfos(int? processIdFilter = null, string? processNameFilter = null)
        {
            // Start with the default buffer size.
            uint bufferSize = MostRecentSize;

            while (true)
            {
                void* bufferPtr = NativeMemory.Alloc(bufferSize); // some platforms require the buffer to be 64-bit aligned and NativeMemory.Alloc guarantees sufficient alignment.

                try
                {
                    uint actualSize = 0;
                    uint status = Interop.NtDll.NtQuerySystemInformation(
                        Interop.NtDll.SystemProcessInformation,
                        bufferPtr,
                        bufferSize,
                        &actualSize);

                    if (status != Interop.NtDll.STATUS_INFO_LENGTH_MISMATCH)
                    {
                        // see definition of NT_SUCCESS(Status) in SDK
                        if ((int)status < 0)
                        {
                            throw new InvalidOperationException(SR.CouldntGetProcessInfos, new Win32Exception((int)status));
                        }

                        Debug.Assert(actualSize > 0 && actualSize <= bufferSize, $"Actual size reported by NtQuerySystemInformation was {actualSize} for a buffer of size={bufferSize}.");
                        MostRecentSize = GetEstimatedBufferSize(actualSize);
                        // Parse the data block to get process information
                        return GetProcessInfos(new ReadOnlySpan<byte>(bufferPtr, (int)actualSize), processIdFilter, processNameFilter);
                    }

                    Debug.Assert(actualSize > bufferSize, $"Actual size reported by NtQuerySystemInformation was {actualSize} for a buffer of size={bufferSize}.");
                    bufferSize = GetEstimatedBufferSize(actualSize);
                }
                finally
                {
                    NativeMemory.Free(bufferPtr);
                }
            }
        }

        // allocating a few more kilo bytes just in case there are some new process
        // kicked in since new call to NtQuerySystemInformation
        private static uint GetEstimatedBufferSize(uint actualSize) => actualSize + 1024 * 10;

        private static unsafe Dictionary<int, ProcessInfo> GetProcessInfos(ReadOnlySpan<byte> data, int? processIdFilter, string? processNameFilter)
        {
            // Use a dictionary to avoid duplicate entries if any
            // 60 is a reasonable number for processes on a normal machine.
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>(60);

            int processInformationOffset = 0;

            while (true)
            {
                ref readonly SYSTEM_PROCESS_INFORMATION pi = ref MemoryMarshal.AsRef<SYSTEM_PROCESS_INFORMATION>(data.Slice(processInformationOffset));

                // Process ID shouldn't overflow. OS API GetCurrentProcessID returns DWORD.
                int processId = pi.UniqueProcessId.ToInt32();
                if (processIdFilter is null || processIdFilter.GetValueOrDefault() == processId)
                {
                    string? processName = null;
                    ReadOnlySpan<char> processNameSpan =
                        pi.ImageName.Buffer != IntPtr.Zero ? GetProcessShortName(new ReadOnlySpan<char>(pi.ImageName.Buffer.ToPointer(), pi.ImageName.Length / sizeof(char))) :
                        (processName =
                            processId == NtProcessManager.SystemProcessID ? "System" :
                            processId == NtProcessManager.IdleProcessID ? "Idle" :
                            processId.ToString(CultureInfo.InvariantCulture)); // use the process ID for a normal process without a name

                    if (processNameFilter is null || processNameSpan.Equals(processNameFilter, StringComparison.OrdinalIgnoreCase))
                    {
                        processName ??= processNameSpan.ToString();

                        // get information for a process
                        ProcessInfo processInfo = new ProcessInfo((int)pi.NumberOfThreads)
                        {
                            ProcessName = processName,
                            ProcessId = processId,
                            SessionId = (int)pi.SessionId,
                            PoolPagedBytes = (long)pi.QuotaPagedPoolUsage,
                            PoolNonPagedBytes = (long)pi.QuotaNonPagedPoolUsage,
                            VirtualBytes = (long)pi.VirtualSize,
                            VirtualBytesPeak = (long)pi.PeakVirtualSize,
                            WorkingSetPeak = (long)pi.PeakWorkingSetSize,
                            WorkingSet = (long)pi.WorkingSetSize,
                            PageFileBytesPeak = (long)pi.PeakPagefileUsage,
                            PageFileBytes = (long)pi.PagefileUsage,
                            PrivateBytes = (long)pi.PrivatePageCount,
                            BasePriority = pi.BasePriority,
                            HandleCount = (int)pi.HandleCount,
                        };

                        processInfos[processInfo.ProcessId] = processInfo;

                        // get the threads for current process
                        int threadInformationOffset = processInformationOffset + sizeof(SYSTEM_PROCESS_INFORMATION);
                        for (int i = 0; i < pi.NumberOfThreads; i++)
                        {
                            ref readonly SYSTEM_THREAD_INFORMATION ti = ref MemoryMarshal.AsRef<SYSTEM_THREAD_INFORMATION>(data.Slice(threadInformationOffset));

                            ThreadInfo threadInfo = new ThreadInfo
                            {
                                _processId = (int)ti.ClientId.UniqueProcess,
                                _threadId = (ulong)ti.ClientId.UniqueThread,
                                _basePriority = ti.BasePriority,
                                _currentPriority = ti.Priority,
                                _startAddress = ti.StartAddress,
                                _threadState = (ThreadState)ti.ThreadState,
                                _threadWaitReason = NtProcessManager.GetThreadWaitReason((int)ti.WaitReason),
                            };

                            processInfo._threadInfoList.Add(threadInfo);

                            threadInformationOffset += sizeof(SYSTEM_THREAD_INFORMATION);
                        }
                    }
                }

                if (pi.NextEntryOffset == 0)
                {
                    break;
                }
                processInformationOffset += (int)pi.NextEntryOffset;
            }

            return processInfos;
        }

        // This function generates the short form of process name.
        //
        // This is from GetProcessShortName in NT code base.
        // Check base\screg\winreg\perfdlls\process\perfsprc.c for details.
        internal static ReadOnlySpan<char> GetProcessShortName(ReadOnlySpan<char> name)
        {
            // Trim off everything up to and including the last slash, if there is one.
            // If there isn't, LastIndexOf will return -1 and this will end up as a nop.
            name = name.Slice(name.LastIndexOf('\\') + 1);

            // If the name ends with the ".exe" extension, then drop it, otherwise include
            // it in the name.
            if (name.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
            {
                name = name.Slice(0, name.Length - 4);
            }

            return name;
        }
    }
}
