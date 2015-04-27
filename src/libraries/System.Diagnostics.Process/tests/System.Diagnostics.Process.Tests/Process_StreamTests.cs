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

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
        public void Process_SyncErrorStream()
        {
            Process p = CreateProcessError();
            p.StartInfo.RedirectStandardError = true;
            p.Start();
            Assert.Equal(p.StandardError.ReadToEnd(), TestExeName + " error stream" + Environment.NewLine);
            Assert.True(p.WaitForExit(WaitInMS));
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
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

            Assert.Equal(TestExeName + " error stream", sb.ToString());
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
        public void Process_SyncOutputStream()
        {
            Process p = CreateProcessStream();
            p.StartInfo.RedirectStandardOutput = true;
            p.Start();
            string s = p.StandardOutput.ReadToEnd();
            Assert.True(p.WaitForExit(WaitInMS));
            Assert.Equal(s, TestExeName + " started" + Environment.NewLine + TestExeName + " closed" + Environment.NewLine);
        }

        [Fact, ActiveIssue(1538, PlatformID.OSX)]
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

                Assert.Equal(sb.ToString(), TestExeName + " started" + TestExeName + " closed");
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

                Assert.Equal(sb.ToString(), TestExeName + " started");
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
            Assert.True(p.WaitForExit(WaitInMS));
        }
    }
}
