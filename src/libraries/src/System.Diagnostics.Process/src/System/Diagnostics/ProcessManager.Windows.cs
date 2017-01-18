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
            return IsProcessRunning(processId, GetProcessIds());
        }

        /// <summary>Gets whether the process with the specified ID on the specified machine is currently running.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the process is running; otherwise, false.</returns>
        public static bool IsProcessRunning(int processId, string machineName)
        {
            return IsProcessRunning(processId, GetProcessIds(machineName));
        }

        /// <summary>Gets the ProcessInfo for the specified process ID on the specified machine.</summary>
        /// <param name="processId">The process ID.</param>
        /// <param name="machineName">The machine name.</param>
        /// <returns>The ProcessInfo for the process if it could be found; otherwise, null.</returns>
        public static ProcessInfo GetProcessInfo(int processId, string machineName)
        {
            ProcessInfo[] processInfos = ProcessManager.GetProcessInfos(machineName);
            foreach (ProcessInfo processInfo in processInfos)
            {
                if (processInfo.ProcessId == processId)
                {
                    return processInfo;
                }
            }
            return null;
        }

        /// <summary>Gets process infos for each process on the specified machine.</summary>
        /// <param name="machineName">The target machine.</param>
        /// <returns>An array of process infos, one per found process.</returns>
        public static ProcessInfo[] GetProcessInfos(string machineName)
        {
            return IsRemoteMachine(machineName) ?
                NtProcessManager.GetProcessInfos(machineName, true) :
                NtProcessInfoHelper.GetProcessInfos(); // Do not use performance counter for local machine
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

        /// <summary>Gets whether the named machine is remote or local.</summary>
        /// <param name="machineName">The machine name.</param>
        /// <returns>true if the machine is remote; false if it's local.</returns>
        public static bool IsRemoteMachine(string machineName)
        {
            if (machineName == null)
                throw new ArgumentNullException(nameof(machineName));

            if (machineName.Length == 0)
                throw new ArgumentException(SR.Format(SR.InvalidParameter, nameof(machineName), machineName));

            string baseName;

            if (machineName.StartsWith("\\", StringComparison.Ordinal))
                baseName = machineName.Substring(2);
            else
                baseName = machineName;
            if (baseName.Equals(".")) return false;

            if (String.Compare(Interop.Kernel32.GetComputerName(), baseName, StringComparison.OrdinalIgnoreCase) == 0) return false;
            return true;
        }

        public static IntPtr GetMainWindowHandle(int processId) 
        {
            MainWindowFinder finder = new MainWindowFinder();
            return finder.FindMainWindow(processId);
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

        private static bool IsProcessRunning(int processId, int[] processIds)
        {
            return Array.IndexOf(processIds, processId) >= 0;
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
            if (!IsProcessRunning(processId))
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
    internal static class NtProcessManager
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

        private static ProcessModuleCollection GetModules(int processId, bool firstModuleOnly)
        {
            // preserving Everett behavior.    
            if (processId == SystemProcessID || processId == IdleProcessID)
            {
                // system process and idle process doesn't have any modules 
                throw new Win32Exception(Interop.Errors.EFail, SR.EnumProcessModuleFailed);
            }

            SafeProcessHandle processHandle = SafeProcessHandle.InvalidHandle;
            try
            {
                processHandle = ProcessManager.OpenProcess(processId, Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION | Interop.Advapi32.ProcessOptions.PROCESS_VM_READ, true);

                IntPtr[] moduleHandles = new IntPtr[64];
                GCHandle moduleHandlesArrayHandle = new GCHandle();
                int moduleCount = 0;
                for (; ; )
                {
                    bool enumResult = false;
                    try
                    {
                        moduleHandlesArrayHandle = GCHandle.Alloc(moduleHandles, GCHandleType.Pinned);
                        enumResult = Interop.Kernel32.EnumProcessModules(processHandle, moduleHandlesArrayHandle.AddrOfPinnedObject(), moduleHandles.Length * IntPtr.Size, ref moduleCount);

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
                        //
                        // Also, EnumProcessModules is not a reliable method to get the modules for a process. 
                        // If OS loader is touching module information, this method might fail and copy part of the data.
                        // This is no easy solution to this problem. The only reliable way to fix this is to 
                        // suspend all the threads in target process. Of course we don't want to do this in Process class.
                        // So we just to try avoid the race by calling the same method 50 (an arbitrary number) times.
                        //
                        if (!enumResult)
                        {
                            bool sourceProcessIsWow64 = false;
                            bool targetProcessIsWow64 = false;
                            SafeProcessHandle hCurProcess = SafeProcessHandle.InvalidHandle;
                            try
                            {
                                hCurProcess = ProcessManager.OpenProcess(unchecked((int)Interop.Kernel32.GetCurrentProcessId()), Interop.Advapi32.ProcessOptions.PROCESS_QUERY_INFORMATION, true);
                                bool wow64Ret;

                                wow64Ret = Interop.Kernel32.IsWow64Process(hCurProcess, ref sourceProcessIsWow64);
                                if (!wow64Ret)
                                {
                                    throw new Win32Exception();
                                }

                                wow64Ret = Interop.Kernel32.IsWow64Process(processHandle, ref targetProcessIsWow64);
                                if (!wow64Ret)
                                {
                                    throw new Win32Exception();
                                }

                                if (sourceProcessIsWow64 && !targetProcessIsWow64)
                                {
                                    // Wow64 isn't going to allow this to happen, the best we can do is give a descriptive error to the user.
                                    throw new Win32Exception(Interop.Errors.ERROR_PARTIAL_COPY, SR.EnumProcessModuleFailedDueToWow);
                                }
                            }
                            finally
                            {
                                if (hCurProcess != SafeProcessHandle.InvalidHandle)
                                {
                                    hCurProcess.Dispose();
                                }
                            }

                            // If the failure wasn't due to Wow64, try again.
                            for (int i = 0; i < 50; i++)
                            {
                                enumResult = Interop.Kernel32.EnumProcessModules(processHandle, moduleHandlesArrayHandle.AddrOfPinnedObject(), moduleHandles.Length * IntPtr.Size, ref moduleCount);
                                if (enumResult)
                                {
                                    break;
                                }
                                Thread.Sleep(1);
                            }
                        }
                    }
                    finally
                    {
                        moduleHandlesArrayHandle.Free();
                    }

                    if (!enumResult)
                    {
                        throw new Win32Exception();
                    }

                    moduleCount /= IntPtr.Size;
                    if (moduleCount <= moduleHandles.Length) break;
                    moduleHandles = new IntPtr[moduleHandles.Length * 2];
                }

                var modules = new ProcessModuleCollection(firstModuleOnly ? 1 : moduleCount);

                char[] chars = new char[1024];

                for (int i = 0; i < moduleCount; i++)
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
                        HandleError();
                        continue;
                    }

                    var module = new ProcessModule()
                    {
                        ModuleMemorySize = ntModuleInfo.SizeOfImage,
                        EntryPointAddress = ntModuleInfo.EntryPoint,
                        BaseAddress = ntModuleInfo.BaseOfDll
                    };

                    int length = Interop.Kernel32.GetModuleBaseName(processHandle, moduleHandle, chars, chars.Length);
                    if (length == 0)
                    {
                        HandleError();
                        continue;
                    }

                    module.ModuleName = new string(chars, 0, length);

                    length = Interop.Kernel32.GetModuleFileNameEx(processHandle, moduleHandle, chars, chars.Length);
                    if (length == 0)
                    {
                        HandleError();
                        continue;
                    }

                    module.FileName = (length >= 4 && chars[0] == '\\' && chars[1] == '\\' && chars[2] == '?' && chars[3] == '\\') ?
                        new string(chars, 4, length - 4) :
                        new string(chars, 0, length);

                    modules.Add(module);
                }

                return modules;
            }
            finally
            {
#if FEATURE_TRACESWITCH
                Debug.WriteLineIf(Process._processTracing.TraceVerbose, "Process - CloseHandle(process)");
#endif
                if (!processHandle.IsInvalid)
                {
                    processHandle.Dispose();
                }
            }
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
            Interop.NtDll.NtProcessBasicInfo info = new Interop.NtDll.NtProcessBasicInfo();
            int status = Interop.NtDll.NtQueryInformationProcess(processHandle, Interop.NtDll.NtQueryProcessBasicInfo, info, (int)Marshal.SizeOf(info), null);
            if (status != 0)
            {
                throw new InvalidOperationException(SR.CantGetProcessId, new Win32Exception(status));
            }
            // We should change the signature of this function and ID property in process class.
            return info.UniqueProcessId.ToInt32();
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


    internal static class NtProcessInfoHelper
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

        public static ProcessInfo[] GetProcessInfos()
        {
            int requiredSize = 0;
            int status;

            ProcessInfo[] processInfos;
            GCHandle bufferHandle = new GCHandle();

            // Start with the default buffer size.
            int bufferSize = DefaultCachedBufferSize;

            // Get the cached buffer.
            long[] buffer = Interlocked.Exchange(ref CachedBuffer, null);

            try
            {
                do
                {
                    if (buffer == null)
                    {
                        // Allocate buffer of longs since some platforms require the buffer to be 64-bit aligned.
                        buffer = new long[(bufferSize + 7) / 8];
                    }
                    else
                    {
                        // If we have cached buffer, set the size properly.
                        bufferSize = buffer.Length * sizeof(long);
                    }

                    bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

                    status = Interop.NtDll.NtQuerySystemInformation(
                        Interop.NtDll.NtQuerySystemProcessInformation,
                        bufferHandle.AddrOfPinnedObject(),
                        bufferSize,
                        out requiredSize);

                    if ((uint)status == Interop.NtDll.STATUS_INFO_LENGTH_MISMATCH)
                    {
                        if (bufferHandle.IsAllocated) bufferHandle.Free();
                        buffer = null;
                        bufferSize = GetNewBufferSize(bufferSize, requiredSize);
                    }
                } while ((uint)status == Interop.NtDll.STATUS_INFO_LENGTH_MISMATCH);

                if (status < 0)
                { // see definition of NT_SUCCESS(Status) in SDK
                    throw new InvalidOperationException(SR.CouldntGetProcessInfos, new Win32Exception(status));
                }

                // Parse the data block to get process information
                processInfos = GetProcessInfos(bufferHandle.AddrOfPinnedObject());
            }
            finally
            {
                // Cache the final buffer for use on the next call.
                Interlocked.Exchange(ref CachedBuffer, buffer);

                if (bufferHandle.IsAllocated) bufferHandle.Free();
            }

            return processInfos;
        }

        // Use a smaller buffer size on debug to ensure we hit the retry path.
#if DEBUG
        private const int DefaultCachedBufferSize = 1024;
#else
        private const int DefaultCachedBufferSize = 128 * 1024;
#endif

        // Cache a single buffer for use in GetProcessInfos().
        private static long[] CachedBuffer;

        static ProcessInfo[] GetProcessInfos(IntPtr dataPtr)
        {
            // 60 is a reasonable number for processes on a normal machine.
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>(60);

            long totalOffset = 0;

            while (true)
            {
                IntPtr currentPtr = (IntPtr)((long)dataPtr + totalOffset);
                SystemProcessInformation pi = new SystemProcessInformation();

                Marshal.PtrToStructure(currentPtr, pi);

                // get information for a process
                ProcessInfo processInfo = new ProcessInfo();
                // Process ID shouldn't overflow. OS API GetCurrentProcessID returns DWORD.
                processInfo.ProcessId = pi.UniqueProcessId.ToInt32();
                processInfo.SessionId = (int)pi.SessionId;
                processInfo.PoolPagedBytes = (long)pi.QuotaPagedPoolUsage; ;
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


                if (pi.NamePtr == IntPtr.Zero)
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
                    string processName = GetProcessShortName(Marshal.PtrToStringUni(pi.NamePtr, pi.NameLength / sizeof(char)));
                    processInfo.ProcessName = processName;
                }

                // get the threads for current process
                processInfos[processInfo.ProcessId] = processInfo;

                currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(pi));
                int i = 0;
                while (i < pi.NumberOfThreads)
                {
                    SystemThreadInformation ti = new SystemThreadInformation();
                    Marshal.PtrToStructure(currentPtr, ti);
                    ThreadInfo threadInfo = new ThreadInfo();

                    threadInfo._processId = (int)ti.UniqueProcess;
                    threadInfo._threadId = (ulong)ti.UniqueThread;
                    threadInfo._basePriority = ti.BasePriority;
                    threadInfo._currentPriority = ti.Priority;
                    threadInfo._startAddress = ti.StartAddress;
                    threadInfo._threadState = (ThreadState)ti.ThreadState;
                    threadInfo._threadWaitReason = NtProcessManager.GetThreadWaitReason((int)ti.WaitReason);

                    processInfo._threadInfoList.Add(threadInfo);
                    currentPtr = (IntPtr)((long)currentPtr + Marshal.SizeOf(ti));
                    i++;
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
        internal class SystemProcessInformation
        {
            internal uint NextEntryOffset;
            internal uint NumberOfThreads;
            private long _SpareLi1;
            private long _SpareLi2;
            private long _SpareLi3;
            private long _CreateTime;
            private long _UserTime;
            private long _KernelTime;

            internal ushort NameLength;   // UNICODE_STRING   
            internal ushort MaximumNameLength;
            internal IntPtr NamePtr;     // This will point into the data block returned by NtQuerySystemInformation

            internal int BasePriority;
            internal IntPtr UniqueProcessId;
            internal IntPtr InheritedFromUniqueProcessId;
            internal uint HandleCount;
            internal uint SessionId;
            internal UIntPtr PageDirectoryBase;
            internal UIntPtr PeakVirtualSize;  // SIZE_T
            internal UIntPtr VirtualSize;
            internal uint PageFaultCount;

            internal UIntPtr PeakWorkingSetSize;
            internal UIntPtr WorkingSetSize;
            internal UIntPtr QuotaPeakPagedPoolUsage;
            internal UIntPtr QuotaPagedPoolUsage;
            internal UIntPtr QuotaPeakNonPagedPoolUsage;
            internal UIntPtr QuotaNonPagedPoolUsage;
            internal UIntPtr PagefileUsage;
            internal UIntPtr PeakPagefileUsage;
            internal UIntPtr PrivatePageCount;

            private long _ReadOperationCount;
            private long _WriteOperationCount;
            private long _OtherOperationCount;
            private long _ReadTransferCount;
            private long _WriteTransferCount;
            private long _OtherTransferCount;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class SystemThreadInformation
        {
            private long _KernelTime;
            private long _UserTime;
            private long _CreateTime;

            private uint _WaitTime;
            internal IntPtr StartAddress;
            internal IntPtr UniqueProcess;
            internal IntPtr UniqueThread;
            internal int Priority;
            internal int BasePriority;
            internal uint ContextSwitches;
            internal uint ThreadState;
            internal uint WaitReason;
        }
    }

    internal sealed class MainWindowFinder 
    {
        private const int GW_OWNER = 4;
        private IntPtr _bestHandle;
        private int _processId;
 
        public IntPtr FindMainWindow(int processId)
        {
            _bestHandle = (IntPtr)0;
            _processId = processId;
            
            Interop.User32.EnumThreadWindowsCallback callback = new Interop.User32.EnumThreadWindowsCallback(EnumWindowsCallback);
            Interop.User32.EnumWindows(callback, IntPtr.Zero);
 
            GC.KeepAlive(callback);
            return _bestHandle;
        }
 
        private bool IsMainWindow(IntPtr handle) 
        {           
            if (Interop.User32.GetWindow(handle, GW_OWNER) != (IntPtr)0 || !Interop.User32.IsWindowVisible(handle))
                return false;
            
            return true;
        }
 
        private bool EnumWindowsCallback(IntPtr handle, IntPtr extraParameter) {
            int processId;
            Interop.User32.GetWindowThreadProcessId(handle, out processId);

            if (processId == _processId) {
                if (IsMainWindow(handle)) {
                    _bestHandle = handle;
                    return false;
                }
            }
            return true;
        }
    }
}
