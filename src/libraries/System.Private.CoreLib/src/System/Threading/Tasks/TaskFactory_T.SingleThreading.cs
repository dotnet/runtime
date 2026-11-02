// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        Task.CheckPromiseCreationOptions(creationOptions);
        if (!asyncResult.IsCompleted)
        {
            throw new PlatformNotSupportedException();
        }

        return FromAsyncCompleted(asyncResult, endFunction, endAction, creationOptions, scheduler);
    }

    // Keep the captured callback allocation after the unsupported-wait check.
    private static Task<TResult> FromAsyncCompleted(
        IAsyncResult asyncResult,
        Func<IAsyncResult, TResult>? endFunction,
        Action<IAsyncResult>? endAction,
        TaskCreationOptions creationOptions,
        TaskScheduler scheduler)
    {
        Task<TResult> promise = new Task<TResult>((object?)null, creationOptions);

        if (TplEventSource.Log.IsEnabled())
        {
            TplEventSource.Log.TraceOperationBegin(promise.Id, "TaskFactory.FromAsync", 0);
        }
        if (Task.s_asyncDebuggingEnabled)
        {
            Task.AddToActiveTasks(promise);
        }

        Task callbackTask = new Task(new Action<object>(delegate
        {
            FromAsyncCoreLogic(asyncResult, endFunction, endAction, promise, requiresSynchronization: true);
        }),
            (object?)null, null,
            default, TaskCreationOptions.None, InternalTaskOptions.None, null);

        if (TplEventSource.Log.IsEnabled())
        {
            TplEventSource.Log.TraceOperationBegin(callbackTask.Id, "TaskFactory.FromAsync Callback", 0);
        }
        if (Task.s_asyncDebuggingEnabled)
        {
            Task.AddToActiveTasks(callbackTask);
        }

        try { callbackTask.InternalRunSynchronously(scheduler, waitForCompletion: false); }
        catch (Exception e) { promise.TrySetException(e); } // catch and log any scheduler exceptions

        return promise;
    }
}
