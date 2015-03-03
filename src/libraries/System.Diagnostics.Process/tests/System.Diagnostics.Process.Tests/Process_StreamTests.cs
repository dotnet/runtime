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
            Process p = new Process();
            p.StartInfo.FileName = _processName;
            p.StartInfo.Arguments = "error";
            return p;
        }

        Process CreateProcessInput()
        {
            Process p = new Process();
            p.StartInfo.FileName = _processName;
            p.StartInfo.Arguments = "input";
            return p;
        }

        Process CreateProcessStream()
        {
            Process p = new Process();
            p.StartInfo.FileName = _processName;
            p.StartInfo.Arguments = "stream";
            return p;
        }

        [Fact]
        public void Process_SyncErrorStream()
        {
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            Assert.Equal(p.StandardError.ReadToEnd(), "ProcessTest_ConsoleApp.exe error stream\r\n");
            p.WaitForExit();
        }

        private StringBuilder process_AsyncErrorStream_sb = new StringBuilder();

        [Fact]
        public void Process_AsyncErrorStream()
        {
            process_AsyncErrorStream_sb.Clear();
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += process_AsyncErrorStream_ErrorDataReceived;
            p.Start();
            p.BeginErrorReadLine();
            p.WaitForExit();
            Assert.Equal("ProcessTest_ConsoleApp.exe error stream", process_AsyncErrorStream_sb.ToString());
        }

        public void process_AsyncErrorStream_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            process_AsyncErrorStream_sb.Append(e.Data);
        }

        [Fact]
        public void Process_SyncOutputStream()
        {
            Process p = CreateProcessStream();
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string s = p.StandardOutput.ReadToEnd();
            p.WaitForExit();
            Assert.Equal(s, "ProcessTest_ConsoleApp.exe started\r\nProcessTest_ConsoleApp.exe closed\r\n");
        }

        private StringBuilder process_AsyncOutputStream_sb = new StringBuilder();

        [Fact]
        public void Process_AsyncOutputStream()
        {
            process_AsyncOutputStream_sb.Clear();
            Process p = CreateProcessStream();
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += process_AsyncOutputStream_OutputDataReceived;
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            Assert.Equal(process_AsyncOutputStream_sb.ToString(), "ProcessTest_ConsoleApp.exe startedProcessTest_ConsoleApp.exe closed");


            // Now add the CancelAsyncAPI as well.
            process_AsyncOutputStream_sb.Clear();
            p = CreateProcessStream();
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += process_AsyncOutputStream_OutputDataReceived2;
            p.Start();
            p.BeginOutputReadLine();
            p.WaitForExit();
            Assert.Equal(process_AsyncOutputStream_sb.ToString(), "ProcessTest_ConsoleApp.exe started");
        }

        void process_AsyncOutputStream_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            process_AsyncOutputStream_sb.Append(e.Data);
        }

        void process_AsyncOutputStream_OutputDataReceived2(object sender, DataReceivedEventArgs e)
        {
            process_AsyncOutputStream_sb.Append(e.Data);
            Process p = sender as Process;
            p.CancelOutputRead();
        }

        [Fact]
        public void Process_SyncStreams()
        {
            // Check whether the input streams works correctly.

            //1. Check with RedirectStandardInput
            Process p = CreateProcessInput();
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += process_SyncStreams_OutputDataReceived;
            p.Start();
            using (StreamWriter writer = p.StandardInput)
            {
                string str = "This string should come as output";
                writer.WriteLine(str);
            }
        }

        public void process_SyncStreams_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Assert.Equal(e.Data, "This string should come as output");
            }
        }
    }
}
