// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.XUnitExtensions;
using Xunit;

namespace System.Diagnostics.Tests
{
    public partial class ProcessTests
    {
        private string WriteScriptFile(string directory, string name, int returnValue)
        {
            string filename = Path.Combine(directory, name);
            filename += ".bat";
            File.WriteAllText(filename, $"exit {returnValue}");
            return filename;
        }
        
        private static void SendSignal(PosixSignal signal, int processId)
        {
            uint dwCtrlEvent = signal switch
            {
                PosixSignal.SIGINT => Interop.Kernel32.CTRL_C_EVENT,
                PosixSignal.SIGQUIT => Interop.Kernel32.CTRL_BREAK_EVENT,
                _ => throw new ArgumentOutOfRangeException(nameof(signal))
            };

            if (!Interop.GenerateConsoleCtrlEvent(dwCtrlEvent, (uint)processId))
            {
                int error = Marshal.GetLastWin32Error();
                if (error == Interop.Errors.ERROR_INVALID_FUNCTION && PlatformDetection.IsInContainer)
                {
                    // Docker in CI runs without a console attached.
                    throw new SkipTestException($"GenerateConsoleCtrlEvent failed with ERROR_INVALID_FUNCTION. The process is not a console process or does not have a console.");
                }

                throw new Win32Exception(error);
            }
        }

        // See https://learn.microsoft.com/windows/win32/api/processthreadsapi/nf-processthreadsapi-createprocessw#remarks:
        // When a process is created with CREATE_NEW_PROCESS_GROUP specified, an implicit call to SetConsoleCtrlHandler(NULL,TRUE) 
        // is made on behalf of the new process; this means that the new process has CTRL+C disabled.
        private static unsafe void ReEnableCtrlCHandlerIfNeeded(PosixSignal signal)
        {
            if (signal is PosixSignal.SIGINT)
            {
                if (!Interop.Kernel32.SetConsoleCtrlHandler(null, false))
                {
                    throw new Win32Exception();
                }
            }
        }
    }
}
