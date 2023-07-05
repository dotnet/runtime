// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Tests
{
    public partial class ConsoleTests
    {
        [ConditionalFact(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        public void OpenStandardInput_AsyncRead_CompletesAsynchronouslyIfDataNotAvailable()
        {
            using RemoteInvokeHandle child = RemoteExecutor.Invoke(
                async () =>
                {
                    using Stream stdin = Console.OpenStandardInput();

                    // Start a read that isn't yet satisfiable
                    ValueTask<int> readTask = stdin.ReadAsync(new byte[1]);
                    Assert.False(readTask.IsCompleted);

                    // Indicate to the parent we're ready for data to read
                    Console.WriteLine("ready");

                    // Wait for the read to be satisfied
                    Assert.Equal(1, await readTask);
                }, new RemoteInvokeOptions { StartInfo = new ProcessStartInfo() { RedirectStandardInput = true, RedirectStandardOutput = true, RedirectStandardError = true } });

            // Wait for child to write to its stdout
            Assert.Equal("ready", child.Process.StandardOutput.ReadLine());

            // Unblock child's read
            child.Process.StandardInput.WriteLine("a");

            // Wait for child to exit
            child.Process.WaitForExit();
        }
    }
}
