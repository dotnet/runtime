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
        public static IntPtr GetMainWindowHandle(int processId) 
        {
            MainWindowFinder finder = new MainWindowFinder();
            return finder.FindMainWindow(processId);
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

    internal static partial class NtProcessManager
    {
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
                for (;;)
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
                    if (moduleCount <= moduleHandles.Length)
                        break;
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
                if (!processHandle.IsInvalid)
                {
                    processHandle.Dispose();
                }
            }
        }
    }

    internal static class NtProcessInfoHelper
    {
        // Cache a single buffer for use in GetProcessInfos().
        private static long[] CachedBuffer;

        // Use a smaller buffer size on debug to ensure we hit the retry path.
#if DEBUG
        private const int DefaultCachedBufferSize = 1024;
#else
        private const int DefaultCachedBufferSize = 128 * 1024;
#endif

        internal static ProcessInfo[] GetProcessInfos(Predicate<int> processIdFilter = null)
        {
            int requiredSize = 0;
            int status;

            ProcessInfo[] processInfos;

            // Start with the default buffer size.
            int bufferSize = DefaultCachedBufferSize;

            // Get the cached buffer.
            long[] buffer = Interlocked.Exchange(ref CachedBuffer, null);

            try
            {
                while (true)
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

                    unsafe
                    {
                        // Note that the buffer will contain pointers to itself and it needs to be pinned while it is being processed 
                        // by GetProcessInfos below
                        fixed (long* bufferPtr = buffer)
                        {
                            status = Interop.NtDll.NtQuerySystemInformation(
                                Interop.NtDll.NtQuerySystemProcessInformation,
                                bufferPtr,
                                bufferSize,
                                out requiredSize);

                            if (unchecked((uint)status) != Interop.NtDll.STATUS_INFO_LENGTH_MISMATCH)
                            {
                                // see definition of NT_SUCCESS(Status) in SDK
                                if (status < 0)
                                {
                                    throw new InvalidOperationException(SR.CouldntGetProcessInfos, new Win32Exception(status));
                                }

                                // Parse the data block to get process information
                                processInfos = GetProcessInfos(MemoryMarshal.AsBytes<long>(buffer), processIdFilter);
                                break;
                            }
                        }
                    }

                    buffer = null;
                    bufferSize = GetNewBufferSize(bufferSize, requiredSize);
                }
            }
            finally
            {
                // Cache the final buffer for use on the next call.
                Interlocked.Exchange(ref CachedBuffer, buffer);
            }

            return processInfos;
        }

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

        private static unsafe ProcessInfo[] GetProcessInfos(ReadOnlySpan<byte> data, Predicate<int> processIdFilter)
        {
            // Use a dictionary to avoid duplicate entries if any
            // 60 is a reasonable number for processes on a normal machine.
            Dictionary<int, ProcessInfo> processInfos = new Dictionary<int, ProcessInfo>(60);

            int processInformationOffset = 0;

            while (true)
            {
                ref readonly SystemProcessInformation pi = ref MemoryMarshal.AsRef<SystemProcessInformation>(data.Slice(processInformationOffset));

                // Process ID shouldn't overflow. OS API GetCurrentProcessID returns DWORD.
                int processInfoProcessId = pi.UniqueProcessId.ToInt32();
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

                    int threadInformationOffset = processInformationOffset + sizeof(SystemProcessInformation);
                    for (int i = 0; i < pi.NumberOfThreads; i++)
                    {
                        ref readonly SystemThreadInformation ti = ref MemoryMarshal.AsRef<SystemThreadInformation>(data.Slice(threadInformationOffset));

                        ThreadInfo threadInfo = new ThreadInfo();

                        threadInfo._processId = (int)ti.ClientId.UniqueProcess;
                        threadInfo._threadId = (ulong)ti.ClientId.UniqueThread;
                        threadInfo._basePriority = ti.BasePriority;
                        threadInfo._currentPriority = ti.Priority;
                        threadInfo._startAddress = ti.StartAddress;
                        threadInfo._threadState = (ThreadState)ti.ThreadState;
                        threadInfo._threadWaitReason = NtProcessManager.GetThreadWaitReason((int)ti.WaitReason);

                        processInfo._threadInfoList.Add(threadInfo);

                        threadInformationOffset += sizeof(SystemThreadInformation);
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
        internal static string GetProcessShortName(string name)
        {
            if (string.IsNullOrEmpty(name))
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
                ReadOnlySpan<char> extension = name.AsSpan(period);

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
