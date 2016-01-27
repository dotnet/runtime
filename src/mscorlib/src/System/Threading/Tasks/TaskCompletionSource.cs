// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// =+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
//
//
//
// TaskCompletionSource<TResult> is the producer end of an unbound future.  Its
// Task member may be distributed as the consumer end of the future.
//
// =-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-

using System;
using System.Diagnostics.Contracts;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Security.Permissions;
using System.Threading;

// Disable the "reference to volatile field not treated as volatile" error.
#pragma warning disable 0420

namespace System.Threading.Tasks
{
    /// <summary>
    /// Represents the producer side of a <see cref="T:System.Threading.Tasks.Task{TResult}"/> unbound to a
    /// delegate, providing access to the consumer side through the <see cref="Task"/> property.
    /// </summary>
    /// <remarks>
    /// <para>
    /// It is often the case that a <see cref="T:System.Threading.Tasks.Task{TResult}"/> is desired to
    /// represent another asynchronous operation.
    /// <see cref="TaskCompletionSource{TResult}">TaskCompletionSource</see> is provided for this purpose. It enables
    /// the creation of a task that can be handed out to consumers, and those consumers can use the members
    /// of the task as they would any other. However, unlike most tasks, the state of a task created by a
    /// TaskCompletionSource is controlled explicitly by the methods on TaskCompletionSource. This enables the
    /// completion of the external asynchronous operation to be propagated to the underlying Task. The
    /// separation also ensures that consumers are not able to transition the state without access to the
    /// corresponding TaskCompletionSource.
    /// </para>
    /// <para>
    /// All members of <see cref="TaskCompletionSource{TResult}"/> are thread-safe
    /// and may be used from multiple threads concurrently.
    /// </para>
    /// </remarks>
    /// <typeparam name="TResult">The type of the result value assocatied with this <see
    /// cref="TaskCompletionSource{TResult}"/>.</typeparam>
    [HostProtection(Synchronization = true, ExternalThreading = true)]
    public class TaskCompletionSource<TResult>
    {
        private readonly Task<TResult> m_task;

        /// <summary>
        /// Creates a <see cref="TaskCompletionSource{TResult}"/>.
        /// </summary>
        public TaskCompletionSource()
        {
            m_task = new Task<TResult>();
        }

        /// <summary>
        /// Creates a <see cref="TaskCompletionSource{TResult}"/>
        /// with the specified options.
        /// </summary>
        /// <remarks>
        /// The <see cref="T:System.Threading.Tasks.Task{TResult}"/> created
        /// by this instance and accessible through its <see cref="Task"/> property
        /// will be instantiated using the specified <paramref name="creationOptions"/>.
        /// </remarks>
        /// <param name="creationOptions">The options to use when creating the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> represent options invalid for use
        /// with a <see cref="TaskCompletionSource{TResult}"/>.
        /// </exception>
        public TaskCompletionSource(TaskCreationOptions creationOptions)
            : this(null, creationOptions)
        {
        }

        /// <summary>
        /// Creates a <see cref="TaskCompletionSource{TResult}"/>
        /// with the specified state.
        /// </summary>
        /// <param name="state">The state to use as the underlying 
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>'s AsyncState.</param>
        public TaskCompletionSource(object state)
            : this(state, TaskCreationOptions.None)
        {
        }

        /// <summary>
        /// Creates a <see cref="TaskCompletionSource{TResult}"/> with
        /// the specified state and options.
        /// </summary>
        /// <param name="creationOptions">The options to use when creating the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <param name="state">The state to use as the underlying 
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/>'s AsyncState.</param>
        /// <exception cref="T:System.ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> represent options invalid for use
        /// with a <see cref="TaskCompletionSource{TResult}"/>.
        /// </exception>
        public TaskCompletionSource(object state, TaskCreationOptions creationOptions)
        {
            m_task = new Task<TResult>(state, creationOptions);
        }


        /// <summary>
        /// Gets the <see cref="T:System.Threading.Tasks.Task{TResult}"/> created
        /// by this <see cref="TaskCompletionSource{TResult}"/>.
        /// </summary>
        /// <remarks>
        /// This property enables a consumer access to the <see
        /// cref="T:System.Threading.Tasks.Task{TResult}"/> that is controlled by this instance.
        /// The <see cref="SetResult"/>, <see cref="SetException(System.Exception)"/>,
        /// <see cref="SetException(System.Collections.Generic.IEnumerable{System.Exception})"/>, and <see cref="SetCanceled"/>
        /// methods (and their "Try" variants) on this instance all result in the relevant state
        /// transitions on this underlying Task.
        /// </remarks>
        public Task<TResult> Task
        {
            get { return m_task; }
        }

