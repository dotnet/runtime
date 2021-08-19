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
using System.Runtime.InteropServices;
using System.Buffers;
using Microsoft.Extensions.ObjectPool;

namespace HttpStress
{
    public sealed class LogHttpEventListener : EventListener
    {
        public const string LogDirectory = "logs";

        private int _lastLogNumber = 0;
        private FileStream _log;
        private Channel<string> _messagesChannel = Channel.CreateUnbounded<string>();
        private Task _processMessages;
        private DefaultObjectPool<StringBuilder> _stringBuilderPool = new DefaultObjectPool<StringBuilder>(new StringBuilderPooledObjectPolicy());

        private FileStream CreateNextLogFileStream()
        {
            string fn = Path.Combine(LogDirectory, $"client_{++_lastLogNumber:000}.log");
            if (File.Exists(fn))
            {
                File.Delete(fn);
            }
            return new FileStream(fn, FileMode.CreateNew, FileAccess.Write);
        }

        public LogHttpEventListener()
        {
            if (!Directory.Exists(LogDirectory))
            {
                Directory.CreateDirectory(LogDirectory);
            }

            foreach (string filename in Directory.GetFiles(LogDirectory, "client*.log"))
            {
                try
                {
                    File.Delete(filename);
                } catch {}
            }
            _log = CreateNextLogFileStream();
            _messagesChannel = Channel.CreateUnbounded<string>();
            _processMessages = ProcessMessagesAsync();
        }

        protected override void OnEventSourceCreated(EventSource eventSource)
        {
            if (eventSource.Name == "Private.InternalDiagnostics.System.Net.Http" ||
                eventSource.Name == "Private.InternalDiagnostics.System.Net.Quic")
            {
                EnableEvents(eventSource, EventLevel.LogAlways);
            }
        }

        private async Task ProcessMessagesAsync()
        {
            byte[] buffer = new byte[8192];
            var encoding = Encoding.ASCII;

            int i = 0;
            await foreach (string message in _messagesChannel.Reader.ReadAllAsync())
            {
                if ((++i % 10_000) == 0)
                {
                    await RotateFiles();
                }
                int maxLen = encoding.GetMaxByteCount(message.Length);
                if (maxLen > buffer.Length)
                {
                    buffer = new byte[maxLen];
                }
                int byteCount = encoding.GetBytes(message, buffer);

                await _log.WriteAsync(buffer.AsMemory(0, byteCount));
            }

            async ValueTask RotateFiles()
            {
                await _log.FlushAsync();
                // Rotate the log if it reaches 50 MB size.
                if (_log.Length > (100 << 20))
                {
                    await _log.DisposeAsync();
                    _log = CreateNextLogFileStream();
                }
            }
        }

        protected override async void OnEventWritten(EventWrittenEventArgs eventData)
        {
            StringBuilder sb = _stringBuilderPool.Get();
            sb.Append($"{eventData.TimeStamp:HH:mm:ss.fffffff}[{eventData.EventName}] ");
            for (int i = 0; i < eventData.Payload?.Count; i++)
            {
                if (i > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(eventData.PayloadNames?[i]).Append(": ").Append(eventData.Payload[i]);
            }
            sb.Append(Environment.NewLine);
            await _messagesChannel.Writer.WriteAsync(sb.ToString());
            _stringBuilderPool.Return(sb);
        }

        public override void Dispose()
        {
            base.Dispose();
            _log.Flush();
            _messagesChannel.Writer.Complete();
            _processMessages.Wait();
            _log.Dispose();
        }
    }
}
