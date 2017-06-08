// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;

internal partial class Interop
{
    internal partial class Kernel32
    {
        [DllImport(Libraries.Kernel32, CharSet = CharSet.Unicode, SetLastError = true, BestFitMapping = false, EntryPoint = "CreateProcessW")]
        internal static extern bool CreateProcess(
            [MarshalAs(UnmanagedType.LPTStr)] string lpApplicationName,
            StringBuilder lpCommandLine,
            ref SECURITY_ATTRIBUTES procSecAttrs,
            ref SECURITY_ATTRIBUTES threadSecAttrs,
            bool bInheritHandles,
            int dwCreationFlags,
            IntPtr lpEnvironment,
            [MarshalAs(UnmanagedType.LPTStr)] string lpCurrentDirectory,
            STARTUPINFO lpStartupInfo,
            [Out] PROCESS_INFORMATION lpProcessInformation
        );

        [StructLayout(LayoutKind.Sequential)]
        internal class PROCESS_INFORMATION
        {
            internal IntPtr hProcess = IntPtr.Zero;
            internal IntPtr hThread = IntPtr.Zero;
            internal int dwProcessId = 0;
            internal int dwThreadId = 0;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal class STARTUPINFO : IDisposable
        {
            internal int cb;
            internal IntPtr lpReserved = IntPtr.Zero;
            internal IntPtr lpDesktop = IntPtr.Zero;
            internal IntPtr lpTitle = IntPtr.Zero;
            internal int dwX = 0;
            internal int dwY = 0;
            internal int dwXSize = 0;
            internal int dwYSize = 0;
            internal int dwXCountChars = 0;
            internal int dwYCountChars = 0;
            internal int dwFillAttribute = 0;
            internal int dwFlags = 0;
            internal short wShowWindow = 0;
            internal short cbReserved2 = 0;
            internal IntPtr lpReserved2 = IntPtr.Zero;
            internal SafeFileHandle hStdInput = new SafeFileHandle(IntPtr.Zero, false);
            internal SafeFileHandle hStdOutput = new SafeFileHandle(IntPtr.Zero, false);
            internal SafeFileHandle hStdError = new SafeFileHandle(IntPtr.Zero, false);

            internal
                STARTUPINFO()
            {
                cb = Marshal.SizeOf(this);
            }

            public void Dispose()
            {
                // close the handles created for child process
                if (hStdInput != null && !hStdInput.IsInvalid)
                {
                    hStdInput.Dispose();
                    hStdInput = null;
                }

                if (hStdOutput != null && !hStdOutput.IsInvalid)
                {
                    hStdOutput.Dispose();
                    hStdOutput = null;
                }

                if (hStdError != null && !hStdError.IsInvalid)
                {
                    hStdError.Dispose();
                    hStdError = null;
                }
            }
        }
    }
}
