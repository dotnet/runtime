// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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
        public void Process_EncodingBeforeProvider()
        {
            Action<int> run = expectedCodePage =>
            {
                Process p = CreateProcessInfinite();
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

            if (global::Interop.PlatformDetection.OperatingSystem != global::Interop.OperatingSystem.Windows)
            {
                run(Encoding.UTF8.CodePage);
                return;
            }

            int inputEncoding = GetConsoleCP();
            int outputEncoding = GetConsoleOutputCP();

            try
            {
                {
                    SetConsoleCP(s_ConsoleEncoding);
                    SetConsoleOutputCP(s_ConsoleEncoding);

                    run(Encoding.UTF8.CodePage);
                }

                {
                    SetConsoleCP(s_ConsoleEncoding);
                    SetConsoleOutputCP(s_ConsoleEncoding);

                    // Register the codeprovider which will ensure 437 is enabled.
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

                    run(s_ConsoleEncoding);
                }
            }
            finally
            {
                SetConsoleCP(inputEncoding);
                SetConsoleOutputCP(outputEncoding);
            }
        }

    }
}