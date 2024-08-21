// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using WasiPollWorld.wit.imports.wasi.io.v0_2_1;
using Pollable = WasiPollWorld.wit.imports.wasi.io.v0_2_1.IPoll.Pollable;

namespace System.Threading
{
    internal static class WasiEventLoop
    {
        private static readonly List<WeakReference<PollableHolder>> s_weakRefHolders = new();

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
            s_weakRefHolders.Add(holder.selfWeakRef);
            return holder.taskCompletionSource.Task;
        }

        // this is not thread safe
        internal static void DispatchWasiEventLoop()
        {
            ThreadPoolWorkQueue.Dispatch();

            var holders = new List<PollableHolder>(s_weakRefHolders.Count);
            var pending = new List<Pollable>(s_weakRefHolders.Count);
            for (int i = 0; i < s_weakRefHolders.Count; i++)
            {
                var weakRef = s_weakRefHolders[i];
                if (weakRef.TryGetTarget(out var holder))
                {
                    if (!holder.isDisposed)
                    {
                        holders.Add(holder);
                        pending.Add(holder.pollable);
                    }
                }
            }

            s_weakRefHolders.Clear();

            if (pending.Count > 0)
            {
                var readyIndexes = PollInterop.Poll(pending);
                for (int i = 0; i < readyIndexes.Length; i++)
                {
                    uint readyIndex = readyIndexes[i];
                    var holder = holders[(int)readyIndex];
                    holder.ResolveAndDispose();
                }
                for (int i = 0; i < holders.Count; i++)
                {
                    PollableHolder holder = holders[i];
                    if (!holder.isDisposed)
                    {
                        s_weakRefHolders.Add(holder.selfWeakRef);
                    }
                }
            }
        }

        private sealed class PollableHolder
        {
            public bool isDisposed;
            public readonly Pollable pollable;
            public readonly TaskCompletionSource taskCompletionSource;
            public readonly WeakReference<PollableHolder> selfWeakRef;
            public readonly CancellationTokenRegistration cancellationTokenRegistration;

            public PollableHolder(Pollable pollable, CancellationToken cancellationToken)
            {
                this.pollable = pollable;

                // this means that taskCompletionSource.Task.AsyncState -> this;
                // which means PollableHolder will be alive until the Task alive
                taskCompletionSource = new TaskCompletionSource(this);

                // static method is used to avoid allocating a delegate
                cancellationTokenRegistration = cancellationToken.Register(CancelAndDispose, this);

                // don't allocate it for each re-registration
                selfWeakRef = new WeakReference<PollableHolder>(this);
            }

            public void ResolveAndDispose()
            {
                if (isDisposed)
                {
                    return;
                }

                // no need to unregister the holder from s_pollables, when this is called
                isDisposed = true;
                taskCompletionSource.TrySetResult();
                pollable.Dispose();
                cancellationTokenRegistration.Dispose();
                GC.SuppressFinalize(this);
            }

            // for GC of abandoned Tasks or for cancellation
            private static void CancelAndDispose(object? s)
            {
                PollableHolder self = (PollableHolder)s!;
                if (self.isDisposed)
                {
                    return;
                }

                // it will be removed from s_pollables on the next run
                self.isDisposed = true;
                self.taskCompletionSource.TrySetCanceled();
                self.pollable.Dispose();
                self.cancellationTokenRegistration.Dispose();
                GC.SuppressFinalize(self);
            }

            ~PollableHolder()
            {
                CancelAndDispose(this);
            }
        }
    }
}
