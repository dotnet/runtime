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
                    throw new ArgumentOutOfRangeException(SR.Format(SR.MaxQueueLengthBadValue, nameof(value)));
                }

                lock (_messageQueue)
                {
                    _maxQueuedMessages = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        private ConsoleLoggerQueueFullMode _fullMode = ConsoleLoggerQueueFullMode.Wait;
        public ConsoleLoggerQueueFullMode FullMode
        {
            get => _fullMode;
            set
            {
                if (value != ConsoleLoggerQueueFullMode.Wait && value != ConsoleLoggerQueueFullMode.DropWrite)
                {
                    throw new ArgumentOutOfRangeException(SR.Format(SR.QueueModeNotSupported, nameof(value)));
                }

                lock (_messageQueue)
                {
                    // _fullMode is used inside the lock and is safer to guard setter with lock as well
                    // this set is not expected to happen frequently
                    _fullMode = value;
                    Monitor.PulseAll(_messageQueue);
                }
            }
        }
        private readonly Thread _outputThread;

        public IConsole Console { get; }
        public IConsole ErrorConsole { get; }

        public ConsoleLoggerProcessor(IConsole console, IConsole errorConsole, ConsoleLoggerQueueFullMode fullMode, int maxQueueLength)
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
            while (TryDequeue(out LogMessageEntry message))
            {
                WriteMessage(message);
            }
        }

        public bool Enqueue(LogMessageEntry item)
        {
            lock (_messageQueue)
            {
                while (_messageQueue.Count >= MaxQueueLength && !_isAddingCompleted)
                {
                    if (FullMode == ConsoleLoggerQueueFullMode.DropWrite)
                    {
                        _messagesDropped++;
                        return true;
                    }

                    Debug.Assert(FullMode == ConsoleLoggerQueueFullMode.Wait);
                    Monitor.Wait(_messageQueue);
                }

                if (!_isAddingCompleted)
                {
                    Debug.Assert(_messageQueue.Count < MaxQueueLength);
                    bool startedEmpty = _messageQueue.Count == 0;
                    if (_messagesDropped > 0)
                    {
                        _messageQueue.Enqueue(new LogMessageEntry(
                            message: SR.Format(SR.WarningMessageOnDrop + Environment.NewLine, _messagesDropped),
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

                if (_messageQueue.Count > 0 && !_isAddingCompleted)
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
                _isAddingCompleted = true;
                Monitor.PulseAll(_messageQueue);
            }
        }
    }
}
