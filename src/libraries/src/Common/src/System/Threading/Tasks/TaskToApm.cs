// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// Helper methods for using Tasks to implement the APM pattern.
//
// Example usage, wrapping a Task<int>-returning FooAsync method with Begin/EndFoo methods:
//
//     public IAsyncResult BeginFoo(..., AsyncCallback callback, object state)
//     {
//         Task<int> t = FooAsync(...);
//         return TaskToApm.Begin(t, callback, state);
//     }
//     public int EndFoo(IAsyncResult asyncResult)
//     {
//         return TaskToApm.End<int>(asyncResult);
//     }

using System.Diagnostics;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides support for efficiently using Tasks to implement the APM (Begin/End) pattern.
    /// </summary>
    internal static class TaskToApm
    {
        /// <summary>
        /// Marshals the Task as an IAsyncResult, using the supplied callback and state
        /// to implement the APM pattern.
        /// </summary>
        /// <param name="task">The Task to be marshaled.</param>
        /// <param name="callback">The callback to be invoked upon completion.</param>
        /// <param name="state">The state to be stored in the IAsyncResult.</param>
        /// <returns>An IAsyncResult to represent the task's asynchronous operation.</returns>
        public static IAsyncResult Begin(Task task, AsyncCallback callback, object state)
        {
            Debug.Assert(task != null);

            // If the task has already completed, then since the Task's CompletedSynchronously==false
            // and we want it to be true, we need to create a new IAsyncResult. (We also need the AsyncState to match.)
            IAsyncResult asyncResult;
            if (task.IsCompleted)
            {
                // Synchronous completion.
                asyncResult = new TaskWrapperAsyncResult(task, state, completedSynchronously: true);
                callback?.Invoke(asyncResult);
            }
            else
            {
                // For asynchronous completion we need to schedule a callback.  Whether we can use the Task as the IAsyncResult
                // depends on whether the Task's AsyncState has reference equality with the requested state.
                asyncResult = task.AsyncState == state ? (IAsyncResult)task : new TaskWrapperAsyncResult(task, state, completedSynchronously: false);
                if (callback != null)
                {
                    InvokeCallbackWhenTaskCompletes(task, callback, asyncResult);
                }
            }
            return asyncResult;
        }

        /// <summary>Processes an IAsyncResult returned by Begin.</summary>
        /// <param name="asyncResult">The IAsyncResult to unwrap.</param>
        public static void End(IAsyncResult asyncResult)
        {
            Task task;

            // If the IAsyncResult is our task-wrapping IAsyncResult, extract the Task.
            var twar = asyncResult as TaskWrapperAsyncResult;
            if (twar != null)
            {
                task = twar.Task;
                Debug.Assert(task != null, "TaskWrapperAsyncResult should never wrap a null Task.");
            }
            else
            {
                // Otherwise, the IAsyncResult should be a Task.
                task = asyncResult as Task;
            }

            // Make sure we actually got a task, then complete the operation by waiting on it.
            if (task == null)
            {
                throw new ArgumentNullException();
            }

            task.GetAwaiter().GetResult();
        }

        /// <summary>Processes an IAsyncResult returned by Begin.</summary>
        /// <param name="asyncResult">The IAsyncResult to unwrap.</param>
        public static TResult End<TResult>(IAsyncResult asyncResult)
        {
            Task<TResult> task;

            // If the IAsyncResult is our task-wrapping IAsyncResult, extract the Task.
            var twar = asyncResult as TaskWrapperAsyncResult;
            if (twar != null)
            {
                task = twar.Task as Task<TResult>;
                Debug.Assert(twar.Task != null, "TaskWrapperAsyncResult should never wrap a null Task.");
            }
            else
            {
                // Otherwise, the IAsyncResult should be a Task<TResult>.
                task = asyncResult as Task<TResult>;
            }

            // Make sure we actually got a task, then complete the operation by waiting on it.
            if (task == null)
            {
                throw new ArgumentNullException();
            }

            return task.GetAwaiter().GetResult();
        }

        /// <summary>Invokes the callback asynchronously when the task has completed.</summary>
        /// <param name="antecedent">The Task to await.</param>
        /// <param name="callback">The callback to invoke when the Task completes.</param>
        /// <param name="asyncResult">The Task used as the IAsyncResult.</param>
        private static void InvokeCallbackWhenTaskCompletes(Task antecedent, AsyncCallback callback, IAsyncResult asyncResult)
        {
            Debug.Assert(antecedent != null);
            Debug.Assert(callback != null);
            Debug.Assert(asyncResult != null);

            // We use OnCompleted rather than ContinueWith in order to avoid running synchronously
            // if the task has already completed by the time we get here.  This is separated out into
            // its own method currently so that we only pay for the closure if necessary.
            antecedent.ConfigureAwait(continueOnCapturedContext: false)
                      .GetAwaiter()
                      .OnCompleted(() => callback(asyncResult));

            // PERFORMANCE NOTE:
            // Assuming we're in the default ExecutionContext, the "slow path" of an incomplete
            // task will result in four allocations: the new IAsyncResult,  the delegate+closure
            // in this method, and the continuation object inside of OnCompleted (necessary
            // to capture both the Action delegate and the ExecutionContext in a single object).  
            // In the future, if performance requirements drove a need, those four 
            // allocations could be reduced to one.  This would be achieved by having TaskWrapperAsyncResult
            // also implement ITaskCompletionAction (and optionally IThreadPoolWorkItem).  It would need
            // additional fields to store the AsyncCallback and an ExecutionContext.  Once configured, 
            // it would be set into the Task as a continuation.  Its Invoke method would then be run when 
            // the antecedent completed, and, doing all of the necessary work to flow ExecutionContext, 
            // it would invoke the AsyncCallback.  It could also have a field on it for the antecedent, 
            // so that the End method would have access to the completed antecedent. For related examples, 
            // see other implementations of ITaskCompletionAction, and in particular ReadWriteTask 
            // used in Stream.Begin/EndXx's implementation.
        }

        /// <summary>
        /// Provides a simple IAsyncResult that wraps a Task.  This, in effect, allows
        /// for overriding what's seen for the CompletedSynchronously and AsyncState values.
        /// </summary>
        private sealed class TaskWrapperAsyncResult : IAsyncResult
        {
            /// <summary>The wrapped Task.</summary>
            internal readonly Task Task;
            /// <summary>The new AsyncState value.</summary>
            private readonly object _state;
            /// <summary>The new CompletedSynchronously value.</summary>
            private readonly bool _completedSynchronously;

            /// <summary>Initializes the IAsyncResult with the Task to wrap and the overriding AsyncState and CompletedSynchronously values.</summary>
            /// <param name="task">The Task to wrap.</param>
            /// <param name="state">The new AsyncState value</param>
            /// <param name="completedSynchronously">The new CompletedSynchronously value.</param>
            internal TaskWrapperAsyncResult(Task task, object state, bool completedSynchronously)
            {
                Debug.Assert(task != null);
                Debug.Assert(!completedSynchronously || task.IsCompleted, "If completedSynchronously is true, the task must be completed.");

                this.Task = task;
                _state = state;
                _completedSynchronously = completedSynchronously;
            }

            // The IAsyncResult implementation.  
            // - IsCompleted and AsyncWaitHandle just pass through to the Task.
            // - AsyncState and CompletedSynchronously return the corresponding values stored in this object.

            object IAsyncResult.AsyncState { get { return _state; } }
            bool IAsyncResult.CompletedSynchronously { get { return _completedSynchronously; } }
            bool IAsyncResult.IsCompleted { get { return this.Task.IsCompleted; } }
            WaitHandle IAsyncResult.AsyncWaitHandle { get { return ((IAsyncResult)this.Task).AsyncWaitHandle; } }
        }
    }
}
