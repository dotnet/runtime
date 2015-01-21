// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Diagnostics;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public partial class ProcessTest
    {
        [System.Runtime.InteropServices.DllImport("api-ms-win-core-console-l1-1-0.dll")]
        private extern static int GetConsoleCP();

        [System.Runtime.InteropServices.DllImport("api-ms-win-core-console-l1-1-0.dll")]
        private extern static int GetConsoleOutputCP();

        [System.Runtime.InteropServices.DllImport("api-ms-win-core-console-l1-1-0.dll")]
        private extern static int SetConsoleCP(int codePage);

        [System.Runtime.InteropServices.DllImport("api-ms-win-core-console-l1-1-0.dll")]
        private extern static int SetConsoleOutputCP(int codePage);

        private const int s_ConsoleEncoding = 437;

        [Fact]
        [ActiveIssue(335)]
        public static void Process_EncodingBeforeProvider()
        {
            SetConsoleCP(s_ConsoleEncoding);
            SetConsoleOutputCP(s_ConsoleEncoding);

            int inputEncoding = GetConsoleCP();
            int outputEncoding = GetConsoleOutputCP();

            try
            {
                Process p = CreateProcessInfinite();
                p.StartInfo.RedirectStandardInput = true;
                p.Start();
                Assert.Equal(p.StandardInput.Encoding.CodePage, Encoding.UTF8.CodePage);
                p.Kill();
                p.WaitForExit();

                p = CreateProcessInfinite();
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                Assert.Equal(p.StandardOutput.CurrentEncoding.CodePage,  Encoding.UTF8.CodePage);
                p.Kill();
                p.WaitForExit();

                p = CreateProcessInfinite();
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                Assert.Equal(p.StandardError.CurrentEncoding.CodePage, Encoding.UTF8.CodePage);
                p.Kill();
                p.WaitForExit();


                // Register the codeprovider which will ensure 437 is enabled.
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                p = CreateProcessInfinite();
                p.StartInfo.RedirectStandardInput = true;
                p.Start();
                Assert.Equal(p.StandardInput.Encoding.CodePage, inputEncoding);
                p.Kill();
                p.WaitForExit();

                p = CreateProcessInfinite();
                p.StartInfo.RedirectStandardOutput = true;
                p.Start();
                Assert.Equal(p.StandardOutput.CurrentEncoding.CodePage, outputEncoding);
                p.Kill();
                p.WaitForExit();

                p = CreateProcessInfinite();
                p.StartInfo.RedirectStandardError = true;
                p.Start();
                Assert.Equal(p.StandardError.CurrentEncoding.CodePage, outputEncoding);
                p.Kill();
                p.WaitForExit();
            }
            finally
            {
                foreach (Process p in Process.GetProcessesByName(s_ProcessName))
                {
                    p.Kill();
                    p.WaitForExit();
                }
            }
        }
    }
}