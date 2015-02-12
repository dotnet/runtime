// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.IO;
using Xunit;

namespace System.Diagnostics.ProcessTests
{
    public partial class ProcessTest
    {
        [Fact]
        public static void Process_SyncErrorStream()
        {
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            if (!p.HasExited)
            {
                string s = p.StandardError.ReadToEnd();
                p.WaitForExit();

                s = s.Replace("\r", "").Replace("\n", "");

                Assert.Equal("Unhandled Exception: Intentional Exception thrown", s);
            }
        }

        private static StringBuilder s_process_AsyncErrorStream_sb = new StringBuilder();

        [Fact]
        public static void Process_AsyncErrorStream()
        {
            s_process_AsyncErrorStream_sb.Clear();
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.ErrorDataReceived += process_AsyncErrorStream_ErrorDataReceived;
            p.Start();
            if (!p.HasExited)
            {
                p.BeginErrorReadLine();
                p.WaitForExit();
                Assert.Equal("Unhandled Exception: Intentional Exception thrown", s_process_AsyncErrorStream_sb.ToString());
            }
        }

        public static void process_AsyncErrorStream_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            s_process_AsyncErrorStream_sb.Append(e.Data);
        }

        [Fact]
        public static void Process_SyncOutputStream()
        {
            Process p = CreateProcess();
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            if (!p.HasExited)
            {
                string s = p.StandardOutput.ReadToEnd();
                p.WaitForExit();
                Assert.Equal(s, "ProcessTest_ConsoleApp.exe started\r\nProcessTest_ConsoleApp.exe closed\r\n");
            }
        }

        private static StringBuilder s_process_AsyncOutputStream_sb = new StringBuilder();

        [Fact]
        public static void Process_AsyncOutputStream()
        {
            s_process_AsyncOutputStream_sb.Clear();
            Process p = CreateProcess();
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += process_AsyncOutputStream_OutputDataReceived;
            p.Start();
            if (!p.HasExited)
            {
                p.BeginOutputReadLine();
                p.WaitForExit();
                Assert.Equal(s_process_AsyncOutputStream_sb.ToString(), "ProcessTest_ConsoleApp.exe startedProcessTest_ConsoleApp.exe closed");
            }

            // Now add the CancelAsyncAPI as well.
            s_process_AsyncOutputStream_sb.Clear();
            p = CreateProcess();
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += process_AsyncOutputStream_OutputDataReceived2;
            p.Start();
            if (!p.HasExited)
            {
                p.BeginOutputReadLine();
                p.WaitForExit();
                Assert.Equal(s_process_AsyncOutputStream_sb.ToString(), "ProcessTest_ConsoleApp.exe started");
            }
        }

        static void process_AsyncOutputStream_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            s_process_AsyncOutputStream_sb.Append(e.Data);
        }

        static void process_AsyncOutputStream_OutputDataReceived2(object sender, DataReceivedEventArgs e)
        {
            s_process_AsyncOutputStream_sb.Append(e.Data);
            Process p = sender as Process;
            p.CancelOutputRead();
        }

        [Fact]
        public static void Process_SyncStreams()
        {
            // Check whether the input streams works correctly.

            //1. Check with RedirectStandardInut
            Process p = CreateProcessInput();
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.OutputDataReceived += process_SyncStreams_OutputDataReceived;
            p.Start();
            if (!p.HasExited)
            {
                using (StreamWriter writer = p.StandardInput)
                {
                    string str = "This string should come as output";
                    writer.WriteLine(str);
                }
                // Check that we get this as output
            }
        }

        public static void process_SyncStreams_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                Assert.Equal(e.Data, "This string should come as output");
            }
        }
    }
}
