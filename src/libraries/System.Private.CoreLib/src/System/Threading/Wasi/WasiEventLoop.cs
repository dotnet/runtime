// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;
using WasiPollWorld.wit.imports.wasi.io.v0_2_0;

namespace System.Threading
{
    internal static class WasiEventLoop
    {
        private static List<(IPoll.Pollable, TaskCompletionSource)> pollables = new();

        internal static Task RegisterWasiPollable(int handle)
        {
            var source = new TaskCompletionSource(TaskCreationOptions.AttachedToParent);
            pollables.Add((new IPoll.Pollable(new IPoll.Pollable.THandle(handle)), source));
            return source.Task;
        }

        internal static void DispatchWasiEventLoop()
        {
            ThreadPoolWorkQueue.Dispatch();

            if (WasiEventLoop.pollables.Count > 0)
            {
                var pollables = WasiEventLoop.pollables;
                WasiEventLoop.pollables = new();
                var arguments = new List<IPoll.Pollable>();
                var sources = new List<TaskCompletionSource>();
                foreach ((var pollable, var source) in pollables)
                {
                    arguments.Add(pollable);
                    sources.Add(source);
                }
                var results = PollInterop.Poll(arguments);
                var ready = new bool[arguments.Count];
                foreach (var result in results)
                {
                    ready[result] = true;
                    arguments[(int)result].Dispose();
                    sources[(int)result].SetResult();
                }
                for (var i = 0; i < arguments.Count; ++i)
                {
                    if (!ready[i])
                    {
                        WasiEventLoop.pollables.Add((arguments[i], sources[i]));
                    }
                }
            }
        }
    }
}
