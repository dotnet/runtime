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
        private const int ConsoleEncoding = 437;

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestChangesInConsoleEncoding()
        {
            void RunWithExpectedCodePage(int expectedCodePage)
            {
                Process p = CreateProcessLong();
                p.StartInfo.RedirectStandardInput = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.Start();

                Assert.Equal(p.StandardInput.Encoding.CodePage, expectedCodePage);
                Assert.Equal(p.StandardOutput.CurrentEncoding.CodePage, expectedCodePage);
                Assert.Equal(p.StandardError.CurrentEncoding.CodePage, expectedCodePage);

                p.Kill();
                Assert.True(p.WaitForExit(WaitInMS));
            };

            // Don't test this on Windows containers, as the test is currently failing
            // cf. https://github.com/dotnet/runtime/issues/42000
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
