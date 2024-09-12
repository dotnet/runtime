// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using WasiPollWorld.wit.imports.wasi.io.v0_2_1;
using Pollable = WasiPollWorld.wit.imports.wasi.io.v0_2_1.IPoll.Pollable;
using MonotonicClockInterop = WasiPollWorld.wit.imports.wasi.clocks.v0_2_1.MonotonicClockInterop;

namespace System.Threading
{
    internal static class WasiEventLoop
    {
        // TODO: if the Pollable never resolves and and the Task is abandoned
        // it will be leaked and stay in this list forever.
        // it will also keep the Pollable handle alive and prevent it from being disposed
        private static readonly List<PollableHolder> s_pollables = new();
        private static bool s_checkScheduled;
        private static Pollable? s_resolvedPollable;
        private static Task? s_mainTask;

        internal static Task RegisterWasiPollableHandle(int handle, CancellationToken cancellationToken)
        {
            // note that this is duplicate of the original Pollable
            // the original should have been neutralized without disposing the handle
            var pollableCpy = new Pollable(new Pollable.THandle(handle));
            return RegisterWasiPollable(pollableCpy, cancellationToken);
        }

        internal static Task RegisterWasiPollable(Pollable pollable, CancellationToken cancellationToken)
        {
            // this will register the pollable holder into s_pollables
            var holder = new PollableHolder(pollable, cancellationToken);
            s_pollables.Add(holder);

            ScheduleCheck();

            return holder.taskCompletionSource.Task;
        }


        internal static T PollWasiEventLoopUntilResolved<T>(Task<T> mainTask)
        {
            try
            {
                s_mainTask = mainTask;
                while (!mainTask.IsCompleted)
                {
                    ThreadPoolWorkQueue.Dispatch();
                }
            }
            finally
            {
                s_mainTask = null;
            }
            var exception = mainTask.Exception;
            if (exception is not null)
            {
                throw exception;
            }

            return mainTask.Result;
        }

        internal static void PollWasiEventLoopUntilResolvedVoid(Task mainTask)
        {
            try
            {
                s_mainTask = mainTask;
                while (!mainTask.IsCompleted)
                {
                    ThreadPoolWorkQueue.Dispatch();
                }
            }
            finally
            {
                s_mainTask = null;
            }

            var exception = mainTask.Exception;
            if (exception is not null)
            {
                throw exception;
            }
        }

        internal static void ScheduleCheck()
        {
            if (!s_checkScheduled && s_pollables.Count > 0)
            {
                s_checkScheduled = true;
                ThreadPool.UnsafeQueueUserWorkItem(CheckPollables, null);
            }
        }

        internal static void CheckPollables(object? _)
        {
            s_checkScheduled = false;

            var holders = new List<PollableHolder>(s_pollables.Count);
            var pending = new List<Pollable>(s_pollables.Count);
            for (int i = 0; i < s_pollables.Count; i++)
            {
                var holder = s_pollables[i];
                if (!holder.isDisposed)
                {
                    holders.Add(holder);
                    pending.Add(holder.pollable);
                }
            }

            s_pollables.Clear();

            if (pending.Count > 0)
            {
                var resolvedPollableIndex = -1;
                // if there is CPU-bound work to do, we should not block on PollInterop.Poll below
                // so we will append pollable resolved in 0ms
                // in effect, the PollInterop.Poll would not block us
                if (ThreadPool.PendingWorkItemCount > 0 || (s_mainTask != null && s_mainTask.IsCompleted))
                {
                    s_resolvedPollable ??= MonotonicClockInterop.SubscribeDuration(0);
                    resolvedPollableIndex = pending.Count;
                    pending.Add(s_resolvedPollable);
                }

                var readyIndexes = PollInterop.Poll(pending);
                for (int i = 0; i < readyIndexes.Length; i++)
                {
                    uint readyIndex = readyIndexes[i];
                    if (resolvedPollableIndex != readyIndex)
                    {
                        var holder = holders[(int)readyIndex];
                        holder.ResolveAndDispose();
                    }
                }

                for (int i = 0; i < holders.Count; i++)
                {
                    PollableHolder holder = holders[i];
                    if (!holder.isDisposed)
                    {
                        s_pollables.Add(holder);
                    }
                }

                ScheduleCheck();
            }
        }

        private sealed class PollableHolder
        {
            public bool isDisposed;
            public readonly Pollable pollable;
            public readonly TaskCompletionSource taskCompletionSource;
            public readonly CancellationTokenRegistration cancellationTokenRegistration;
            public readonly CancellationToken cancellationToken;

            public PollableHolder(Pollable pollable, CancellationToken cancellationToken)
            {
                this.pollable = pollable;
                this.cancellationToken = cancellationToken;

                // this means that taskCompletionSource.Task.AsyncState -> this;
                // which means PollableHolder will be alive until the Task alive
                taskCompletionSource = new TaskCompletionSource(this);

                // static method is used to avoid allocating a delegate
                cancellationTokenRegistration = cancellationToken.Register(CancelAndDispose, this);
            }

            public void ResolveAndDispose()
            {
                if (isDisposed)
                {
                    return;
                }

                // no need to unregister the holder from s_pollables, when this is called
                isDisposed = true;
                pollable.Dispose();
                cancellationTokenRegistration.Dispose();
                taskCompletionSource.TrySetResult();
            }

            // for GC of abandoned Tasks or for cancellation
            public static void CancelAndDispose(object? s)
            {
                PollableHolder self = (PollableHolder)s!;
                if (self.isDisposed)
                {
                    return;
                }

                // it will be removed from s_pollables on the next run
                self.isDisposed = true;
                self.pollable.Dispose();
                self.cancellationTokenRegistration.Dispose();
                self.taskCompletionSource.TrySetCanceled(self.cancellationToken);
            }
        }
    }
}
