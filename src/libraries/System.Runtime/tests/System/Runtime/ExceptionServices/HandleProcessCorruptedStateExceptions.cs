// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using Xunit;

namespace System.Runtime.ExceptionServices.Tests
{
    public class HandleProcessCorruptedStateExceptionsTests : RemoteExecutorTestBase
    {
        [DllImport("kernel32.dll")]
        static extern void RaiseException(uint dwExceptionCode, uint dwExceptionFlags, uint nNumberOfArguments, IntPtr lpArguments);

        [DllImport("kernel32.dll")]
        private static extern int SetErrorMode(int uMode);

        private const int SEM_NOGPFAULTERRORBOX = 2;

        [HandleProcessCorruptedStateExceptions]
        static void CauseAVInNative()
        {
            SetErrorMode(SEM_NOGPFAULTERRORBOX);
            try 
            {
                RaiseException(0xC0000005, 0, 0, IntPtr.Zero);
            }
            catch (AccessViolationException)
            {
            }
        }

        [Fact]
        [PlatformSpecific(TestPlatforms.Windows)] // Feature Corrupting Exceptions not present for Linux
        [ActiveIssue("https://github.com/dotnet/corefx/issues/21123", TargetFrameworkMonikers.Uap)]
        public static void ProcessExit_Called()
        {
            using (Process p = RemoteInvoke(() => { CauseAVInNative(); return SuccessExitCode; }).Process)
            {
                p.WaitForExit();
                if (PlatformDetection.IsFullFramework)
                    Assert.Equal(SuccessExitCode, p.ExitCode);
                else
                    Assert.NotEqual(SuccessExitCode, p.ExitCode);
            }            
        }
    }
}
