// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.IO;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public partial class ProcessTest
    {

        Process CreateProcessError()
        {
            return CreateProcess("error");
        }

        Process CreateProcessInput()
        {
            return CreateProcess("input");
        }

        Process CreateProcessStream()
        {
            return CreateProcess("stream");
        }

        [Fact]
        public void Process_SyncErrorStream()
        {
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            Assert.Equal(p.StandardError.ReadToEnd(), "ProcessTest_ConsoleApp.exe error stream\r\n");
            p.WaitForExit(WaitInMS);
        }

        [Fact]
        public void Process_AsyncErrorStream()
        {
            StringBuilder sb = new StringBuilder();
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += (s, e) => { sb.Append(e.Data); };
            p.Start();
            p.BeginErrorReadLine();

            if (p.WaitForExit(WaitInMS))
                p.WaitForExit(); // This ensures async event handlers are finished processing.
            
            Assert.Equal("ProcessTest_ConsoleApp.exe error stream", sb.ToString());
        }

        [Fact]
        public void Process_SyncOutputStream()
        {
            Process p = CreateProcessStream();
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string s = p.StandardOutput.ReadToEnd();
            p.WaitForExit(WaitInMS);
            Assert.Equal(s, "ProcessTest_ConsoleApp.exe started\r\nProcessTest_ConsoleApp.exe closed\r\n");
        }

        [Fact]
        public void Process_AsyncOutputStream()
        {
            {
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcessStream();
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) => sb.Append(e.Data);
                p.Start();
                p.BeginOutputReadLine();
                if (p.WaitForExit(WaitInMS))
                    p.WaitForExit(); // This ensures async event handlers are finished processing.

                Assert.Equal(sb.ToString(), "ProcessTest_ConsoleApp.exe startedProcessTest_ConsoleApp.exe closed");
            }

            {
                // Now add the CancelAsyncAPI as well.
                StringBuilder sb = new StringBuilder();
                Process p = CreateProcessStream();
                p.StartInfo.RedirectStandardOutput = true;
                p.OutputDataReceived += (s, e) => { sb.Append(e.Data); ((Process)s).CancelOutputRead(); };
                p.Start();
                p.BeginOutputReadLine();
                if (p.WaitForExit(WaitInMS))
                    p.WaitForExit(); // This ensures async event handlers are finished processing.

                Assert.Equal(sb.ToString(), "ProcessTest_ConsoleApp.exe started");
            }
        }

        [Fact]
        public void Process_SyncStreams()
        {
            Process p = CreateProcessInput();
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += (s, e) => { Assert.Equal(e.Data, "This string should come as output"); };
            p.Start();
            using (StreamWriter writer = p.StandardInput)
            {
                string str = "This string should come as output";
                writer.WriteLine(str);
            }
        }
    }
}
