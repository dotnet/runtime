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
        internal static Process? GetParentProcess()
        {
            nint snapshotHandle = 0;
            try
            {
                // Get a list of all processes
                // SAFETY: To be audited
                unsafe
                {
                    snapshotHandle = Interop.Kernel32.CreateToolhelp32Snapshot(Interop.Kernel32.SnapshotFlags.Process, 0);
                }

                Interop.Kernel32.PROCESSENTRY32 procEntry = default;
                procEntry.dwSize = sizeof(Interop.Kernel32.PROCESSENTRY32);
                // SAFETY: To be audited
                unsafe
                {
                    if (Interop.Kernel32.Process32First(snapshotHandle, &procEntry))
                    {
                        int currentProcessId = Environment.ProcessId;
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
            }
            catch (Exception)
            {
            }
            finally
            {
                // SAFETY: To be audited
                unsafe
                {
                    Interop.Kernel32.CloseHandle(snapshotHandle);
                }
            }

            return null;
        }
    }
}
