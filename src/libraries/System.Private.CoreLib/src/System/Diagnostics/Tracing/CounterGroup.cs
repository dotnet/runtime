// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Runtime.Versioning;
using System.Threading;

namespace System.Diagnostics.Tracing
{
#if !ES_BUILD_STANDALONE
#if !FEATURE_WASM_PERFTRACING
    [System.Runtime.Versioning.UnsupportedOSPlatform("browser")]
#endif
#endif
    internal sealed class CounterGroup
    {
        private readonly EventSource _eventSource;
        private readonly List<DiagnosticCounter> _counters;
        private static readonly object s_counterGroupLock = new object();

        internal CounterGroup(EventSource eventSource)
        {
            _eventSource = eventSource;
            _counters = new List<DiagnosticCounter>();
            RegisterCommandCallback();
        }

        internal void Add(DiagnosticCounter eventCounter)
        {
            lock (s_counterGroupLock) // Lock the CounterGroup
                _counters.Add(eventCounter);
        }

        internal void Remove(DiagnosticCounter eventCounter)
        {
            lock (s_counterGroupLock) // Lock the CounterGroup
                _counters.Remove(eventCounter);
        }

#region EventSource Command Processing

        private void RegisterCommandCallback()
        {
            _eventSource.EventCommandExecuted += OnEventSourceCommand;
        }

        private void OnEventSourceCommand(object? sender, EventCommandEventArgs e)
        {
            if (e.Command == EventCommand.Enable || e.Command == EventCommand.Update)
            {
                Debug.Assert(e.Arguments != null);

                if (e.Arguments.TryGetValue("EventCounterIntervalSec", out string? valueStr) && float.TryParse(valueStr, out float value))
                {
                    lock (s_counterGroupLock)      // Lock the CounterGroup
                    {
                        EnableTimer(value);
                    }
                }
            }
            else if (e.Command == EventCommand.Disable)
            {
                lock (s_counterGroupLock)
                {
                    DisableTimer();
                }
            }
        }

#endregion // EventSource Command Processing

#region Global CounterGroup Array management

        // We need eventCounters to 'attach' themselves to a particular EventSource.
        // this table provides the mapping from EventSource -> CounterGroup
        // which represents this 'attached' information.
        private static WeakReference<CounterGroup>[]? s_counterGroups;

        private static void EnsureEventSourceIndexAvailable(int eventSourceIndex)
        {
            Debug.Assert(Monitor.IsEntered(s_counterGroupLock));
            if (CounterGroup.s_counterGroups == null)
            {
                CounterGroup.s_counterGroups = new WeakReference<CounterGroup>[eventSourceIndex + 1];
            }
            else if (eventSourceIndex >= CounterGroup.s_counterGroups.Length)
            {
                WeakReference<CounterGroup>[] newCounterGroups = new WeakReference<CounterGroup>[eventSourceIndex + 1];
                Array.Copy(CounterGroup.s_counterGroups, newCounterGroups, CounterGroup.s_counterGroups.Length);
                CounterGroup.s_counterGroups = newCounterGroups;
            }
        }

        internal static CounterGroup GetCounterGroup(EventSource eventSource)
        {
            lock (s_counterGroupLock)
            {
                int eventSourceIndex = EventListener.EventSourceIndex(eventSource);
                EnsureEventSourceIndexAvailable(eventSourceIndex);
                Debug.Assert(s_counterGroups != null);
                WeakReference<CounterGroup> weakRef = CounterGroup.s_counterGroups[eventSourceIndex];
                if (weakRef == null || !weakRef.TryGetTarget(out CounterGroup? ret))
                {
                    ret = new CounterGroup(eventSource);
                    CounterGroup.s_counterGroups[eventSourceIndex] = new WeakReference<CounterGroup>(ret);
                }
                return ret;
            }
        }

#endregion // Global CounterGroup Array management

#region Timer Processing

        private DateTime _timeStampSinceCollectionStarted;
        private int _pollingIntervalInMilliseconds;
        private DateTime _nextPollingTimeStamp;

        private void EnableTimer(float pollingIntervalInSeconds)
        {
            Debug.Assert(Monitor.IsEntered(s_counterGroupLock));
            if (pollingIntervalInSeconds <= 0)
            {
                DisableTimer();
            }
            else if (_pollingIntervalInMilliseconds == 0 || pollingIntervalInSeconds * 1000 < _pollingIntervalInMilliseconds)
            {
                _pollingIntervalInMilliseconds = (int)(pollingIntervalInSeconds * 1000);
                ResetCounters(); // Reset statistics for counters before we start the thread.

                _timeStampSinceCollectionStarted = DateTime.UtcNow;
                _nextPollingTimeStamp = DateTime.UtcNow + new TimeSpan(0, 0, (int)pollingIntervalInSeconds);

                // Create the polling thread and init all the shared state if needed
                if (s_pollingThread == null)
                {
                    s_pollingThreadSleepEvent = new AutoResetEvent(false);
                    s_counterGroupEnabledList = new List<CounterGroup>();
                    s_pollingThread = new Thread(PollForValues)
                    {
                        IsBackground = true,
                        Name = ".NET Counter Poller"
                    };
                    s_pollingThread.InternalUnsafeStart();
                }

                if (!s_counterGroupEnabledList!.Contains(this))
                {
                    s_counterGroupEnabledList.Add(this);
                }

                // notify the polling thread that the polling interval may have changed and the sleep should
                // be recomputed
                s_pollingThreadSleepEvent!.Set();
            }
        }

