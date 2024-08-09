// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using WasiPollWorld.wit.imports.wasi.io.v0_2_1;

namespace System.Threading
{
    internal static class WasiEventLoop
    {
        private static List<WeakReference<TaskCompletionSource>> s_pollables = new();

        internal static Task RegisterWasiPollableHandle(int handle)
        {
            // note that this is duplicate of the original Pollable
            // the original should be neutralized without disposing the handle
            var pollableCpy = new IPoll.Pollable(new IPoll.Pollable.THandle(handle));
            return RegisterWasiPollable(pollableCpy);
        }

        internal static Task RegisterWasiPollable(IPoll.Pollable pollable)
        {
            var tcs = new TaskCompletionSource(pollable);
            var weakRef = new WeakReference<TaskCompletionSource>(tcs);
            s_pollables.Add(weakRef);
            return tcs.Task;
        }

        internal static void DispatchWasiEventLoop()
        {
            ThreadPoolWorkQueue.Dispatch();

            if (s_pollables.Count > 0)
            {
                var pollables = s_pollables;
                s_pollables = new List<WeakReference<TaskCompletionSource>>(pollables.Count);
                var arguments = new List<IPoll.Pollable>(pollables.Count);
                var indexes = new List<int>(pollables.Count);
                for (var i = 0; i < pollables.Count; i++)
                {
                    var weakRef = pollables[i];
                    if (weakRef.TryGetTarget(out TaskCompletionSource? tcs))
                    {
                        var pollable = (IPoll.Pollable)tcs!.Task.AsyncState!;
                        arguments.Add(pollable);
                        indexes.Add(i);
                    }
                }

                // this is blocking until at least one pollable resolves
                var readyIndexes = PollInterop.Poll(arguments);

                var ready = new bool[arguments.Count];
                foreach (int readyIndex in readyIndexes)
                {
                    ready[readyIndex] = true;
                    arguments[readyIndex].Dispose();
                    var weakRef = pollables[indexes[readyIndex]];
                    if (weakRef.TryGetTarget(out TaskCompletionSource? tcs))
                    {
                        tcs!.SetResult();
                    }
                }
                for (var i = 0; i < arguments.Count; ++i)
                {
                    if (!ready[i])
                    {
                        s_pollables.Add(pollables[indexes[i]]);
                    }
                }
            }
        }
    }
}
