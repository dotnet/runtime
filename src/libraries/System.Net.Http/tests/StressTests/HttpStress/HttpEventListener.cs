// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Channels;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace HttpStress
{
    public sealed class LogHttpEventListener : EventListener
    {
        private int _lastLogNumber = 0;
        private StreamWriter _log;
        private Channel<string> _messagesChannel = Channel.CreateUnbounded<string>();
        private Task _processMessages;
        private CancellationTokenSource _stopProcessing;

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

            _messagesChannel = Channel.CreateUnbounded<string>();
            _processMessages = ProcessMessagesAsync();
            _stopProcessing = new CancellationTokenSource();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http")
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

        private async Task ProcessMessagesAsync()
        {
            await Task.Yield();

            try
            {
                int i = 0;
                await foreach (string message in _messagesChannel.Reader.ReadAllAsync(_stopProcessing.Token))
                {
                    if ((++i % 10_000) == 0)
                    {
                        RotateFiles();
                    }

                    _log.WriteLine(message);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            void RotateFiles()
            {
                // Rotate the log if it reaches 50 MB size.
                if (_log.BaseStream.Length > (50 << 20))
                {
                    _log.Close();
                    _log = new StreamWriter($"client_{++_lastLogNumber:000}.log", false) { AutoFlush = true };
                }
            }
        }

        protected override async void OnEventWritten(EventWrittenEventArgs eventData)
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
            await _messagesChannel.Writer.WriteAsync(sb.ToString());
        }

        public override void Dispose()
        {
            base.Dispose();

            if (!_processMessages.Wait(TimeSpan.FromSeconds(30)))
            {
                _stopProcessing.Cancel();
                _processMessages.Wait();
            }
            _log.Dispose();
        }
    }
}
