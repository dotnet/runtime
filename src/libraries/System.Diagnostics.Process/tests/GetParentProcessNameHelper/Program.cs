// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace GetParentProcessNameHelper
{
    internal static partial class Program
    {
        internal static void Main(string[] args)
        {
            var parent = GetParentProcess();
            if (parent is null)
            {
                Console.WriteLine("NULL");
            }
            else
            {
                Console.WriteLine(parent.ProcessName);
            }
        }

        #region Code taken from Win32.cs in namespace Microsoft.Extensions.Hosting.WindowsServices

        [SupportedOSPlatform("windows")]
        internal static Process? GetParentProcess()
        {
            var snapshotHandle = IntPtr.Zero;
            try
            {
                // Get a list of all processes
                snapshotHandle = Interop.Kernel32.CreateToolhelp32Snapshot(Interop.Kernel32.SnapshotFlags.Process, 0);

                Interop.Kernel32.PROCESSENTRY32 procEntry = default(Interop.Kernel32.PROCESSENTRY32);
                procEntry.dwSize = Marshal.SizeOf(typeof(Interop.Kernel32.PROCESSENTRY32));
                if (Interop.Kernel32.Process32First(snapshotHandle, ref procEntry))
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
                            return Process.GetProcessById((int)procEntry.th32ParentProcessID);
                        }
                    }
                    while (Interop.Kernel32.Process32Next(snapshotHandle, ref procEntry));
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

        #endregion
    }
}