        /// <summary>Spins until the underlying task is completed.</summary>
        /// <remarks>This should only be called if the task is in the process of being completed by another thread.</remarks>
        private void SpinUntilCompleted()
        {
            // Spin wait until the completion is finalized by another thread.
            var sw = new SpinWait();
            while (!m_task.IsCompleted) 
                sw.SpinOnce();
        }

        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>
        /// state.
        /// </summary>
        /// <param name="exception">The exception to bind to this <see 
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="exception"/> argument is null.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            bool rval = m_task.TrySetException(exception);
            if (!rval && !m_task.IsCompleted) SpinUntilCompleted();
            return rval;
        }

        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>
        /// state.
        /// </summary>
        /// <param name="exceptions">The collection of exceptions to bind to this <see 
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="exceptions"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">There are one or more null elements in <paramref name="exceptions"/>.</exception>
        /// <exception cref="T:System.ArgumentException">The <paramref name="exceptions"/> collection is empty.</exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetException(IEnumerable<Exception> exceptions)
        {
            if (exceptions == null) throw new ArgumentNullException("exceptions");
            
            List<Exception> defensiveCopy = new List<Exception>();
            foreach (Exception e in exceptions)
            {
                if (e == null)
                    throw new ArgumentException(Environment.GetResourceString("TaskCompletionSourceT_TrySetException_NullException"), "exceptions");
                defensiveCopy.Add(e);
            }

            if (defensiveCopy.Count == 0)
                throw new ArgumentException(Environment.GetResourceString("TaskCompletionSourceT_TrySetException_NoExceptions"), "exceptions");

            bool rval = m_task.TrySetException(defensiveCopy);
            if (!rval && !m_task.IsCompleted) SpinUntilCompleted();
            return rval;
        }

        /// <summary>Attempts to transition the underlying task to the faulted state.</summary>
        /// <param name="exceptions">The collection of exception dispatch infos to bind to this task.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>Unlike the public methods, this method doesn't currently validate that its arguments are correct.</remarks>
        internal bool TrySetException(IEnumerable<ExceptionDispatchInfo> exceptions)
        {
            Contract.Assert(exceptions != null);
#if DEBUG
            foreach(var edi in exceptions) Contract.Assert(edi != null, "Contents must be non-null");
#endif

            bool rval = m_task.TrySetException(exceptions);
            if (!rval && !m_task.IsCompleted) SpinUntilCompleted();
            return rval;
        }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>
        /// state.
        /// </summary>
        /// <param name="exception">The exception to bind to this <see 
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="exception"/> argument is null.</exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// The underlying <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetException(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException("exception");

            if (!TrySetException(exception))
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
            }
        }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>
        /// state.
        /// </summary>
        /// <param name="exceptions">The collection of exceptions to bind to this <see 
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="T:System.ArgumentNullException">The <paramref name="exceptions"/> argument is null.</exception>
        /// <exception cref="T:System.ArgumentException">There are one or more null elements in <paramref name="exceptions"/>.</exception>
        /// <exception cref="T:System.InvalidOperationException">
        /// The underlying <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetException(IEnumerable<Exception> exceptions)
        {
            if (!TrySetException(exceptions))
            {
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
            }
        }


        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>
        /// state.
        /// </summary>
        /// <param name="result">The result value to bind to this <see 
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetResult(TResult result)
        {
            bool rval = m_task.TrySetResult(result);
            if (!rval && !m_task.IsCompleted) SpinUntilCompleted();
            return rval;
        }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>
        /// state.
        /// </summary>
        /// <param name="result">The result value to bind to this <see 
        /// cref="T:System.Threading.Tasks.Task{TResult}"/>.</param>
        /// <exception cref="T:System.InvalidOperationException">
        /// The underlying <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetResult(TResult result)
        {
            if (!TrySetResult(result))
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
        }

        /// <summary>
        /// Attempts to transition the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>
        /// state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>This operation will return false if the 
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </remarks>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public bool TrySetCanceled()
        {
            return TrySetCanceled(default(CancellationToken));
        }

        // Enables a token to be stored into the canceled task
        public bool TrySetCanceled(CancellationToken cancellationToken)
        {
            bool rval = m_task.TrySetCanceled(cancellationToken);
            if (!rval && !m_task.IsCompleted) SpinUntilCompleted();
            return rval;
        }

        /// <summary>
        /// Transitions the underlying
        /// <see cref="T:System.Threading.Tasks.Task{TResult}"/> into the 
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>
        /// state.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// The underlying <see cref="T:System.Threading.Tasks.Task{TResult}"/> is already in one
        /// of the three final states:
        /// <see cref="System.Threading.Tasks.TaskStatus.RanToCompletion">RanToCompletion</see>, 
        /// <see cref="System.Threading.Tasks.TaskStatus.Faulted">Faulted</see>, or
        /// <see cref="System.Threading.Tasks.TaskStatus.Canceled">Canceled</see>.
        /// </exception>
        /// <exception cref="T:System.ObjectDisposedException">The <see cref="Task"/> was disposed.</exception>
        public void SetCanceled()
        {
            if(!TrySetCanceled())
                throw new InvalidOperationException(Environment.GetResourceString("TaskT_TransitionToFinal_AlreadyCompleted"));
        }
    }
}
