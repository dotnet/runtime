// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Microsoft.Win32;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessStandardConsoleTests : ProcessTestBase
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestChangesInConsoleEncoding()
        {
            const int ConsoleEncoding = 437;

            void RunWithExpectedCodePage(int expectedCodePage)
            {
                Process p = CreateProcessLong();
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();

                Assert.Equal(expectedCodePage, p.StandardInput.Encoding.CodePage);
                Assert.Equal(expectedCodePage, p.StandardOutput.CurrentEncoding.CodePage);
                Assert.Equal(expectedCodePage, p.StandardError.CurrentEncoding.CodePage);

                p.Kill();
                Assert.True(p.WaitForExit(WaitInMS));
            };

            // Don't test this on Windows containers, as there is a known issue.
            // See https://github.com/dotnet/runtime/issues/42000 for more details.
            if (!OperatingSystem.IsWindows() || PlatformDetection.IsInContainer)
            {
                RunWithExpectedCodePage(Encoding.UTF8.CodePage);
                return;
            }

            int inputEncoding = Interop.GetConsoleCP();
            int outputEncoding = Interop.GetConsoleOutputCP();

            try
            {
                Interop.SetConsoleCP(ConsoleEncoding);
                Interop.SetConsoleOutputCP(ConsoleEncoding);

                RunWithExpectedCodePage(ConsoleEncoding);
            }
            finally
            {
                Interop.SetConsoleCP(inputEncoding);
                Interop.SetConsoleOutputCP(outputEncoding);
            }
        }
    }
}
