// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Wasm.Build.Tests
{
    internal static class ProcessExtensions
    {
        private static int RunProcessAndWaitForExit(string fileName, string arguments, TimeSpan timeout, out string stdout)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false
            };

            var process = Process.Start(startInfo);

            stdout = null;
            if (process.WaitForExit((int)timeout.TotalMilliseconds))
            {
                stdout = process.StandardOutput.ReadToEnd();
                return process.ExitCode;
            }

            process.Kill(entireProcessTree: true);
            // sigkill - 128+9(sigkill)
            return 137;
        }

        public static Task StartAndWaitForExitAsync(this Process subject)
        {
            var taskCompletionSource = new TaskCompletionSource<object>();

            subject.EnableRaisingEvents = true;

            subject.Exited += (s, a) =>
            {
                taskCompletionSource.SetResult(null);
            };

            subject.Start();

            return taskCompletionSource.Task;
        }
    }
}
