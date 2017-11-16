// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
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
    /// 
    /// See https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Tracing/documentation/EventCounterTutorial.md
    /// for a tutorial guide.  
    /// 
    /// See https://github.com/dotnet/corefx/blob/master/src/System.Diagnostics.Tracing/tests/BasicEventSourceTest/TestEventCounter.cs
    /// which shows tests, which are also useful in seeing actual use.  
    /// </summary>
    public class EventCounter : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EventCounter"/> class.
        /// EVentCounters live as long as the EventSource that they are attached to unless they are
        /// explicitly Disposed.   
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
            _group = EventCounterGroup.GetEventCounterGroup(eventSource);
            _group.Add(this);
            _min = float.PositiveInfinity;
            _max = float.NegativeInfinity;
        }

        /// <summary>
        /// Writes 'value' to the stream of values tracked by the counter.  This updates the sum and other statistics that will 
        /// be logged on the next timer interval.  
        /// </summary>
        /// <param name="value">The value.</param>
        public void WriteMetric(float value)
        {
            Enqueue(value);
        }

        /// <summary>
        /// Removes the counter from set that the EventSource will report on.  After being disposed, this
        /// counter will do nothing and its resource will be reclaimed if all references to it are removed.
        /// If an EventCounter is not explicitly disposed it will be cleaned up automatically when the
        /// EventSource it is attached to dies.  
        /// </summary>
        public void Dispose()
        {
            var group = _group;
            if (group != null)
            {
                group.Remove(this);
                _group = null;
            }
        }

        public override string ToString()
        {
            return "EventCounter '" + _name + "' Count " + _count + " Mean " + (((double)_sum) / _count).ToString("n3");
        }
        #region private implementation

        private readonly string _name;
        private EventCounterGroup _group;

        #region Buffer Management

        // Values buffering
        private const int BufferedSize = 10;
        private const float UnusedBufferSlotValue = float.NegativeInfinity;
        private const int UnsetIndex = -1;
        private volatile float[] _bufferedValues;
        private volatile int _bufferedValuesIndex;

        // arbitrarily we use _bufferfValues as the lock object.  
        private object MyLock { get { return _bufferedValues; } }

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
                    lock (MyLock) // Lock the counter
                        Flush();
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
            Debug.Assert(Monitor.IsEntered(MyLock));
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
            Debug.Assert(Monitor.IsEntered(MyLock));
            _sum += value;
            _sumSquared += value * value;
            if (value > _max)
                _max = value;

            if (value < _min)
                _min = value;

            _count++;
        }

        internal EventCounterPayload GetEventCounterPayload()
        {
            lock (MyLock)     // Lock the counter
            {
                Flush();
                EventCounterPayload result = new EventCounterPayload();
                result.Name = _name;
                result.Count = _count;
                if (0 < _count)
                {
                    result.Mean = _sum / _count;
                    result.StandardDeviation = (float)Math.Sqrt(_sumSquared / _count - _sum * _sum / _count / _count);
                }
                else
                {
                    result.Mean = 0;
                    result.StandardDeviation = 0;
                }
                result.Min = _min;
                result.Max = _max;
                ResetStatistics();
                return result;
            }
        }

        private void ResetStatistics()
        {
            Debug.Assert(Monitor.IsEntered(MyLock));
            _count = 0;
            _sum = 0;
            _sumSquared = 0;
            _min = float.PositiveInfinity;
            _max = float.NegativeInfinity;
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

    internal class EventCounterGroup
    {
        private readonly EventSource _eventSource;
        private readonly List<EventCounter> _eventCounters;

        internal EventCounterGroup(EventSource eventSource)
        {
            _eventSource = eventSource;
            _eventCounters = new List<EventCounter>();
            RegisterCommandCallback();
        }

        internal void Add(EventCounter eventCounter)
        {
            lock (this) // Lock the EventCounterGroup
                _eventCounters.Add(eventCounter);
        }

        internal void Remove(EventCounter eventCounter)
        {
            lock (this) // Lock the EventCounterGroup
                _eventCounters.Remove(eventCounter);
        }

        #region EventSource Command Processing

        private void RegisterCommandCallback()
        {
            // Old destktop runtimes don't have this 
#if NO_EVENTCOMMANDEXECUTED_SUPPORT
            // We could not build against the API that had the EventCommandExecuted but maybe it is there are runtime.  
            // use reflection to try to get it.  
            System.Reflection.MethodInfo method = typeof(EventSource).GetMethod("add_EventCommandExecuted");
            if (method != null)
            {
                method.Invoke(_eventSource, new object[] { (EventHandler<EventCommandEventArgs>)OnEventSourceCommand });
            }
            else
            {
                string msg = "EventCounterError: Old Runtime that does not support EventSource.EventCommandExecuted.  EventCounters not Supported";
                _eventSource.Write(msg);
                Debug.WriteLine(msg);
            }
#else 
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
                    // Recursion through EventSource callbacks possible.  When we enable the timer
                    // we synchonously issue a EventSource.Write event, which in turn can call back
                    // to user code (in an EventListener) while holding this lock.   This is dangerous
                    // because it mean this code might inadvertantly participate in a lock loop. 
                    // The scenario seems very unlikely so we ignore that problem for now.  
                    lock (this)      // Lock the EventCounterGroup
                    {
                        EnableTimer(value);
                    }
                }
            }
        }

        #endregion // EventSource Command Processing

        #region Global EventCounterGroup Array management

        // We need eventCounters to 'attach' themselves to a particular EventSource.   
        // this table provides the mapping from EventSource -> EventCounterGroup 
        // which represents this 'attached' information.   
        private static WeakReference<EventCounterGroup>[] s_eventCounterGroups;
        private static readonly object s_eventCounterGroupsLock = new object();

        private static void EnsureEventSourceIndexAvailable(int eventSourceIndex)
        {
            Debug.Assert(Monitor.IsEntered(s_eventCounterGroupsLock));
            if (EventCounterGroup.s_eventCounterGroups == null)
            {
                EventCounterGroup.s_eventCounterGroups = new WeakReference<EventCounterGroup>[eventSourceIndex + 1];
            }
            else if (eventSourceIndex >= EventCounterGroup.s_eventCounterGroups.Length)
            {
                WeakReference<EventCounterGroup>[] newEventCounterGroups = new WeakReference<EventCounterGroup>[eventSourceIndex + 1];
                Array.Copy(EventCounterGroup.s_eventCounterGroups, 0, newEventCounterGroups, 0, EventCounterGroup.s_eventCounterGroups.Length);
                EventCounterGroup.s_eventCounterGroups = newEventCounterGroups;
            }
        }

        internal static EventCounterGroup GetEventCounterGroup(EventSource eventSource)
        {
            lock (s_eventCounterGroupsLock)
            {
                int eventSourceIndex = EventListener.EventSourceIndex(eventSource);
                EnsureEventSourceIndexAvailable(eventSourceIndex);
                WeakReference<EventCounterGroup> weakRef = EventCounterGroup.s_eventCounterGroups[eventSourceIndex];
                EventCounterGroup ret = null;
                if (weakRef == null || !weakRef.TryGetTarget(out ret))
                {
                    ret = new EventCounterGroup(eventSource);
                    EventCounterGroup.s_eventCounterGroups[eventSourceIndex] = new WeakReference<EventCounterGroup>(ret);
                }
                return ret;
            }
        }

        #endregion // Global EventCounterGroup Array management

        #region Timer Processing

        private DateTime _timeStampSinceCollectionStarted;
        private int _pollingIntervalInMilliseconds;
        private Timer _pollingTimer;

        private void DisposeTimer()
        {
            Debug.Assert(Monitor.IsEntered(this));
            if (_pollingTimer != null)
            {
                _pollingTimer.Dispose();
                _pollingTimer = null;
            }
        }

        private void EnableTimer(float pollingIntervalInSeconds)
        {
            Debug.Assert(Monitor.IsEntered(this));
            if (pollingIntervalInSeconds <= 0)
            {
                DisposeTimer();
                _pollingIntervalInMilliseconds = 0;
            }
            else if (_pollingIntervalInMilliseconds == 0 || pollingIntervalInSeconds * 1000 < _pollingIntervalInMilliseconds)
            {
                Debug.WriteLine("Polling interval changed at " + DateTime.UtcNow.ToString("mm.ss.ffffff"));
                _pollingIntervalInMilliseconds = (int)(pollingIntervalInSeconds * 1000);
                DisposeTimer();
                _timeStampSinceCollectionStarted = DateTime.UtcNow;
                _pollingTimer = new Timer(OnTimer, null, _pollingIntervalInMilliseconds, _pollingIntervalInMilliseconds);
            }
            // Always fire the timer event (so you see everything up to this time).  
            OnTimer(null);
        }

        private void OnTimer(object state)
        {
            Debug.WriteLine("Timer fired at " + DateTime.UtcNow.ToString("mm.ss.ffffff"));
            lock (this) // Lock the EventCounterGroup
            {
                if (_eventSource.IsEnabled())
                {
                    DateTime now = DateTime.UtcNow;
                    TimeSpan elapsed = now - _timeStampSinceCollectionStarted;

                    foreach (var eventCounter in _eventCounters)
                    {
                        EventCounterPayload payload = eventCounter.GetEventCounterPayload();
                        payload.IntervalSec = (float)elapsed.TotalSeconds;
                        _eventSource.Write("EventCounters", new EventSourceOptions() { Level = EventLevel.LogAlways }, new PayloadType(payload));
                    }
                    _timeStampSinceCollectionStarted = now;
                }
                else
                {
                    DisposeTimer();
                }
            }
        }

        /// <summary>
        /// This is the payload that is sent in the with EventSource.Write
        /// </summary>
        [EventData]
        class PayloadType
        {
            public PayloadType(EventCounterPayload payload) { Payload = payload; }
            public EventCounterPayload Payload { get; set; }
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

    }

    #endregion // internal supporting classes
}
