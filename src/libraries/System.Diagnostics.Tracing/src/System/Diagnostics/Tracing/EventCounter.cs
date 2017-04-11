// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
#if ES_BUILD_PCL
    using System.Threading.Tasks;
#endif

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing
#else
namespace System.Diagnostics.Tracing
#endif
{
    /// <summary>
    /// Provides the ability to collect statistics through EventSource
    /// </summary>
    public class EventCounter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCounter"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="eventSource">The event source.</param>
        public EventCounter(string name, EventSource eventSource)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            if (eventSource == null)
            {
                throw new ArgumentNullException(nameof(eventSource));
            }

            InitializeBuffer();
            _name = name;
            EventCounterGroup.AddEventCounter(eventSource, this);
        }

        /// <summary>
        /// Writes the metric.
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteMetric(float value)
        {
            Enqueue(value);
        }

        #region private implementation

        private readonly string _name;

        #region Buffer Management

        // Values buffering
        private const int BufferedSize = 10;
        private const float UnusedBufferSlotValue = float.NegativeInfinity;
        private const int UnsetIndex = -1;
        private volatile float[] _bufferedValues; 
        private volatile int _bufferedValuesIndex;

        private void InitializeBuffer()
        {
            _bufferedValues = new float[BufferedSize];
            for (int i = 0; i < _bufferedValues.Length; i++)
            {
                _bufferedValues[i] = UnusedBufferSlotValue;
            }
        }

        private void Enqueue(float value)
        {
            // It is possible that two threads read the same bufferedValuesIndex, but only one will be able to write the slot, so that is okay.
            int i = _bufferedValuesIndex;
            while (true)
            {
                float result = Interlocked.CompareExchange(ref _bufferedValues[i], value, UnusedBufferSlotValue);
                i++;
                if (_bufferedValues.Length <= i)
                {
                    // It is possible that two threads both think the buffer is full, but only one get to actually flush it, the other
                    // will eventually enter this code path and potentially calling Flushing on a buffer that is not full, and that's okay too.
                    lock (_bufferedValues)
                    {
                        Flush();
                    }
                    i = 0;
                }

                if (result == UnusedBufferSlotValue)
                {
                    // CompareExchange succeeded 
                    _bufferedValuesIndex = i;
                    return;
                }
            }
        }

        private void Flush()
        {
            for (int i = 0; i < _bufferedValues.Length; i++)
            {
                var value = Interlocked.Exchange(ref _bufferedValues[i], UnusedBufferSlotValue);
                if (value != UnusedBufferSlotValue)
                {
                    OnMetricWritten(value);
                }
            }

            _bufferedValuesIndex = 0;
        }

        #endregion // Buffer Management

        #region Statistics Calculation

        // Statistics
        private int _count;
        private float _sum;
        private float _sumSquared;
        private float _min;
        private float _max;

        private void OnMetricWritten(float value)
        {
            _sum += value;
            _sumSquared += value * value;
            if (_count == 0 || value > _max)
            {
                _max = value;
            }

            if (_count == 0 || value < _min)
            {
                _min = value;
            }

            _count++;
        }

        internal EventCounterPayload GetEventCounterPayload()
        {
            lock (_bufferedValues)
            {
                Flush();
                EventCounterPayload result = new EventCounterPayload();
                result.Name = _name;
                result.Count = _count;
                result.Mean = _sum / _count;
                result.StandardDeviation = (float)Math.Sqrt(_sumSquared / _count - _sum * _sum / _count / _count);
                result.Min = _min;
                result.Max = _max;
                ResetStatistics();
                return result;
            }
        }

        private void ResetStatistics()
        {
            _count = 0;
            _sum = 0;
            _sumSquared = 0;
            _min = 0;
            _max = 0;
        }

        #endregion // Statistics Calculation

        #endregion // private implementation
    }

    #region internal supporting classes

    [EventData]
    internal class EventCounterPayload : IEnumerable<KeyValuePair<string, object>>
    {
        public string Name { get; set; }

        public float Mean { get; set; }

        public float StandardDeviation { get; set; }

        public int Count { get; set; }

        public float Min { get; set; }

        public float Max { get; set; }

        public float IntervalSec { get; internal set; }

        #region Implementation of the IEnumerable interface

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return ForEnumeration.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ForEnumeration.GetEnumerator();
        }

        private IEnumerable<KeyValuePair<string, object>> ForEnumeration
        {
            get
            {
                yield return new KeyValuePair<string, object>("Name", Name);
                yield return new KeyValuePair<string, object>("Mean", Mean);
                yield return new KeyValuePair<string, object>("StandardDeviation", StandardDeviation);
                yield return new KeyValuePair<string, object>("Count", Count);
                yield return new KeyValuePair<string, object>("Min", Min);
                yield return new KeyValuePair<string, object>("Max", Max);
            }
        }

        #endregion // Implementation of the IEnumerable interface
    }

    internal class EventCounterGroup : IDisposable
    {
        private readonly EventSource _eventSource;
        private readonly int _eventSourceIndex;
        private readonly List<EventCounter> _eventCounters;

        internal EventCounterGroup(EventSource eventSource, int eventSourceIndex)
        {
            _eventSource = eventSource;
            _eventSourceIndex = eventSourceIndex;
            _eventCounters = new List<EventCounter>();
            RegisterCommandCallback();
        }

        private void Add(EventCounter eventCounter)
        {
            _eventCounters.Add(eventCounter);
        }

        #region EventSource Command Processing

        private void RegisterCommandCallback()
        {
#if SUPPORTS_EVENTCOMMANDEXECUTED
            _eventSource.EventCommandExecuted += OnEventSourceCommand;
#endif
        }

        private void OnEventSourceCommand(object sender, EventCommandEventArgs e)
        {
            if (e.Command == EventCommand.Enable || e.Command == EventCommand.Update)
            {
                string valueStr;
                float value;
                if (e.Arguments.TryGetValue("EventCounterIntervalSec", out valueStr) && float.TryParse(valueStr, out value))
                {
                    EnableTimer(value);
                }
            }
        }

        #endregion // EventSource Command Processing

        #region Global EventCounterGroup Array management

        private static EventCounterGroup[] s_eventCounterGroups;

        internal static void AddEventCounter(EventSource eventSource, EventCounter eventCounter)
        {
            int eventSourceIndex = EventListenerHelper.EventSourceIndex(eventSource);

            EventCounterGroup.EnsureEventSourceIndexAvailable(eventSourceIndex);
            EventCounterGroup eventCounterGroup = GetEventCounterGroup(eventSource);
            eventCounterGroup.Add(eventCounter);
        }

        private static void EnsureEventSourceIndexAvailable(int eventSourceIndex)
        {
            if (EventCounterGroup.s_eventCounterGroups == null)
            {
                EventCounterGroup.s_eventCounterGroups = new EventCounterGroup[eventSourceIndex + 1];
            }
            else if (eventSourceIndex >= EventCounterGroup.s_eventCounterGroups.Length)
            {
                EventCounterGroup[] newEventCounterGroups = new EventCounterGroup[eventSourceIndex + 1];
                Array.Copy(EventCounterGroup.s_eventCounterGroups, 0, newEventCounterGroups, 0, EventCounterGroup.s_eventCounterGroups.Length);
                EventCounterGroup.s_eventCounterGroups = newEventCounterGroups;
            }
        }

        private static EventCounterGroup GetEventCounterGroup(EventSource eventSource)
        {
            int eventSourceIndex = EventListenerHelper.EventSourceIndex(eventSource);
            EventCounterGroup result = EventCounterGroup.s_eventCounterGroups[eventSourceIndex];
            if (result == null)
            {
                result = new EventCounterGroup(eventSource, eventSourceIndex);
                EventCounterGroup.s_eventCounterGroups[eventSourceIndex] = result;
            }

            return result;
        }

        #endregion // Global EventCounterGroup Array management

        #region Timer Processing

        private DateTime _timeStampSinceCollectionStarted;
        private int _pollingIntervalInMilliseconds;
        private Timer _pollingTimer;

        private void EnableTimer(float pollingIntervalInSeconds)
        {
            if (pollingIntervalInSeconds == 0)
            {
                if (_pollingTimer != null)
                {
                    _pollingTimer.Dispose();
                    _pollingTimer = null;
                }

                _pollingIntervalInMilliseconds = 0;
            }
            else if (_pollingIntervalInMilliseconds == 0 || pollingIntervalInSeconds < _pollingIntervalInMilliseconds)
            {
                _pollingIntervalInMilliseconds = (int)(pollingIntervalInSeconds * 1000);
                if (_pollingTimer != null)
                {
                    _pollingTimer.Dispose();
                    _pollingTimer = null;
                }

                _timeStampSinceCollectionStarted = DateTime.Now;
                _pollingTimer = new Timer(OnTimer, null, _pollingIntervalInMilliseconds, _pollingIntervalInMilliseconds);
            }
        }

        private void OnTimer(object state)
        {
            if (_eventSource.IsEnabled())
            {
                DateTime now = DateTime.Now;
                TimeSpan elapsed = now - _timeStampSinceCollectionStarted;
                lock (_pollingTimer)
                {
                    foreach (var eventCounter in _eventCounters)
                    {
                        EventCounterPayload payload = eventCounter.GetEventCounterPayload();
                        payload.IntervalSec = (float)elapsed.TotalSeconds;
                        _eventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new { Payload = payload });
                    }


                    _timeStampSinceCollectionStarted = now;
                }
            }
            else
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
                EventCounterGroup.s_eventCounterGroups[_eventSourceIndex] = null;
            }
        }

        #region PCL timer hack

