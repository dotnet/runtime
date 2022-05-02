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
        private DateTime m_syncTimeUtc;
        private long m_syncTimeQPC;
        private long m_timeQPCFrequency;

        private bool m_stopDispatchTask;
        private readonly EventPipeWaitHandle m_dispatchTaskWaitHandle = new EventPipeWaitHandle();
        private Task? m_dispatchTask;
        private readonly object m_dispatchControlLock = new object();
        private readonly Dictionary<EventListener, EventListenerSubscription> m_subscriptions = new Dictionary<EventListener, EventListenerSubscription>();

        private const uint DefaultEventListenerCircularMBSize = 10;

        private EventPipeEventDispatcher()
        {
            // Get the ID of the runtime provider so that it can be used as a filter when processing events.
            m_RuntimeProviderID = EventPipeInternal.GetProvider(NativeRuntimeEventSource.EventSourceName);
            m_dispatchTaskWaitHandle.SafeWaitHandle = new SafeWaitHandle(IntPtr.Zero, false);
        }

        internal void SendCommand(EventListener eventListener, EventCommand command, bool enable, EventLevel level, EventKeywords matchAnyKeywords)
        {
            if (command == EventCommand.Update && enable)
            {
                lock (m_dispatchControlLock)
                {
                    // Add the new subscription.  This will overwrite an existing subscription for the listener if one exists.
                    m_subscriptions[eventListener] = new EventListenerSubscription(matchAnyKeywords, level);

                    // Commit the configuration change.
                    CommitDispatchConfiguration();
                }
            }
            else if (command == EventCommand.Update && !enable)
            {
                RemoveEventListener(eventListener);
            }
        }

        internal void RemoveEventListener(EventListener listener)
        {
            lock (m_dispatchControlLock)
            {
                // Remove the event listener from the list of subscribers.
                m_subscriptions.Remove(listener);

                // Commit the configuration change.
                CommitDispatchConfiguration();
            }
        }

        private void CommitDispatchConfiguration()
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));

            // Ensure that the dispatch task is stopped.
            // This is a no-op if the task is already stopped.
            StopDispatchTask();

            // Stop tracing.
            // This is a no-op if it's already disabled.
            EventPipeInternal.Disable(m_sessionID);

            // Check to see if tracing should be enabled.
            if (m_subscriptions.Count <= 0)
            {
                return;
            }

            // Determine the keywords and level that should be used based on the set of enabled EventListeners.
            EventKeywords aggregatedKeywords = EventKeywords.None;
            EventLevel highestLevel = EventLevel.LogAlways;

            foreach (EventListenerSubscription subscription in m_subscriptions.Values)
            {
                aggregatedKeywords |= subscription.MatchAnyKeywords;
                highestLevel = (subscription.Level > highestLevel) ? subscription.Level : highestLevel;
            }

            // Enable the EventPipe session.
            EventPipeProviderConfiguration[] providerConfiguration = new EventPipeProviderConfiguration[]
            {
                new EventPipeProviderConfiguration(NativeRuntimeEventSource.EventSourceName, (ulong)aggregatedKeywords, (uint)highestLevel, null)
            };

            m_sessionID = EventPipeInternal.Enable(null, EventPipeSerializationFormat.NetTrace, DefaultEventListenerCircularMBSize, providerConfiguration);
            Debug.Assert(m_sessionID != 0);

            // Get the session information that is required to properly dispatch events.
            EventPipeSessionInfo sessionInfo;
            unsafe
            {
                if (!EventPipeInternal.GetSessionInfo(m_sessionID, &sessionInfo))
                {
                    Debug.Fail("GetSessionInfo returned false.");
                }
            }

            m_syncTimeUtc = DateTime.FromFileTimeUtc(sessionInfo.StartTimeAsUTCFileTime);
            m_syncTimeQPC = sessionInfo.StartTimeStamp;
            m_timeQPCFrequency = sessionInfo.TimeStampFrequency;

            // Start the dispatch task.
            StartDispatchTask();
        }

        private void StartDispatchTask()
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));

            if (m_dispatchTask == null)
            {
                m_stopDispatchTask = false;
                // Create a SafeWaitHandle that won't release the handle when done
                m_dispatchTaskWaitHandle.SafeWaitHandle = new SafeWaitHandle(EventPipeInternal.GetWaitHandle(m_sessionID), false);

                m_dispatchTask = Task.Factory.StartNew(DispatchEventsToEventListeners, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
            }
        }

        private void StopDispatchTask()
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));

            if (m_dispatchTask != null)
            {
                m_stopDispatchTask = true;
                Debug.Assert(!m_dispatchTaskWaitHandle.SafeWaitHandle.IsInvalid);
                EventWaitHandle.Set(m_dispatchTaskWaitHandle.SafeWaitHandle);
                m_dispatchTask.Wait();
                m_dispatchTask = null;
            }
        }

        private unsafe void DispatchEventsToEventListeners()
        {
            // Struct to fill with the call to GetNextEvent.
            EventPipeEventInstanceData instanceData;

            while (!m_stopDispatchTask)
            {
                bool eventsReceived = false;
                // Get the next event.
                while (!m_stopDispatchTask && EventPipeInternal.GetNextEvent(m_sessionID, &instanceData))
                {
                    eventsReceived = true;

                    // Filter based on provider.
                    if (instanceData.ProviderID == m_RuntimeProviderID)
                    {
                        // Dispatch the event.
                        ReadOnlySpan<byte> payload = new ReadOnlySpan<byte>((void*)instanceData.Payload, (int)instanceData.PayloadLength);
                        DateTime dateTimeStamp = TimeStampToDateTime(instanceData.TimeStamp);
                        NativeRuntimeEventSource.Log.ProcessEvent(instanceData.EventID, instanceData.ThreadID, dateTimeStamp, instanceData.ActivityId, instanceData.ChildActivityId, payload);
                    }
                }

                // Wait for more events.
                if (!m_stopDispatchTask)
                {
                    if (!eventsReceived)
                    {
                        // Future TODO: this would make more sense to handle in EventPipeSession/EventPipe native code.
                        Debug.Assert(!m_dispatchTaskWaitHandle.SafeWaitHandle.IsInvalid);
                        m_dispatchTaskWaitHandle.WaitOne();
                    }

                    Thread.Sleep(10);
                }
            }
        }

        /// <summary>
        /// Converts a QueryPerformanceCounter (QPC) timestamp to a UTC DateTime.
        /// </summary>
        private DateTime TimeStampToDateTime(long timeStamp)
        {
            if (timeStamp == long.MaxValue)
            {
                return DateTime.MaxValue;
            }

            Debug.Assert((m_syncTimeUtc.Ticks != 0) && (m_syncTimeQPC != 0) && (m_timeQPCFrequency != 0));
            long inTicks = (long)((timeStamp - m_syncTimeQPC) * 10000000.0 / m_timeQPCFrequency) + m_syncTimeUtc.Ticks;
            if ((inTicks < 0) || (DateTime.MaxTicks < inTicks))
            {
                inTicks = DateTime.MaxTicks;
            }

            return new DateTime(inTicks, DateTimeKind.Utc);
        }
    }
#endif // FEATURE_PERFTRACING
}
