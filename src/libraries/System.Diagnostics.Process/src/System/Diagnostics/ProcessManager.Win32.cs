// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32.SafeHandles;

using SYSTEM_PROCESS_INFORMATION = Interop.NtDll.SYSTEM_PROCESS_INFORMATION;
using SYSTEM_THREAD_INFORMATION = Interop.NtDll.SYSTEM_THREAD_INFORMATION;

namespace System.Diagnostics
{
    internal static partial class ProcessManager
    {
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

    internal static partial class NtProcessManager
    {
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

                bool succeeded = Interop.Kernel32.EnumProcessModules(processHandle, null, 0, out int needed);

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

                    EnumProcessModulesUntilSuccess(processHandle, null, 0, out needed);
                }

                int modulesCount = needed / IntPtr.Size;
                IntPtr[] moduleHandles = new IntPtr[modulesCount];
                while (true)
                {
                    int size = needed;
                    EnumProcessModulesUntilSuccess(processHandle, moduleHandles, size, out needed);
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

                        var module = new ProcessModule()
                        {
                            ModuleMemorySize = ntModuleInfo.SizeOfImage,
                            EntryPointAddress = ntModuleInfo.EntryPoint,
                            BaseAddress = ntModuleInfo.BaseOfDll
                        };

                        int length = 0;
                        while ((length = Interop.Kernel32.GetModuleBaseName(processHandle, moduleHandle, chars, chars.Length)) == chars.Length)
                        {
                            char[] toReturn = chars;
                            chars = ArrayPool<char>.Shared.Rent(length * 2);
                            ArrayPool<char>.Shared.Return(toReturn);
                        }

                        if (length == 0)
                        {
                            module.Dispose();
                            HandleLastWin32Error();
                            continue;
                        }

                        module.ModuleName = new string(chars, 0, length);

                        while ((length = Interop.Kernel32.GetModuleFileNameEx(processHandle, moduleHandle, chars, chars.Length)) == chars.Length)
                        {
                            char[] toReturn = chars;
                            chars = ArrayPool<char>.Shared.Rent(length * 2);
                            ArrayPool<char>.Shared.Return(toReturn);
                        }

                        if (length == 0)
                        {
                            module.Dispose();
                            HandleLastWin32Error();
                            continue;
                        }

                        const string NtPathPrefix = @"\\?\";
                        ReadOnlySpan<char> charsSpan = chars.AsSpan(0, length);
                        if (charsSpan.StartsWith(NtPathPrefix))
                        {
                            charsSpan = charsSpan.Slice(NtPathPrefix.Length);
                        }
                        module.FileName = charsSpan.ToString();

                        modules.Add(module);
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

        private static void EnumProcessModulesUntilSuccess(SafeProcessHandle processHandle, IntPtr[]? modules, int size, out int needed)
        {
            // When called on a running process, EnumProcessModules may fail with ERROR_PARTIAL_COPY
            // if the target process is not yet initialized or if the module list changes during the function call.
            // We just try to avoid the race by retring 50 (an arbitrary number) times.
            int i = 0;
            while (true)
            {
                if (Interop.Kernel32.EnumProcessModules(processHandle, modules, size, out needed))
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

        internal static unsafe ProcessInfo[] GetProcessInfos(int? processIdFilter = null)
        {
            // Start with the default buffer size.
            uint bufferSize = MostRecentSize;

            while (true)
            {
                void* bufferPtr = NativeMemory.Alloc(bufferSize); // some platforms require the buffer to be 64-bit aligned and NativeLibrary.Alloc guarantees sufficient alignment.

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
                        return GetProcessInfos(new ReadOnlySpan<byte>(bufferPtr, (int)actualSize), processIdFilter);
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

        private static unsafe ProcessInfo[] GetProcessInfos(ReadOnlySpan<byte> data, int? processIdFilter)
        {
            // Use a dictionary to avoid duplicate entries if any
            // 60 is a reasonable number for processes on a normal machine.
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>(60);

            int processInformationOffset = 0;

            while (true)
            {
                ref readonly SYSTEM_PROCESS_INFORMATION pi = ref MemoryMarshal.AsRef<SYSTEM_PROCESS_INFORMATION>(data.Slice(processInformationOffset));

                // Process ID shouldn't overflow. OS API GetCurrentProcessID returns DWORD.
                int processInfoProcessId = pi.UniqueProcessId.ToInt32();
                if (processIdFilter == null || processIdFilter.GetValueOrDefault() == processInfoProcessId)
                {
                    // get information for a process
                    ProcessInfo processInfo = new ProcessInfo((int)pi.NumberOfThreads)
                    {
                        ProcessId = processInfoProcessId,
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
                        string processName = GetProcessShortName(new ReadOnlySpan<char>(pi.ImageName.Buffer.ToPointer(), pi.ImageName.Length / sizeof(char)));
                        processInfo.ProcessName = processName;
                    }

                    // get the threads for current process
                    processInfos[processInfo.ProcessId] = processInfo;

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

                if (pi.NextEntryOffset == 0)
                {
                    break;
                }
                processInformationOffset += (int)pi.NextEntryOffset;
            }

            ProcessInfo[] temp = new ProcessInfo[processInfos.Values.Count];
            processInfos.Values.CopyTo(temp, 0);
            return temp;
        }

        // This function generates the short form of process name.
        //
        // This is from GetProcessShortName in NT code base.
        // Check base\screg\winreg\perfdlls\process\perfsprc.c for details.
        internal static string GetProcessShortName(ReadOnlySpan<char> name)
        {
            if (name.IsEmpty)
            {
                return string.Empty;
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
                ReadOnlySpan<char> extension = name.Slice(period);

                if (extension.Equals(".exe", StringComparison.OrdinalIgnoreCase))
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
            return name.Slice(slash, period - slash + 1).ToString();
        }
    }
}
