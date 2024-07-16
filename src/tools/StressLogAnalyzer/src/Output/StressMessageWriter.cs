// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using Microsoft.Diagnostics.DataContractReader;
using Microsoft.Diagnostics.DataContractReader.Contracts;

namespace StressLogAnalyzer.Output;

internal class StressMessageWriter(IThreadNameOutput threadOutput, TimeTracker timeTracker, Target target, bool writeFormatString, TextWriter output) : IStressMessageOutput
{
    private StressMessageFormatter formatter = new(target, new DefaultSpecialPointerFormatter());

    public async Task OutputLineAsync(string line) => await output.WriteLineAsync(line).ConfigureAwait(false);

    public async Task OutputMessageAsync(ThreadStressLogData thread, StressMsgData message)
    {
        await output.WriteAsync(threadOutput.GetThreadName(thread.ThreadId)).ConfigureAwait(false);
        await output.WriteAsync($" {timeTracker.TicksToSecondsFromStart(message.Timestamp):0000.000000000} : ").ConfigureAwait(false);

        await WriteFacility((LogFacility)message.Facility).ConfigureAwait(false);

        if (writeFormatString)
        {
            string format = ReadZeroTerminatedString(message.FormatString, 256);
            await output.WriteAsync($"***|\"{format}\"|*** ").ConfigureAwait(false);
        }

        await output.WriteLineAsync(formatter.GetFormattedMessage(message)).ConfigureAwait(false);
    }

    private async Task WriteFacility(LogFacility facility)
    {
        if (facility == unchecked((LogFacility)(-1)))
        {
            await output.WriteAsync("LF_ALL").ConfigureAwait(false);
        }
        else if ((facility & (LogFacility.LF_ALWAYS | (LogFacility)0xfffe | LogFacility.LF_GC)) == (LogFacility.LF_ALWAYS | LogFacility.LF_GC))
        {
            // specially encoded GC message including dprintf level
            await output.WriteAsync($"`GC l={((uint)facility >> 16) & 0x7fff}`").ConfigureAwait(false);
        }
        else
        {
            await output.WriteAsync(facility.ToString()).ConfigureAwait(false);
        }
    }

    private unsafe string ReadZeroTerminatedString(TargetPointer pointer, int maxLength)
    {
        StringBuilder sb = new();
        for (byte ch = target.Read<byte>(pointer);
        ch != 0;
            ch = target.Read<byte>(pointer = new TargetPointer((ulong)pointer + 1)))
        {
            if (sb.Length > maxLength)
            {
                break;
            }

            sb.Append((char)ch);
        }
        return sb.ToString();
    }
}
