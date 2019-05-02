// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
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

        internal CounterGroup(EventSource eventSource)
        {
            _eventSource = eventSource;
            _counters = new List<DiagnosticCounter>();
            RegisterCommandCallback();
        }

        internal void Add(DiagnosticCounter eventCounter)
        {
            lock (this) // Lock the CounterGroup
                _counters.Add(eventCounter);
        }

        internal void Remove(DiagnosticCounter eventCounter)
        {
            lock (this) // Lock the CounterGroup
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
                string valueStr;
                float value;
                Debug.Assert(e.Arguments != null);

                if (e.Arguments.TryGetValue("EventCounterIntervalSec", out valueStr) && float.TryParse(valueStr, out value))
                {
                    // Recursion through EventSource callbacks possible.  When we enable the timer
                    // we synchonously issue a EventSource.Write event, which in turn can call back
                    // to user code (in an EventListener) while holding this lock.   This is dangerous
                    // because it means this code might inadvertantly participate in a lock loop. 
                    // The scenario seems very unlikely so we ignore that problem for now.  
                    lock (this)      // Lock the CounterGroup
                    {
                        EnableTimer(value);
                    }
                }
            }
        }

        #endregion // EventSource Command Processing

        #region Global CounterGroup Array management

        // We need eventCounters to 'attach' themselves to a particular EventSource.   
        // this table provides the mapping from EventSource -> CounterGroup 
        // which represents this 'attached' information.   
        private static WeakReference<CounterGroup>[]? s_counterGroups;
        private static readonly object s_counterGroupsLock = new object();

        private static void EnsureEventSourceIndexAvailable(int eventSourceIndex)
        {
            Debug.Assert(Monitor.IsEntered(s_counterGroupsLock));
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
            lock (s_counterGroupsLock)
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
        private Timer? _pollingTimer;

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
                // Don't capture the current ExecutionContext and its AsyncLocals onto the timer causing them to live forever
                bool restoreFlow = false;
                try
                {
                    if (!ExecutionContext.IsFlowSuppressed())
                    {
                        ExecutionContext.SuppressFlow();
                        restoreFlow = true;
                    }

                    _pollingTimer = new Timer(s => ((CounterGroup)s!).OnTimer(null), this, _pollingIntervalInMilliseconds, _pollingIntervalInMilliseconds);
                }
                finally
                {
                    // Restore the current ExecutionContext
                    if (restoreFlow)
                        ExecutionContext.RestoreFlow();
                }
            }
            // Always fire the timer event (so you see everything up to this time).  
            OnTimer(null);
        }

        private void OnTimer(object? state)
        {
            Debug.WriteLine("Timer fired at " + DateTime.UtcNow.ToString("mm.ss.ffffff"));
            lock (this) // Lock the CounterGroup
            {
                if (_eventSource.IsEnabled())
                {
                    DateTime now = DateTime.UtcNow;
                    TimeSpan elapsed = now - _timeStampSinceCollectionStarted;

                    foreach (var counter in _counters)
                    {
                        counter.WritePayload((float)elapsed.TotalSeconds, _pollingIntervalInMilliseconds);
                    }
                    _timeStampSinceCollectionStarted = now;
                }
                else
                {
                    DisposeTimer();
                }
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

    }
}
