// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.Channels
{
    internal static partial class ChannelUtilities
    {
        internal static void UnsafeQueueUserWorkItem(Action<object?> action, object? state) =>
            // No overload of {Unsafe}QueueUserWorkItem accepts an Action, only a WaitCallback.
            // To avoid allocating an extra object, use QueueUserWorkItem, which uses Task,
            // which does support Action<object>.
            QueueUserWorkItem(action, state);

        internal static void UnsafeQueueUserWorkItem<TState>(Action<TState> action, TState state)
        {
            ThreadPool.UnsafeQueueUserWorkItem(static tuple =>
            {
                var args = (Tuple<Action<TState>, TState>)tuple;
                args.Item1(args.Item2);
            }, Tuple.Create(action, state));
        }

        internal static void QueueUserWorkItem(Action<object?> action, object? state) =>
            Task.Factory.StartNew(action, state, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
    }
}
