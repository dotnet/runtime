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
        private readonly Queue<LogMessageEntry> _messageQueue;
        private volatile int _messagesDropped;
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
        private readonly Thread _outputThread;

        public IConsole Console { get; }
        public IConsole ErrorConsole { get; }

        public ConsoleLoggerProcessor(IConsole console, IConsole errorConsole, ConsoleLoggerBufferFullMode fullMode, int maxQueueLength)
        {
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
        }

        public virtual void EnqueueMessage(LogMessageEntry message)
        {
            // cannot enqueue when adding is completed
            if (!Enqueue(message))
            {
                WriteMessage(message);
            }
        }

        // internal for testing
        internal void WriteMessage(LogMessageEntry entry)
        {
            try
            {
                IConsole console = entry.LogAsError ? ErrorConsole : Console;
                console.Write(entry.Message);
            }
            catch
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
                        _messagesDropped++;
                        return true;
                    }

                    Debug.Assert(FullMode == ConsoleLoggerBufferFullMode.Wait);
                    Monitor.Wait(_messageQueue);
                }

                if (!_isAddingCompleted)
                {
                    Debug.Assert(_messageQueue.Count < MaxQueueLength);
                    bool startedEmpty = _messageQueue.Count == 0;
                    if (_messagesDropped > 0)
                    {
                        _messageQueue.Enqueue(new LogMessageEntry(
                            message: _messagesDropped + SR.WarningMessageOnDrop + Environment.NewLine,
                            logAsError: true
                        ));

                        _messagesDropped = 0;
                    }

                    // if we just logged the dropped message warning this could push the queue size to
                    // MaxLength + 1, that shouldn't be a problem. It won't grow any further until it is less than
                    // MaxLength once again.
                    _messageQueue.Enqueue(item);

                    // if the queue started empty it could be at 1 or 2 now
                    if (startedEmpty)
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
    }
}