#if ES_BUILD_PCL
    internal delegate void TimerCallback(object state);

        internal sealed class Timer : CancellationTokenSource, IDisposable
        {
            private int _period;
            private TimerCallback _callback;
            private object _state;

            internal Timer(TimerCallback callback, object state, int dueTime, int period)
            {
                _callback = callback;
                _state = state;
                _period = period;
                Schedule(dueTime);
            }

            private void Schedule(int dueTime)
            {
                Task.Delay(dueTime, Token).ContinueWith(OnTimer, null, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
            }

            private void OnTimer(Task t, object s)
            {
                Schedule(_period);
                _callback(_state);
            }

            public new void Dispose() { base.Cancel(); }
        }
#endif
        #endregion // PCL timer hack

        #endregion // Timer Processing

        #region Implementation of the IDisposable interface

        private bool _disposed = false;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                if (_pollingTimer != null)
                {
                    _pollingTimer.Dispose();
                    _pollingTimer = null;
                }
            }

            _disposed = true;
        }

        #endregion // Implementation of the IDisposable interface
    }

    // This class a work-around because .NET V4.6.2 did not make EventSourceIndex public (it was only protected)
    // We make this helper class to get around that protection.   We want V4.6.3 to make this public at which
    // point this class is no longer needed and can be removed.
    internal class EventListenerHelper : EventListener {
        public new static int EventSourceIndex(EventSource eventSource) { return EventListener.EventSourceIndex(eventSource); }
        protected override void OnEventWritten(EventWrittenEventArgs eventData) { } // override abstact methods to keep compiler happy
    }

    #endregion // internal supporting classes
}