        private void DisableTimer()
        {
            Debug.Assert(Monitor.IsEntered(s_counterGroupLock));
            _pollingIntervalInMilliseconds = 0;
            s_counterGroupEnabledList?.Remove(this);
        }

        private void ResetCounters()
        {
            lock (s_counterGroupLock) // Lock the CounterGroup
            {
                foreach (DiagnosticCounter counter in _counters)
                {
                    if (counter is IncrementingEventCounter ieCounter)
                    {
                        ieCounter.UpdateMetric();
                    }
                    else if (counter is IncrementingPollingCounter ipCounter)
                    {
                        ipCounter.UpdateMetric();
                    }
                    else if (counter is EventCounter eCounter)
                    {
                        eCounter.ResetStatistics();
                    }
                }
            }
        }

        private void OnTimer()
        {
            if (_eventSource.IsEnabled())
            {
                DateTime now;
                TimeSpan elapsed;
                int pollingIntervalInMilliseconds;
                DiagnosticCounter[] counters;
                lock (s_counterGroupLock)
                {
                    now = DateTime.UtcNow;
                    elapsed = now - _timeStampSinceCollectionStarted;
                    pollingIntervalInMilliseconds = _pollingIntervalInMilliseconds;
                    counters = new DiagnosticCounter[_counters.Count];
                    _counters.CopyTo(counters);
                }

                // MUST keep out of the scope of s_counterGroupLock because this will cause WritePayload
                // callback can be re-entrant to CounterGroup (i.e. it's possible it calls back into EnableTimer()
                // above, since WritePayload callback can contain user code that can invoke EventSource constructor
                // and lead to a deadlock. (See https://github.com/dotnet/runtime/issues/40190 for details)
                foreach (DiagnosticCounter counter in counters)
                {
                    // NOTE: It is still possible for a race condition to occur here. An example is if the session
                    // that subscribed to these batch of counters was disabled and it was immediately enabled in
                    // a different session, some of the counter data that was supposed to be written to the old
                    // session can now "overflow" into the new session.
                    // This problem pre-existed to this change (when we used to hold lock in the call to WritePayload):
                    // the only difference being the old behavior caused the entire batch of counters to be either
                    // written to the old session or the new session. The behavior change is not being treated as a
                    // significant problem to address for now, but we can come back and address it if it turns out to
                    // be an actual issue.
                    counter.WritePayload((float)elapsed.TotalSeconds, pollingIntervalInMilliseconds);
                }

                lock (s_counterGroupLock)
                {
                    _timeStampSinceCollectionStarted = now;
                    TimeSpan delta = now - _nextPollingTimeStamp;
                    delta = _pollingIntervalInMilliseconds > delta.TotalMilliseconds ? TimeSpan.FromMilliseconds(_pollingIntervalInMilliseconds) : delta;
                    if (_pollingIntervalInMilliseconds > 0)
                        _nextPollingTimeStamp += TimeSpan.FromMilliseconds(_pollingIntervalInMilliseconds * Math.Ceiling(delta.TotalMilliseconds / _pollingIntervalInMilliseconds));
                }
            }
        }

        private static Thread? s_pollingThread;
        // Used for sleeping for a certain amount of time while allowing the thread to be woken up
        private static AutoResetEvent? s_pollingThreadSleepEvent;

        private static List<CounterGroup>? s_counterGroupEnabledList;

        private static void PollForValues()
        {
            AutoResetEvent? sleepEvent = null;

            // Cache of onTimer callbacks for each CounterGroup.
            // We cache these outside of the scope of s_counterGroupLock because
            // calling into the callbacks can cause a re-entrancy into CounterGroup.Enable()
            // and result in a deadlock. (See https://github.com/dotnet/runtime/issues/40190 for details)
            var onTimers = new List<CounterGroup>();
            while (true)
            {
                int sleepDurationInMilliseconds = int.MaxValue;
                lock (s_counterGroupLock)
                {
                    sleepEvent = s_pollingThreadSleepEvent;
                    foreach (CounterGroup counterGroup in s_counterGroupEnabledList!)
                    {
                        DateTime now = DateTime.UtcNow;
                        if (counterGroup._nextPollingTimeStamp < now + new TimeSpan(0, 0, 0, 0, 1))
                        {
                            onTimers.Add(counterGroup);
                        }

                        int millisecondsTillNextPoll = (int)((counterGroup._nextPollingTimeStamp - now).TotalMilliseconds);
                        millisecondsTillNextPoll = Math.Max(1, millisecondsTillNextPoll);
                        sleepDurationInMilliseconds = Math.Min(sleepDurationInMilliseconds, millisecondsTillNextPoll);
                    }
                }
                foreach (CounterGroup onTimer in onTimers)
                {
                    onTimer.OnTimer();
                }
                onTimers.Clear();
                if (sleepDurationInMilliseconds == int.MaxValue)
                {
                    sleepDurationInMilliseconds = -1; // WaitOne uses -1 to mean infinite
                }
                sleepEvent?.WaitOne(sleepDurationInMilliseconds);
            }
        }

#endregion // Timer Processing

    }
}
