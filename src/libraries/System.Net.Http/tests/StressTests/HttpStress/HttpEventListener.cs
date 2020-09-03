// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.IO;

namespace HttpStress
{
    public sealed class LogHttpEventListener : EventListener
    {
        private readonly StreamWriter _log;

        public LogHttpEventListener(string logPath)
        {
            _log = new StreamWriter(logPath, true) { AutoFlush = true };
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (_log)
            {
                var sb = new StringBuilder().Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
                for (int i = 0; i < eventData.Payload?.Count; i++)
                {
                    if (i > 0)
                    {
                        sb.Append(", ");
                    }
                    sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
                }
                _log.WriteLine(sb.ToString());
            }
        }

        public override void Dispose()
        {
            _log.Dispose();
            base.Dispose();
        }
    }

    public sealed class ConsoleHttpEventListener : EventListener
    {
        public ConsoleHttpEventListener()
        { }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

        protected override void OnEventWritten(EventWrittenEventArgs eventData)
        {
            lock (Console.Out)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.Write($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
                Console.ResetColor();
                for (int i = 0; i < eventData.Payload?.Count; i++)
                {
                    if (i > 0)
                    {
                        Console.Write(", ");
                    }
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write(eventData.PayloadNames?[i] + ": ");
                    Console.ResetColor();
                    Console.Write(eventData.Payload[i]);
                }
                Console.WriteLine();
            }
        }
    }
}
