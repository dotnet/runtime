// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics
{
    public partial class Process
    {
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private static ProcessExitStatus RunWithEntireProcessTree(ProcessStartInfo startInfo, TimeSpan timeout)
        {
            using Process process = Start(startInfo)!;
            return process.SafeHandle.WaitForExitOrKillOnTimeout(timeout, process.KillEntireProcessTree);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private static async Task<ProcessExitStatus> RunWithEntireProcessTreeAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            using Process process = Start(startInfo)!;
            return await process.SafeHandle.WaitForExitOrKillOnCancellationAsync(cancellationToken, process.KillEntireProcessTree).ConfigureAwait(false);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private static ProcessTextOutput RunAndCaptureTextWithEntireProcessTree(ProcessStartInfo startInfo, TimeSpan? timeout, long startTimestamp)
        {
            using Process process = Start(startInfo)!;
            return RunAndCaptureTextCore(process, timeout, startTimestamp, process.KillEntireProcessTree);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private static async Task<ProcessTextOutput> RunAndCaptureTextWithEntireProcessTreeAsync(ProcessStartInfo startInfo, CancellationToken cancellationToken)
        {
            using Process process = Start(startInfo)!;
            return await RunAndCaptureTextAsyncCore(process, cancellationToken, process.KillEntireProcessTree).ConfigureAwait(false);
        }

        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        [SupportedOSPlatform("maccatalyst")]
        private void KillEntireProcessTree()
        {
            try
            {
                Kill(entireProcessTree: true);
            }
            catch
            {
                // In case killing the process tree fails, attempt to Kill the root.
                Kill();
            }
        }
    }
}
