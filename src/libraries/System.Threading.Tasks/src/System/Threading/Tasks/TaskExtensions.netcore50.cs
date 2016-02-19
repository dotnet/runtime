// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // Creates a proxy Task and hooks up the logic to have it represent the task.Result
            Task promise = Task.CreateUnwrapPromise<VoidResult>(task, lookForOce: false);

            // Return the proxy immediately
            return promise;
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
            if (task == null)
            {
                throw new ArgumentNullException(nameof(task));
            }

            // Creates a proxy Task<TResult> and hooks up the logic to have it represent the task.Result
            Task<TResult> promise = Task.CreateUnwrapPromise<TResult>(task, lookForOce: false);

            // Return the proxy immediately
            return promise;
        }

        // Used as a placeholder TResult to indicate that a Task<TResult> has a void TResult
        private struct VoidResult { }
    }
}
