// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Runtime.Versioning;
using System.Threading;

namespace Microsoft.Extensions.Logging.Console
{
    [UnsupportedOSPlatform("browser")]
    internal class ConsoleLoggerProcessor : IDisposable
    {
        private static readonly Meter s_meter = new Meter("Microsoft-Extension-Logging-Console-Queue", "1.0.0");
        private readonly Queue<LogMessageEntry> _messageQueue;
        private int _messagesDropped;
        private bool _isAddingCompleted;
        private int _maxQueuedMessages = ConsoleLoggerOptions.DefaultMaxQueueLengthValue;
        public int MaxQueueLength
        {
            get => _maxQueuedMessages;
            set
            {
                if (value <= 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} must be larger than zero.");
                }

                lock (_messageQueue)
                {
                    _maxQueuedMessages = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        private ConsoleLoggerBufferFullMode _fullMode = ConsoleLoggerBufferFullMode.Wait;
        public ConsoleLoggerBufferFullMode FullMode
        {
            get => _fullMode;
            set
            {
                if (value != ConsoleLoggerBufferFullMode.Wait && value != ConsoleLoggerBufferFullMode.DropWrite)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, $"{nameof(value)} is not a supported buffer mode value.");
                }
                _fullMode = value;
            }
        }
        private readonly string _queueName;
        private readonly Thread _outputThread;

        public IConsole Console { get; }
        public IConsole ErrorConsole { get; }

        public ConsoleLoggerProcessor(string queueName, IConsole console, IConsole errorConsole, ConsoleLoggerBufferFullMode fullMode, int maxQueueLength)
        {
            _queueName = queueName;
            _messageQueue = new Queue<LogMessageEntry>();
            FullMode = fullMode;
            MaxQueueLength = maxQueueLength;
            Console = console;
            ErrorConsole = errorConsole;
            // Start Console message queue processor
            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "Console logger queue processing thread"
            };
            _outputThread.Start();
            s_meter.CreateObservableGauge<long>("queue-size", GetQueueSize);
        }

        public virtual void EnqueueMessage(LogMessageEntry message)
        {
            // cannot enqueue when adding is completed
            if (!Enqueue(message))
            {
                WriteMessage(message);
            }
        }

        // for testing
        internal void WriteMessage(LogMessageEntry entry)
        {
            try
            {
                var messagesDropped = Interlocked.Exchange(ref _messagesDropped, 0);
                if (messagesDropped != 0)
                {
                    System.Console.Error.WriteLine($"{messagesDropped} message(s) dropped because of queue size limit. Increase the queue size or decrease logging verbosity to avoid this.{Environment.NewLine}");
                }

                IConsole console = entry.LogAsError ? ErrorConsole : Console;
                console.Write(entry.Message);
            }
            catch (Exception)
            {
                CompleteAdding();
            }
        }

        private void ProcessLogQueue()
        {
            while (!_isAddingCompleted || _messageQueue.Count > 0)
            {
                if (TryDequeue(out LogMessageEntry message))
                {
                    WriteMessage(message);
                }
            }
        }

        public bool Enqueue(LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count >= MaxQueueLength && !_isAddingCompleted)
                {
                    if (FullMode == ConsoleLoggerBufferFullMode.DropWrite)
                    {
                        Interlocked.Increment(ref _messagesDropped);
                        return true;
                    }

                    Debug.Assert(FullMode == ConsoleLoggerBufferFullMode.Wait);
                    Monitor.Wait(_messageQueue);
                }

                if (_messageQueue.Count < MaxQueueLength)
                {
                    _messageQueue.Enqueue(item);

                    if (_messageQueue.Count == 1)
                    {
                        // pulse for wait in Dequeue
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }
            }

            return false;
        }

        public bool TryDequeue(out LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count == 0 && !_isAddingCompleted)
                {
                    Monitor.Wait(_messageQueue);
                }

                if (_messageQueue.Count > 0)
                {
                    item = _messageQueue.Dequeue();
                    if (_messageQueue.Count == MaxQueueLength - 1)
                    {
                        // pulse for wait in Enqueue
                        Monitor.PulseAll(_messageQueue);
                    }

                    return true;
                }

                item = default;
                return false;
            }
        }

        public void Dispose()
        {
            CompleteAdding();

            try
            {
                _outputThread.Join(1500); // with timeout in-case Console is locked by user input
            }
            catch (ThreadStateException) { }
        }

        private void CompleteAdding()
        {
            lock (_messageQueue)
            {
                Monitor.PulseAll(_messageQueue);
                _isAddingCompleted = true;
            }
        }

        private IEnumerable<Measurement<long>> GetQueueSize()
        {
            return new Measurement<long>[]
            {
                new Measurement<long>(_messageQueue.Count, new KeyValuePair<string, object?>("queue-name", _queueName)),
            };
        }
    }
}
