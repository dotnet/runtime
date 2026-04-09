// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
    internal sealed partial class EventPipeEventDispatcher
    {
        private void StartDispatchTask(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));
            Debug.Assert(sessionID != 0);

            m_dispatchTaskCancellationSource = new CancellationTokenSource();
            Task? previousDispatchTask = m_dispatchTask;
            m_dispatchTask = Task.Factory.StartNew(() => DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, previousDispatchTask, m_dispatchTaskCancellationSource.Token), CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private void DispatchEventsToEventListeners(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency, Task? previousDispatchTask, CancellationToken token)
        {
            Debug.Assert(sessionID != 0);
            previousDispatchTask?.Wait(CancellationToken.None);

            // Struct to fill with the call to GetNextEvent.
            while (!token.IsCancellationRequested)
            {
                bool eventsReceived = DispatchEventsToEventListenersOnce(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, token);

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
    }
}
