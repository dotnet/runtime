// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Output;

internal sealed class StressMessageWriter(IThreadNameOutput threadOutput, TimeTracker timeTracker, Target target, bool writeFormatString, TextWriter output) : IStressMessageOutput
{
    private StressMessageFormatter formatter = new(target, new DefaultSpecialPointerFormatter());

    public async Task OutputLineAsync(string line) => await output.WriteLineAsync(line).ConfigureAwait(false);

    public async Task OutputMessageAsync(ThreadStressLogData thread, StressMsgData message)
    {
        await output.WriteAsync(threadOutput.GetThreadName(thread.ThreadId)).ConfigureAwait(false);
        string timeOutput = timeTracker.TicksToSecondsFromStart(message.Timestamp).ToString("###0.000000000").PadLeft(13);
        await output.WriteAsync($" {timeOutput} : ").ConfigureAwait(false);

        await output.WriteAsync(GetFacility((LogFacility)message.Facility).PadRight(20)).ConfigureAwait(false);

        await output.WriteAsync($" ").ConfigureAwait(false);

        if (writeFormatString)
        {
            string format = target.ReadZeroTerminatedAsciiString(message.FormatString, 1024);
            await output.WriteAsync($"***|\"{format}\"|*** ").ConfigureAwait(false);
        }

        await output.WriteLineAsync(formatter.GetFormattedMessage(message)).ConfigureAwait(false);
    }

    private static string GetFacility(LogFacility facility)
    {
        if (facility == unchecked((LogFacility)(-1)))
        {
            return "`LF_ALL`";
        }
        else if ((facility & (LogFacility.ALWAYS | (LogFacility)0xfffe | LogFacility.GC)) == (LogFacility.ALWAYS | LogFacility.GC))
        {
            // specially encoded GC message including dprintf level
            return $"`GC l={((uint)facility >> 16) & 0x7fff}`";
        }
        else
        {
            return $"`{facility}`".Replace(", ", "`");
        }
    }
}
