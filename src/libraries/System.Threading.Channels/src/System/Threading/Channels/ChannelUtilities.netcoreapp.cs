// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Channels
{
    internal static partial class ChannelUtilities
    {
        internal static void UnsafeQueueUserWorkItem<TState>(Action<TState> action, TState state) =>
            ThreadPool.UnsafeQueueUserWorkItem(action, state, preferLocal: false);

        internal static void QueueUserWorkItem(Action<object?> action, object? state) =>
            ThreadPool.QueueUserWorkItem(action, state, preferLocal: false);
    }
}
