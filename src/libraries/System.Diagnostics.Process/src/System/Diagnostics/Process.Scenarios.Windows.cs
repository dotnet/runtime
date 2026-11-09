// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using SafeJobHandle = Interop.Kernel32.SafeJobHandle;

namespace System.Diagnostics
{
    public partial class Process
    {
        private static ProcessExitStatus RunWithEntireProcessTree(ProcessStartInfo startInfo, TimeSpan timeout)
        {
            using SafeJobHandle job = SafeJobHandle.Create(Interop.Kernel32.JOB_OBJECT_LIMIT_BREAKAWAY_OK);
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo, fallbackToNull: startInfo.StartDetached, processTreeJob: job);
            return processHandle.WaitForExitOrKillOnTimeout(timeout, job.Terminate);
        }

        private static async Task<ProcessExitStatus> RunWithEntireProcessTreeAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            using SafeJobHandle job = SafeJobHandle.Create(Interop.Kernel32.JOB_OBJECT_LIMIT_BREAKAWAY_OK);
            using SafeProcessHandle processHandle = SafeProcessHandle.Start(startInfo, fallbackToNull: startInfo.StartDetached, processTreeJob: job);
            return await processHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken, job.Terminate).ConfigureAwait(false);
        }

        private static ProcessTextOutput RunAndCaptureTextWithEntireProcessTree(ProcessStartInfo startInfo, TimeSpan? timeout, long startTimestamp)
        {
            using SafeJobHandle job = SafeJobHandle.Create(Interop.Kernel32.JOB_OBJECT_LIMIT_BREAKAWAY_OK);
            using Process process = StartForCapture(startInfo, job);

            return RunAndCaptureTextCore(process, timeout, startTimestamp, job.Terminate);
        }

        private static async Task<ProcessTextOutput> RunAndCaptureTextWithEntireProcessTreeAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            using SafeJobHandle job = SafeJobHandle.Create(Interop.Kernel32.JOB_OBJECT_LIMIT_BREAKAWAY_OK);
            using Process process = StartForCapture(startInfo, job);

            return await RunAndCaptureTextAsyncCore(process, cancellationToken, job.Terminate).ConfigureAwait(false);
        }

        private static Process StartForCapture(ProcessStartInfo startInfo, SafeJobHandle job)
        {
            Process process = new() { StartInfo = startInfo };
            try
            {
                process.Start(job);
                return process;
            }
            catch
            {
                if (process._haveProcessHandle)
                {
                    // Preserve the startup exception if stream initialization failed after process creation.
                    try { job.Terminate(); } catch { }
                }
                process.Dispose();
                throw;
            }
        }
    }
}
