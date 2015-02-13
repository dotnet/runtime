// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace System.Threading.Tasks
{
    //
    // TaskReplicator runs a delegate inside of one or more Tasks, concurrently.  The idea is to exploit "available"
    // parallelism, where "available" is determined by the TaskScheduler.  We always keep one Task queued to
    // the scheduler, and if it starts running we queue another one, etc., up to some (potentially) user-defined
    // limit.
    //
    internal class TaskReplicator
    {
        public delegate void ReplicatableUserAction<TState>(ref TState replicaState, int timeout, out bool yieldedBeforeCompletion);

        private readonly TaskScheduler _scheduler;
        private readonly bool _stopOnFirstFailure;

        private readonly ConcurrentQueue<Replica> _pendingReplicas = new ConcurrentQueue<Replica>();
        private ConcurrentQueue<Exception> _exceptions;
        private bool _stopReplicating;

        private abstract class Replica
        {
            protected readonly TaskReplicator m_replicator;
            protected readonly int m_timeout;
            protected int m_remainingConcurrency;
            protected volatile Task m_pendingTask; // the most recently queued Task for this replica, or null if we're done.

            protected Replica(TaskReplicator replicator, int maxConcurrency, int timeout)
            {
                m_replicator = replicator;
                m_timeout = timeout;
                m_remainingConcurrency = maxConcurrency - 1;
                m_pendingTask = new Task(s => ((Replica)s).Execute(), this);
                m_replicator._pendingReplicas.Enqueue(this);
            }

            public void Start()
            {
                m_pendingTask.RunSynchronously(m_replicator._scheduler);
            }

            public void Wait()
            {
                //
                // We wait in a loop because each Task might queue another Task, and so on.
                // It's entirely possible for multiple Tasks to be queued without this loop seeing them,
                // but that's fine, since we really only need to know when all of them have finished.
                //
                // Note that it's *very* important that we use Task.Wait here, rather than waiting on some
                // other synchronization primitive.  Task.Wait can "inline" the Task's execution, on this thread,
                // if it hasn't started running on another thread.  That's essential for preventing deadlocks,
                // in the case where all other threads are blocked for other reasons.
                //
                Task pendingTask;
                while ((pendingTask = m_pendingTask) != null)
                    pendingTask.Wait();
            }

            public void Execute()
            {
                try
                {
                    if (!m_replicator._stopReplicating && m_remainingConcurrency > 0)
                    {
                        CreateNewReplica();
                        m_remainingConcurrency = 0; // new replica is responsible for adding concurrency from now on.
                    }

                    bool userActionYieldedBeforeCompletion;

                    ExecuteAction(out userActionYieldedBeforeCompletion);

                    if (userActionYieldedBeforeCompletion)
                    {
                        m_pendingTask = new Task(s => ((Replica)s).Execute(), this, CancellationToken.None, TaskCreationOptions.PreferFairness);
                        m_pendingTask.Start(m_replicator._scheduler);
                    }
                    else
                    {
                        m_replicator._stopReplicating = true;
                        m_pendingTask = null;
                    }
                }
                catch (Exception ex)
                {
                    LazyInitializer.EnsureInitialized(ref m_replicator._exceptions).Enqueue(ex);
                    if (m_replicator._stopOnFirstFailure)
                        m_replicator._stopReplicating = true;
                    m_pendingTask = null;
                }
            }

            protected abstract void CreateNewReplica();
            protected abstract void ExecuteAction(out bool yieldedBeforeCompletion);
        }

        private sealed class Replica<TState> : Replica
        {
            private readonly ReplicatableUserAction<TState> _action;
            private TState _state;

            public Replica(TaskReplicator replicator, int maxConcurrency, int timeout, ReplicatableUserAction<TState> action)
                : base(replicator, maxConcurrency, timeout)
            {
                _action = action;
            }

            protected override void CreateNewReplica()
            {
                Replica<TState> newReplica = new Replica<TState>(m_replicator, m_remainingConcurrency, GenerateCooperativeMultitaskingTaskTimeout(), _action);
                newReplica.m_pendingTask.Start(m_replicator._scheduler);
            }

            protected override void ExecuteAction(out bool yieldedBeforeCompletion)
            {
                _action(ref _state, m_timeout, out yieldedBeforeCompletion);
            }
        }

        private TaskReplicator(ParallelOptions options, bool stopOnFirstFailure)
        {
            _scheduler = options.TaskScheduler ?? TaskScheduler.Current;
            _stopOnFirstFailure = stopOnFirstFailure;
        }

        public static void Run<TState>(ReplicatableUserAction<TState> action, ParallelOptions options, bool stopOnFirstFailure)
        {
            int maxConcurrencyLevel = (options.EffectiveMaxConcurrencyLevel > 0) ? options.EffectiveMaxConcurrencyLevel : int.MaxValue;

            TaskReplicator replicator = new TaskReplicator(options, stopOnFirstFailure);
            new Replica<TState>(replicator, maxConcurrencyLevel, CooperativeMultitaskingTaskTimeout_RootTask, action).Start();

            Replica nextReplica;
            while (replicator._pendingReplicas.TryDequeue(out nextReplica))
                nextReplica.Wait();

            if (replicator._exceptions != null)
                throw new AggregateException(replicator._exceptions);
        }


        private const Int32 CooperativeMultitaskingTaskTimeout_Min = 100;  // millisec
        private const Int32 CooperativeMultitaskingTaskTimeout_Increment = 50;  // millisec
        private const Int32 CooperativeMultitaskingTaskTimeout_RootTask = (Int32.MaxValue / 2);

        private static Int32 GenerateCooperativeMultitaskingTaskTimeout()
        {
            // This logic ensures that we have a diversity of timeouts across worker tasks (100, 150, 200, 250, 100, etc)
            // Otherwise all worker will try to timeout at precisely the same point, which is bad if the work is just about to finish.
            Int32 period = PlatformHelper.ProcessorCount;
            Int32 pseudoRnd = Environment.TickCount;
            return CooperativeMultitaskingTaskTimeout_Min + (pseudoRnd % period) * CooperativeMultitaskingTaskTimeout_Increment;
        }
    }
}
