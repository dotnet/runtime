// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Diagnostics.Tests
{
    public class ProcessStandardConsoleTests : ProcessTestBase
    {
        private const int s_ConsoleEncoding = 437;

        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void TestChangesInConsoleEncoding()
        {
            Action<int> run = expectedCodePage =>
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

            if (!OperatingSystem.IsWindows())
            {
                run(Encoding.UTF8.CodePage);
                return;
            }

            int inputEncoding = Interop.GetConsoleCP();
            int outputEncoding = Interop.GetConsoleOutputCP();

            try
            {
                // Don't test this on Windows Nano, Windows Nano only supports UTF8.
                if (File.Exists(Path.Combine(Environment.GetEnvironmentVariable("windir"), "regedit.exe")))
                {
                    Interop.SetConsoleCP(s_ConsoleEncoding);
                    Interop.SetConsoleOutputCP(s_ConsoleEncoding);

                    run(s_ConsoleEncoding);
                }
            }
            finally
            {
                Interop.SetConsoleCP(inputEncoding);
                Interop.SetConsoleOutputCP(outputEncoding);
            }
        }
    }
}
