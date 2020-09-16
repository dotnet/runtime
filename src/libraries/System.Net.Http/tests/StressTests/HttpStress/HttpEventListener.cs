// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Text;
using System.IO;
using System.Linq;

namespace HttpStress
{
    public sealed class LogHttpEventListener : EventListener
    {
        private readonly object _syncRoot = new object();
        private int _lastLogNumber = 0;
        private StreamWriter _log;

        public LogHttpEventListener()
        {
            foreach (var filename in Directory.GetFiles(".", "client*.log"))
            {
                try
                {
                    File.Delete(filename);
                } catch {}
            }
            _log = new StreamWriter("client.log", false) { AutoFlush = true };
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
            lock (_syncRoot)
            {
                // Rotate the log if it reaches 50 MB size.
                if (_log.BaseStream.Length > (50 << 20))
                {
                    _log.Close();
                    _log = new StreamWriter($"client_{++_lastLogNumber:000}.log", false) { AutoFlush = true };
                }

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
}
