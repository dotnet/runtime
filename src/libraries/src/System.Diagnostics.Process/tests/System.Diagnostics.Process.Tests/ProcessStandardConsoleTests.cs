// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using System.Text;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public class ProcessStandardConsoleTests : ProcessTestBase
    {
        private const int s_ConsoleEncoding = 437;

        [Fact]
        public void TestChangesInConsoleEncoding()
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

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                run(Encoding.UTF8.CodePage);
                return;
            }

            int inputEncoding = Interop.GetConsoleCP();
            int outputEncoding = Interop.GetConsoleOutputCP();

            try
            {
                {
                    Interop.SetConsoleCP(s_ConsoleEncoding);
                    Interop.SetConsoleOutputCP(s_ConsoleEncoding);

                    run(Encoding.UTF8.CodePage);
                }

                {
                    Interop.SetConsoleCP(s_ConsoleEncoding);
                    Interop.SetConsoleOutputCP(s_ConsoleEncoding);

                    // Register the codeprovider which will ensure 437 is enabled.
                    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

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