// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Microsoft.Extensions.Hosting.WindowsServices.Internal
{
    [SupportedOSPlatform("windows")]
    internal static class Win32
    {
        internal static unsafe Process? GetParentProcess()
        {
            nint snapshotHandle = 0;
            try
            {
                // Get a list of all processes
                snapshotHandle = Interop.Kernel32.CreateToolhelp32Snapshot(Interop.Kernel32.SnapshotFlags.Process, 0);

                Interop.Kernel32.PROCESSENTRY32 procEntry = default;
                procEntry.dwSize = sizeof(Interop.Kernel32.PROCESSENTRY32);
                if (Interop.Kernel32.Process32First(snapshotHandle, &procEntry))
                {
                    int currentProcessId =
#if NET
                        Environment.ProcessId;
#else
                        Process.GetCurrentProcess().Id;
#endif
                    do
                    {
                        if (currentProcessId == procEntry.th32ProcessID)
                        {
                            return Process.GetProcessById(procEntry.th32ParentProcessID);
                        }
                    }
                    while (Interop.Kernel32.Process32Next(snapshotHandle, &procEntry));
                }
            }
            catch (Exception)
            {
            }
            finally
            {
                Interop.Kernel32.CloseHandle(snapshotHandle);
            }

            return null;
        }
    }
}
