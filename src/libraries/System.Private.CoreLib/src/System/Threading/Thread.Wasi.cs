// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace System.Threading
{
    public sealed partial class Thread
    {
        // these methods are temporarily accessed via UnsafeAccessor from generated code until we have it in public API, probably in WASI preview3 and promises
        internal static Task RegisterWasiPollableHandle(int handle, bool ownsPollable, CancellationToken cancellationToken)
        {
            return WasiEventLoop.RegisterWasiPollableHandle(handle, ownsPollable, cancellationToken);
        }

        internal static void RegisterWasiPollHook(object? state, Func<object?, IList<int>> beforePollHook, Action<object?> onResolveCallback, CancellationToken cancellationToken)
        {
            WasiEventLoop.RegisterWasiPollHook(state, beforePollHook, onResolveCallback, cancellationToken);
        }

        internal static T PollWasiEventLoopUntilResolved<T>(Task<T> mainTask)
        {
            return WasiEventLoop.PollWasiEventLoopUntilResolved<T>(mainTask);
        }

        internal static void PollWasiEventLoopUntilResolvedVoid(Task mainTask)
        {
            WasiEventLoop.PollWasiEventLoopUntilResolvedVoid(mainTask);
        }
    }
}
