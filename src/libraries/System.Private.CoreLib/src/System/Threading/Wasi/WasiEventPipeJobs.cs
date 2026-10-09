// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace System.Threading
{
    // Runs the native EventPipe jobs (session streaming, diagnostic server) that browser schedules with
    // setTimeout. WASI has no host event loop, so the native side keeps the job queue and WasiEventLoop
    // calls Pump on each iteration. Jobs only run while the event loop is being pumped.
    internal static partial class WasiEventPipeJobs
    {
        // Matches the browser re-schedule interval for unfinished jobs.
        private const int PumpIntervalMs = 100;

        private static bool s_pumpRunning;

        // Runs the queued jobs and returns true if any remain queued.
        [ErrorHandler(typeof(QCallExceptionStatusMarshaller), ErrorLocation.HiddenLastParameter)]
        [LibraryImport(RuntimeHelpers.QCall, EntryPoint = "EventPipeInternal_WasiRunJobs")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RunJobs();

        internal static void Pump()
        {
            if (s_pumpRunning)
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
                while (RunJobs())
                {
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
