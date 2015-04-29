// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides a set of static (Shared in Visual Basic) methods for working with specific kinds of 
    /// <see cref="System.Threading.Tasks.Task"/> instances.
    /// </summary>
    public static class TaskExtensions
    {
        /// <summary>
        /// Creates a proxy <see cref="System.Threading.Tasks.Task">Task</see> that represents the 
        /// asynchronous operation of a Task{Task}.
        /// </summary>
        /// <remarks>
        /// It is often useful to be able to return a Task from a <see cref="System.Threading.Tasks.Task{TResult}">
        /// Task{TResult}</see>, where the inner Task represents work done as part of the outer Task{TResult}.  However, 
        /// doing so results in a Task{Task}, which, if not dealt with carefully, could produce unexpected behavior.  Unwrap 
        /// solves this problem by creating a proxy Task that represents the entire asynchronous operation of such a Task{Task}.
        /// </remarks>
        /// <param name="task">The Task{Task} to unwrap.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown if the 
        /// <paramref name="task"/> argument is null.</exception>
        /// <returns>A Task that represents the asynchronous operation of the provided Task{Task}.</returns>
        public static Task Unwrap(this Task<Task> task)
        {
            if (task == null) throw new ArgumentNullException("task");

            bool result;

            // tcs.Task serves as a proxy for task.Result.
            // AttachedToParent is the only legal option for TCS-style task.
            var tcs = new TaskCompletionSource<Task>(task.CreationOptions & TaskCreationOptions.AttachedToParent);

            // Set up some actions to take when task has completed.
            task.ContinueWith(delegate
            {
                switch (task.Status)
                {
                    // If task did not run to completion, then record the cancellation/fault information
                    // to tcs.Task.
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                        result = tcs.TrySetFromTask(task);
                        Debug.Assert(result, "Unwrap(Task<Task>): Expected TrySetFromTask #1 to succeed");
                        break;

                    case TaskStatus.RanToCompletion:
                        // task.Result == null ==> proxy should be canceled.
                        if (task.Result == null) tcs.TrySetCanceled();

                        // When task.Result completes, take some action to set the completion state of tcs.Task.
                        else
                        {
                            task.Result.ContinueWith(_ =>
                            {
                                // Copy completion/cancellation/exception info from task.Result to tcs.Task.
                                result = tcs.TrySetFromTask(task.Result);
                                Debug.Assert(result, "Unwrap(Task<Task>): Expected TrySetFromTask #2 to succeed");
                            }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default)
                                .ContinueWith(antecedent =>
                                {
                                    // Clean up if ContinueWith() operation fails due to TSE
                                    tcs.TrySetException(antecedent.Exception);
                                }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
                        }
                        break;
                }
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default).ContinueWith(antecedent =>
            {
                // Clean up if ContinueWith() operation fails due to TSE
                tcs.TrySetException(antecedent.Exception);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);

            // Return this immediately as a proxy.  When task.Result completes, or task is faulted/canceled,
            // the completion information will be transfered to tcs.Task.
            return tcs.Task;
        }

        /// <summary>
        /// Creates a proxy <see cref="System.Threading.Tasks.Task{TResult}">Task{TResult}</see> that represents the 
        /// asynchronous operation of a Task{Task{TResult}}.
        /// </summary>
        /// <remarks>
        /// It is often useful to be able to return a Task{TResult} from a Task{TResult}, where the inner Task{TResult} 
        /// represents work done as part of the outer Task{TResult}.  However, doing so results in a Task{Task{TResult}}, 
        /// which, if not dealt with carefully, could produce unexpected behavior.  Unwrap solves this problem by 
        /// creating a proxy Task{TResult} that represents the entire asynchronous operation of such a Task{Task{TResult}}.
        /// </remarks>
        /// <param name="task">The Task{Task{TResult}} to unwrap.</param>
        /// <exception cref="T:System.ArgumentNullException">The exception that is thrown if the 
        /// <paramref name="task"/> argument is null.</exception>
        /// <returns>A Task{TResult} that represents the asynchronous operation of the provided Task{Task{TResult}}.</returns>        
        public static Task<TResult> Unwrap<TResult>(this Task<Task<TResult>> task)
        {
            if (task == null) throw new ArgumentNullException("task");

            bool result;

            // tcs.Task serves as a proxy for task.Result.
            // AttachedToParent is the only legal option for TCS-style task.
            var tcs = new TaskCompletionSource<TResult>(task.CreationOptions & TaskCreationOptions.AttachedToParent);

            // Set up some actions to take when task has completed.
            task.ContinueWith(delegate
            {
                switch (task.Status)
                {
                    // If task did not run to completion, then record the cancellation/fault information
                    // to tcs.Task.
                    case TaskStatus.Canceled:
                    case TaskStatus.Faulted:
                        result = tcs.TrySetFromTask(task);
                        Debug.Assert(result, "Unwrap(Task<Task<T>>): Expected TrySetFromTask #1 to succeed");
                        break;

                    case TaskStatus.RanToCompletion:
                        // task.Result == null ==> proxy should be canceled.
                        if (task.Result == null) tcs.TrySetCanceled();

                        // When task.Result completes, take some action to set the completion state of tcs.Task.
                        else
                        {
                            task.Result.ContinueWith(_ =>
                            {
                                // Copy completion/cancellation/exception info from task.Result to tcs.Task.
                                result = tcs.TrySetFromTask(task.Result);
                                Debug.Assert(result, "Unwrap(Task<Task<T>>): Expected TrySetFromTask #2 to succeed");
                            },
                            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default)
                                .ContinueWith(antecedent =>
                                {
                                    // Clean up if ContinueWith() operation fails due to TSE
                                    tcs.TrySetException(antecedent.Exception);
                                }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default);
                        }

                        break;
                }
            }, TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Current).ContinueWith(antecedent =>
            {
                // Clean up if ContinueWith() operation fails due to TSE
                tcs.TrySetException(antecedent.Exception);
            }, CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.DenyChildAttach, TaskScheduler.Default); ;

            // Return this immediately as a proxy.  When task.Result completes, or task is faulted/canceled,
            // the completion information will be transfered to tcs.Task.
            return tcs.Task;
        }

        // Transfer the completion status from "source" to "me".
        private static bool TrySetFromTask<TResult>(this TaskCompletionSource<TResult> me, Task source)
        {
            Debug.Assert(source.IsCompleted, "TrySetFromTask: Expected source to have completed.");
            bool rval = false;

            switch(source.Status)
            {
                case TaskStatus.Canceled:
                    rval = me.TrySetCanceled();
                    break;

                case TaskStatus.Faulted:
                    rval = me.TrySetException(source.Exception.InnerExceptions);
                    break;

                case TaskStatus.RanToCompletion:
                    if(source is Task<TResult>)
                        rval = me.TrySetResult( ((Task<TResult>)source).Result);
                    else
                        rval = me.TrySetResult(default(TResult));
                    break;
            }

            return rval;
        }
    }
}