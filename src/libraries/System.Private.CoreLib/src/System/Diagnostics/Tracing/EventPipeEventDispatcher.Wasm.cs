// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace System.Diagnostics.Tracing
{
    // this is single-threaded version of EventPipeEventDispatcher
    internal sealed partial class EventPipeEventDispatcher
    {
        private void StartDispatchTask(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));
            Debug.Assert(sessionID != 0);

            m_dispatchTaskCancellationSource = new CancellationTokenSource();
            Task? previousDispatchTask = m_dispatchTask;
            if (previousDispatchTask != null)
            {
                m_dispatchTask = previousDispatchTask.ContinueWith(_ => DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, m_dispatchTaskCancellationSource.Token),
                    m_dispatchTaskCancellationSource.Token, TaskContinuationOptions.None, TaskScheduler.Default);
            }
            else
            {
                m_dispatchTask = DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, m_dispatchTaskCancellationSource.Token);
            }
        }

        private async Task DispatchEventsToEventListeners(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency, CancellationToken token)
        {
            Debug.Assert(sessionID != 0);

            while (!token.IsCancellationRequested)
            {
                DispatchEventsToEventListenersOnce(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, token);
                await Task.Delay(100, token).ConfigureAwait(false);
            }

            EventPipeInternal.Disable(sessionID);
        }
    }
}
