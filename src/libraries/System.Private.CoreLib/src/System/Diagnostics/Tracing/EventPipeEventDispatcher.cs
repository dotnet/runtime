// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace System.Diagnostics.Tracing
{
#if FEATURE_PERFTRACING
    internal sealed class EventPipeEventDispatcher
    {
        internal sealed class EventListenerSubscription
        {
            internal EventKeywords MatchAnyKeywords { get; private set; }
            internal EventLevel Level { get; private set; }

            internal EventListenerSubscription(EventKeywords matchAnyKeywords, EventLevel level)
            {
                MatchAnyKeywords = matchAnyKeywords;
                Level = level;
            }
        }

        internal static readonly EventPipeEventDispatcher Instance = new EventPipeEventDispatcher();

        private readonly IntPtr m_RuntimeProviderID;

        private ulong m_sessionID;

        private CancellationTokenSource? m_dispatchTaskCancellationSource;
        private Task? m_dispatchTask;

        // We take this lock to synchronize access to the shared session state. It is important to never take the EventSource.EventListenersLock while
        // holding this, or we can deadlock. Unfortunately calling in to EventSource at all can take the EventListenersLock in ways that are not obvious,
        // so don't call in to EventSource or other EventListeners while holding this unless you are certain it can't take the EventListenersLock.
        private readonly object m_dispatchControlLock = new object();
        private readonly Dictionary<EventListener, EventListenerSubscription> m_subscriptions = new Dictionary<EventListener, EventListenerSubscription>();

        private const uint DefaultEventListenerCircularMBSize = 10;

        private EventPipeEventDispatcher()
        {
            // Get the ID of the runtime provider so that it can be used as a filter when processing events.
            m_RuntimeProviderID = EventPipeInternal.GetProvider(NativeRuntimeEventSource.EventSourceName);
        }

        internal void SendCommand(EventListener eventListener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeywords)
        {
            lock (m_dispatchControlLock)
            {
                if (command == EventCommand.Update && enable)
                {
                    // Add the new subscription.  This will overwrite an existing subscription for the listener if one exists.
                    m_subscriptions[eventListener] = new EventListenerSubscription(matchAnyKeywords, level);
                }
                else if (command == EventCommand.Update && !enable)
                {
                    // Remove the event listener from the list of subscribers.
                    m_subscriptions.Remove(eventListener);
                }

                // Commit the configuration change.
                CommitDispatchConfiguration();
            }
        }

        private void CommitDispatchConfiguration()
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));

            // Signal that the thread should shut down
            SetStopDispatchTask();

            // Check to see if tracing should be enabled.
            if (m_subscriptions.Count <= 0)
            {
                return;
            }

            // Determine the keywords and level that should be used based on the set of enabled EventListeners.
            EventKeywords aggregatedKeywords = EventKeywords.None;
            EventLevel enableLevel = EventLevel.Critical;

            foreach (EventListenerSubscription subscription in m_subscriptions.Values)
            {
                aggregatedKeywords |= subscription.MatchAnyKeywords;

                if (enableLevel is EventLevel.LogAlways)
                {
                    continue;
                }
                if ((enableLevel < subscription.Level) ||
                    (subscription.Level is EventLevel.LogAlways))
                {
                    enableLevel = subscription.Level;
                }
            }

            // Enable the EventPipe session.
            EventPipeProviderConfiguration[] providerConfiguration = new EventPipeProviderConfiguration[]
            {
                new EventPipeProviderConfiguration(NativeRuntimeEventSource.EventSourceName, (ulong)aggregatedKeywords, (uint)enableLevel, null)
            };

            ulong sessionID = EventPipeInternal.Enable(null, EventPipeSerializationFormat.NetTrace, DefaultEventListenerCircularMBSize, providerConfiguration);
            if (sessionID == 0)
            {
                throw new EventSourceException(SR.EventSource_CouldNotEnableEventPipe);
            }

            // Get the session information that is required to properly dispatch events.
            EventPipeSessionInfo sessionInfo;
            unsafe
            {
                if (!EventPipeInternal.GetSessionInfo(sessionID, &sessionInfo))
                {
                    Debug.Fail("GetSessionInfo returned false.");
                }
            }


            DateTime syncTimeUtc = DateTime.FromFileTimeUtc(sessionInfo.StartTimeAsUTCFileTime);
            long syncTimeQPC = sessionInfo.StartTimeStamp;
            long timeQPCFrequency = sessionInfo.TimeStampFrequency;

            Debug.Assert(Volatile.Read(ref m_sessionID) == 0);
            Volatile.Write(ref m_sessionID, sessionID);

            // Start the dispatch task.
            StartDispatchTask(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency);
        }

        private void StartDispatchTask(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));
            Debug.Assert(sessionID != 0);

            m_dispatchTaskCancellationSource = new CancellationTokenSource();
            Task? previousDispatchTask = m_dispatchTask;
            m_dispatchTask = Task.Factory.StartNew(() => DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, previousDispatchTask, m_dispatchTaskCancellationSource.Token), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void SetStopDispatchTask()
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));

            if (m_dispatchTaskCancellationSource?.IsCancellationRequested ?? true)
            {
                return;
            }

            ulong sessionID = Volatile.Read(ref m_sessionID);
            Debug.Assert(sessionID != 0);
            m_dispatchTaskCancellationSource.Cancel();
            EventPipeInternal.SignalSession(sessionID);
            Volatile.Write(ref m_sessionID, 0);
        }

        private unsafe void DispatchEventsToEventListeners(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency, Task? previousDispatchTask, CancellationToken token)
        {
            Debug.Assert(sessionID != 0);
            previousDispatchTask?.Wait(CancellationToken.None);

            // Struct to fill with the call to GetNextEvent.
            EventPipeEventInstanceData instanceData;
            while (!token.IsCancellationRequested)
            {
                bool eventsReceived = false;
                // Get the next event.
                while (!token.IsCancellationRequested && EventPipeInternal.GetNextEvent(sessionID, &instanceData))
                {
                    eventsReceived = true;

                    // Filter based on provider.
                    if (instanceData.ProviderID == m_RuntimeProviderID)
                    {
                        // Dispatch the event.
                        ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>((void*)instanceData.Payload, (int)instanceData.PayloadLength);
                        DateTime dateTimeStamp = TimeStampToDateTime(instanceData.TimeStamp, syncTimeUtc, syncTimeQPC, timeQPCFrequency);
                        NativeRuntimeEventSource.Log.ProcessEvent(instanceData.EventID, instanceData.ThreadID, dateTimeStamp, instanceData.ActivityId, instanceData.ChildActivityId, payload);
                    }
                }

                // Wait for more events.
                if (!token.IsCancellationRequested)
                {
                    if (!eventsReceived)
                    {
                        EventPipeInternal.WaitForSessionSignal(sessionID, Timeout.Infinite);
                    }

                    Thread.Sleep(10);
                }
            }

            // Wait for SignalSession() to be called before we call disable, otherwise
            // the SignalSession() call could be on a disabled session.
            SpinWait sw = default;
            while (Volatile.Read(ref m_sessionID) == sessionID)
            {
                sw.SpinOnce();
            }

            // Disable the old session. This can happen asynchronously since we aren't using the old session
            // anymore.
            EventPipeInternal.Disable(sessionID);
        }

        /// <summary>
        /// Converts a QueryPerformanceCounter (QPC) timestamp to a UTC DateTime.
        /// </summary>
        private static DateTime TimeStampToDateTime(long timeStamp, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
        {
            if (timeStamp == long.MaxValue)
            {
                return DateTime.MaxValue;
            }

            Debug.Assert((syncTimeUtc.Ticks != 0) && (syncTimeQPC != 0) && (timeQPCFrequency != 0));
            long inTicks = (long)((timeStamp - syncTimeQPC) * 10000000.0 / timeQPCFrequency) + syncTimeUtc.Ticks;
            if ((inTicks < 0) || (DateTime.MaxTicks < inTicks))
            {
                inTicks = DateTime.MaxTicks;
            }

            return new DateTime(inTicks, DateTimeKind.Utc);
        }
    }
#endif // FEATURE_PERFTRACING
}
