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
        // this is trade-off between speed of dispatching events and CPU usage of the same thread which is running actual business logic
        private const int DispatcherDelayMs = 100;

        private void StartDispatchTask(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency)
        {
            Debug.Assert(Monitor.IsEntered(m_dispatchControlLock));
            Debug.Assert(sessionID != 0);

            CancellationTokenSource cancellationSource = new CancellationTokenSource();
            m_dispatchTaskCancellationSource = cancellationSource;
            CancellationToken token = cancellationSource.Token;
            Task? previousDispatchTask = m_dispatchTask;
            if (previousDispatchTask != null)
            {
                // The continuation must always run, even if already cancelled, because it owns disabling the session.
                m_dispatchTask = previousDispatchTask.ContinueWith(_ => DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, token),
                    CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.Default).Unwrap();
            }
            else
            {
                m_dispatchTask = DispatchEventsToEventListeners(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, token);
            }
        }

        private async Task DispatchEventsToEventListeners(ulong sessionID, DateTime syncTimeUtc, long syncTimeQPC, long timeQPCFrequency, CancellationToken token)
        {
            Debug.Assert(sessionID != 0);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    DispatchEventsToEventListenersOnce(sessionID, syncTimeUtc, syncTimeQPC, timeQPCFrequency, token);
                    await Task.Delay(DispatcherDelayMs, token).ConfigureAwait(false);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
            }
            finally
            {
                // Wait for SignalSession() to be called before we call disable, otherwise
                // the SignalSession() call could be on a disabled session.
                await Task.Yield();

                EventPipeInternal.Disable(sessionID);
            }
        }
    }
}
