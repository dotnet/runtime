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
#if FEATURE_TRACESWITCH
                Debug.WriteLineIf(Process._processTracing.TraceVerbose, "Process - CloseHandle(process)");
#endif
                if (!processHandle.IsInvalid)
                {
                    processHandle.Dispose();
                }
            }
        }
    }

    internal static partial class NtProcessInfoHelper
    {
        // Cache a single buffer for use in GetProcessInfos().
        private static long[] CachedBuffer;

        internal static ProcessInfo[] GetProcessInfos(Predicate<int> processIdFilter = null)
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

                    if (unchecked((uint)status) == Interop.NtDll.STATUS_INFO_LENGTH_MISMATCH)
                    {
                        if (bufferHandle.IsAllocated) bufferHandle.Free();
                        buffer = null;
                        bufferSize = GetNewBufferSize(bufferSize, requiredSize);
                    }
                } while (unchecked((uint)status) == Interop.NtDll.STATUS_INFO_LENGTH_MISMATCH);

                if (status < 0)
                { // see definition of NT_SUCCESS(Status) in SDK
                    throw new InvalidOperationException(SR.CouldntGetProcessInfos, new Win32Exception(status));
                }

                // Parse the data block to get process information
                processInfos = GetProcessInfos(bufferHandle.AddrOfPinnedObject(), processIdFilter);
            }
            finally
            {
                // Cache the final buffer for use on the next call.
                Interlocked.Exchange(ref CachedBuffer, buffer);

                if (bufferHandle.IsAllocated) bufferHandle.Free();
            }

            return processInfos;
        }
    }
}
