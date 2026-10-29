// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Threading.Tasks;

public partial class TaskFactory<TResult>
{
    private static Task<TResult> FromAsyncImplCore(
        IAsyncResult asyncResult,
        Func<IAsyncResult, TResult>? endFunction,
        Action<IAsyncResult>? endAction,
        TaskCreationOptions creationOptions,
        TaskScheduler scheduler)
    {
        Task<TResult> promise = new Task<TResult>((object?)null, creationOptions);

        if (TplEventSource.Log.IsEnabled())
            TplEventSource.Log.TraceOperationBegin(promise.Id, "TaskFactory.FromAsync", 0);

        if (Task.s_asyncDebuggingEnabled)
            Task.AddToActiveTasks(promise);

        // Just specify this task as detached. No matter what happens, we want endMethod
        // to be called -- even if the parent is canceled. So we don't want to flow
        // RespectParentCancellation.
        Task t = new Task(new Action<object>(delegate
        {
            FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: true);
        }),
            (object?)null, null,
            default, TaskCreationOptions.None, InternalTaskOptions.None, null);

        if (TplEventSource.Log.IsEnabled())
            TplEventSource.Log.TraceOperationBegin(t.Id, "TaskFactory.FromAsync Callback", 0);

        if (Task.s_asyncDebuggingEnabled)
            Task.AddToActiveTasks(t);

        if (asyncResult.IsCompleted)
        {
            try { t.InternalRunSynchronously(scheduler, waitForCompletion: false); }
            catch (Exception e) { promise.TrySetException(e); } // catch and log any scheduler exceptions
        }
        else
        {
            RuntimeFeature.ThrowIfBlockingIsNotSupported();
#pragma warning disable CA1416 // Validate platform compatibility, issue: https://github.com/dotnet/runtime/issues/44544
            ThreadPool.RegisterWaitForSingleObject(
                asyncResult.AsyncWaitHandle,
                delegate
                {
                    try { t.InternalRunSynchronously(scheduler, waitForCompletion: false); }
                    catch (Exception e) { promise.TrySetException(e); } // catch and log any scheduler exceptions
                },
                null,
                Timeout.Infinite,
                true);
#pragma warning restore CA1416
        }

        return promise;
    }
}
