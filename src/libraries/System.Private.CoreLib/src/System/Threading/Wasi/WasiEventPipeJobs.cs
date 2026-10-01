// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Threading
{
    // Runs the native EventPipe jobs (session streaming, diagnostic server) that browser schedules with
    // setTimeout. WASI has no host event loop, so the native side keeps a job list and WasiEventLoop
    // starts this pump when it sees pending jobs. Jobs only run while the event loop is being pumped.
    internal static partial class WasiEventPipeJobs
    {
        // Matches the browser re-schedule interval for unfinished jobs.
        private const int PumpIntervalMs = 100;

        private static bool s_pumpRunning;

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_WasiHasPendingJobs")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool HasPendingJobs();

        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_WasiRunJobs")]
        private static partial void RunJobs();

        internal static void EnsurePumpIfPending()
        {
            if (s_pumpRunning || !HasPendingJobs())
            {
                return;
            }

            s_pumpRunning = true;
            _ = PumpAsync();
        }

        private static async Task PumpAsync()
        {
            try
            {
                while (true)
                {
                    RunJobs();
                    if (!HasPendingJobs())
                    {
                        break;
                    }

                    await Task.Delay(PumpIntervalMs).ConfigureAwait(false);
                }
            }
            finally
            {
                s_pumpRunning = false;
            }
        }
    }
}
