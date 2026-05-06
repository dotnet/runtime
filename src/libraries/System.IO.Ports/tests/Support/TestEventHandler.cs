// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using Xunit;

namespace Legacy.Support
{
    /// <summary>
    /// Base class for all test event handlers
    /// </summary>
    /// <typeparam name="T">The type of the EventType object which is passed to the event handler</typeparam>
    public class TestEventHandler<T>
    {
        private readonly List<T> _eventTypes = new List<T>();
        private readonly List<int> _bytesToRead = new List<int>();
        private readonly List<SerialPort> _sources = new List<SerialPort>();
        private readonly SerialPort _com;
        private readonly bool _shouldThrow;
        private readonly bool _shouldWait;
        private readonly AutoResetEvent _eventHandlerWait = new AutoResetEvent(false);
        private readonly object _lock = new object();
        private bool _successfulWait;

        public int NumEventsHandled { get; private set; }
        public bool SuccessfulWait => !_shouldWait || _successfulWait;

        /// <summary>
        /// If you set this filter, then it must return 'true' to record an event
        /// </summary>
        public Predicate<T> EventFilter { get; set; }

        protected TestEventHandler(SerialPort com, bool shouldThrow, bool shouldWait)
        {
            if (shouldThrow && shouldWait)
            {
                throw new ArgumentException("shouldThrow and shouldWait can not both be true");
            }

            _com = com;
            _shouldThrow = shouldThrow;
            _shouldWait = shouldWait;
        }

        protected void HandleEvent(object source, T eventType)
        {
            int bytesToRead = _com.BytesToRead;

            Debug.Print("EventHandler: Handling {0}", eventType);

            if (EventFilter != null)
            {
                if (!EventFilter(eventType))
                {
                    return;
                }
            }

            lock (_lock)
            {
                _bytesToRead.Add(bytesToRead);
                _eventTypes.Add(eventType);
                _sources.Add((SerialPort)source);

                NumEventsHandled++;
                Monitor.Pulse(_lock);
            }

            if (_shouldThrow)
            {
                throw new Exception("I was told to throw");
            }

            if (_shouldWait)
            {
                _successfulWait = _eventHandlerWait.WaitOne(10000);
            }
        }

        public void ResumeHandleEvent()
        {
            _eventHandlerWait.Set();
        }

        private void RemoveAt(int index)
        {
            lock (_lock)
            {
                _eventTypes.RemoveAt(index);
                _bytesToRead.RemoveAt(index);
                _sources.RemoveAt(index);
                NumEventsHandled--;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _eventTypes.Clear();
                _bytesToRead.Clear();
                _sources.Clear();

                NumEventsHandled = 0;
            }
        }

        public bool WaitForEvent(int maxMilliseconds, int totalNumberOfEvents)
        {
            Stopwatch sw = new Stopwatch();

            lock (_lock)
            {
                sw.Start();
                long remaining;
                while ((remaining = (maxMilliseconds - sw.ElapsedMilliseconds)) > 0 && NumEventsHandled < totalNumberOfEvents)
                {
                    Monitor.Wait(_lock, (int)remaining);
                }
                Debug.Print("WaitForEvent: Events handled {0}/{1}", NumEventsHandled, totalNumberOfEvents);
                return totalNumberOfEvents <= NumEventsHandled;
            }
        }

        // Since we can not guarantee the order or the exact time that the event handler is called
        // We will look for an event that was fired that matches the type and that bytesToRead
        // is greater then the parameter
        public void Validate(T eventType, int bytesToRead)
        {
            lock (_lock)
            {
                for (int i = 0; i < _eventTypes.Count; i++)
                {
                    if (Equals(eventType, _eventTypes[i]) && bytesToRead <= _bytesToRead[i] && _sources[i] == _com)
                    {
                        Debug.Print("Validate - found {0} at {1} {2}", eventType, i, string.Join(",", _eventTypes));
                        RemoveAt(i);
                        return;
                    }
                }
            }
            Assert.Fail($"Failed to validate event type {eventType}. Received: {string.Join(", ", _eventTypes)}");
        }

        public int NumberOfOccurrencesOfType(T eventType)
        {
            lock (_lock)
            {
                return _eventTypes.Count(et => Equals(et, eventType));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < _eventTypes.Count; i++)
            {
                sb.Append(i);
                sb.Append(": Type: ");
                sb.Append(_eventTypes[i]);
                sb.Append(" BytesToRead: ");
                sb.Append(_eventTypes[i]);
                sb.Append("\n");
            }

            return sb.ToString();
        }
    }
}
