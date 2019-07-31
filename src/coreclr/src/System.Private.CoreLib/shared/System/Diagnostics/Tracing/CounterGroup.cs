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
    internal class CounterGroup
    {
        private readonly EventSource _eventSource;
        private readonly List<DiagnosticCounter> _counters;
        private static readonly object s_counterGroupLock  = new object();

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
                lock(s_counterGroupLock)
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
                Array.Copy(CounterGroup.s_counterGroups, 0, newCounterGroups, 0, CounterGroup.s_counterGroups.Length);
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
                CounterGroup? ret = null;
                if (weakRef == null || !weakRef.TryGetTarget(out ret))
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
                _pollingIntervalInMilliseconds = 0;
            }
            else if (_pollingIntervalInMilliseconds == 0 || pollingIntervalInSeconds * 1000 < _pollingIntervalInMilliseconds)
            {
                _pollingIntervalInMilliseconds = (int)(pollingIntervalInSeconds * 1000);
                ResetCounters(); // Reset statistics for counters before we start the thread.

                _timeStampSinceCollectionStarted = DateTime.UtcNow;
                // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                bool restoreFlow = false;
                try
                {
                    if (!ExecutionContext.IsFlowSuppressed())
                    {
                        ExecutionContext.SuppressFlow();
                        restoreFlow = true;
                    }

                    _nextPollingTimeStamp = DateTime.UtcNow + new TimeSpan(0, 0, (int)pollingIntervalInSeconds);
                    
                    // Create the polling thread and init all the shared state if needed
                    if (s_pollingThread == null)
                    {
                        s_pollingThreadSleepEvent = new AutoResetEvent(false);
                        s_counterGroupEnabledList = new List<CounterGroup>();
                        s_pollingThread = new Thread(PollForValues) { IsBackground = true };
                        s_pollingThread.Start();
                    }

                    if (!s_counterGroupEnabledList!.Contains(this))
                    {
                        s_counterGroupEnabledList.Add(this);
                    }

                    // notify the polling thread that the polling interval may have changed and the sleep should
                    // be recomputed
                    s_pollingThreadSleepEvent!.Set();
                }
                finally
                {
                    // Restore the current ExecutionContext
                    if (restoreFlow)
                        ExecutionContext.RestoreFlow();
                }
            }
        }

        private void DisableTimer()
        {
            _pollingIntervalInMilliseconds = 0;
            s_counterGroupEnabledList?.Remove(this);
        }

        private void ResetCounters()
        {
            lock (s_counterGroupLock) // Lock the CounterGroup
            {
                foreach (var counter in _counters)
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
            Debug.Assert(Monitor.IsEntered(s_counterGroupLock));
            if (_eventSource.IsEnabled())
            {
                DateTime now = DateTime.UtcNow;
                TimeSpan elapsed = now - _timeStampSinceCollectionStarted;

                foreach (var counter in _counters)
                {
                    counter.WritePayload((float)elapsed.TotalSeconds, _pollingIntervalInMilliseconds);
                }
                _timeStampSinceCollectionStarted = now;

                do
                {
                    _nextPollingTimeStamp += new TimeSpan(0, 0, 0, 0, _pollingIntervalInMilliseconds);
                } while (_nextPollingTimeStamp <= now);
            }
        }



        private static Thread? s_pollingThread;
        // Used for sleeping for a certain amount of time while allowing the thread to be woken up
        private static AutoResetEvent? s_pollingThreadSleepEvent;

        private static List<CounterGroup>? s_counterGroupEnabledList;

        private static void PollForValues()
        {
            AutoResetEvent? sleepEvent = null;
            while (true)
            {
                int sleepDurationInMilliseconds = Int32.MaxValue;
                lock (s_counterGroupLock)
                {
                    sleepEvent = s_pollingThreadSleepEvent;
                    foreach (CounterGroup counterGroup in s_counterGroupEnabledList!)
                    {
                        DateTime now = DateTime.UtcNow;
                        if (counterGroup._nextPollingTimeStamp < now + new TimeSpan(0, 0, 0, 0, 1))
                        {
                            counterGroup.OnTimer();
                        }

                        int millisecondsTillNextPoll = (int)((counterGroup._nextPollingTimeStamp - now).TotalMilliseconds);
                        millisecondsTillNextPoll = Math.Max(1, millisecondsTillNextPoll);
                        sleepDurationInMilliseconds = Math.Min(sleepDurationInMilliseconds, millisecondsTillNextPoll);
                    }
                }
                if (sleepDurationInMilliseconds == Int32.MaxValue)
                {
                    sleepDurationInMilliseconds = -1; // WaitOne uses -1 to mean infinite
                }
                sleepEvent?.WaitOne(sleepDurationInMilliseconds);
            }
        }


        #endregion // Timer Processing

    }
}
