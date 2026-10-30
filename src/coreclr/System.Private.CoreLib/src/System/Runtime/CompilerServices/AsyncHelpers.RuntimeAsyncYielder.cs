// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        private sealed class RuntimeAsyncYielder : ICriticalNotifyCompletion
        {
            public static readonly RuntimeAsyncYielder Instance = new();

            public void OnCompleted(Action continuation) => throw new NotSupportedException();

            public void UnsafeOnCompleted(Action continuation)
            {
                if (TplEventSource.Log.IsEnabled())
                {
                    default(YieldAwaitable.YieldAwaiter).UnsafeOnCompleted(continuation);
                    return;
                }

                object continuationTarget = continuation.GetTargetForSingleCastInstanceDelegate();
                Debug.Assert(
                    continuationTarget != null &&
                    continuationTarget.GetType().HasSameMetadataDefinitionAs(typeof(RuntimeAsyncTask<>)));

                Task raTask = (Task)continuationTarget;

                SynchronizationContext? syncCtx = SynchronizationContext.Current;
                if (syncCtx != null && syncCtx.GetType() != typeof(SynchronizationContext))
                {
                    syncCtx.Post(static s => ((Task)s!).ExecuteDirectly(null), raTask);
                }
                else
                {
                    TaskScheduler scheduler = TaskScheduler.Current;
                    if (scheduler == TaskScheduler.Default)
                    {
                        ThreadPool.UnsafeQueueUserWorkItemInternal(raTask, preferLocal: false);
                    }
                    else
                    {
                        Task.Factory.StartNew(static s => ((Task)s!).ExecuteDirectly(null), raTask, default, TaskCreationOptions.PreferFairness, scheduler);
                    }
                }
            }
        }
    }
}
