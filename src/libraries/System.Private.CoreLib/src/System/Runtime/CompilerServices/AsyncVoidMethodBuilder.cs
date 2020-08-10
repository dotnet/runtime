// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Provides a builder for asynchronous methods that return void.
    /// This type is intended for compiler use only.
    /// </summary>
    public struct AsyncVoidMethodBuilder
    {
        /// <summary>The synchronization context associated with this operation.</summary>
        private SynchronizationContext? _synchronizationContext;
        /// <summary>The builder this void builder wraps.</summary>
        private AsyncTaskMethodBuilder _builder; // mutable struct: must not be readonly

        /// <summary>Initializes a new <see cref="AsyncVoidMethodBuilder"/>.</summary>
        /// <returns>The initialized <see cref="AsyncVoidMethodBuilder"/>.</returns>
        public static AsyncVoidMethodBuilder Create()
        {
            SynchronizationContext? sc = SynchronizationContext.Current;
            sc?.OperationStarted();

            // _builder should be initialized to AsyncTaskMethodBuilder.Create(), but on coreclr
            // that Create() is a nop, so we can just return the default here.
            return new AsyncVoidMethodBuilder() { _synchronizationContext = sc };
        }

        /// <summary>Initiates the builder's execution with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        [DebuggerStepThrough]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the state machine it represents.</summary>
        /// <param name="stateMachine">The heap-allocated state machine object.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="stateMachine"/> argument was null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is incorrectly initialized.</exception>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            _builder.SetStateMachine(stateMachine);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _builder.AwaitOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>
        /// Schedules the specified state machine to be pushed forward when the specified awaiter completes.
        /// </summary>
        /// <typeparam name="TAwaiter">Specifies the type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">Specifies the type of the state machine.</typeparam>
        /// <param name="awaiter">The awaiter.</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine =>
            _builder.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine);

        /// <summary>Completes the method builder successfully.</summary>
        public void SetResult()
        {
            if (TplEventSource.Log.IsEnabled())
            {
                TplEventSource.Log.TraceOperationEnd(this.Task.Id, AsyncCausalityStatus.Completed);
            }

            // Mark the builder as completed.  As this is a void-returning method, this mostly
            // doesn't matter, but it can affect things like debug events related to finalization.
            _builder.SetResult();

            if (_synchronizationContext != null)
            {
                NotifySynchronizationContextOfCompletion();
            }
        }

        /// <summary>Faults the method builder with an exception.</summary>
        /// <param name="exception">The exception that is the cause of this fault.</param>
        /// <exception cref="System.ArgumentNullException">The <paramref name="exception"/> argument is null (Nothing in Visual Basic).</exception>
        /// <exception cref="System.InvalidOperationException">The builder is not initialized.</exception>
        public void SetException(Exception exception)
        {
            if (exception == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
            }

            if (TplEventSource.Log.IsEnabled())
            {
                TplEventSource.Log.TraceOperationEnd(this.Task.Id, AsyncCausalityStatus.Error);
            }

            if (_synchronizationContext != null)
            {
                // If we captured a synchronization context, Post the throwing of the exception to it
                // and decrement its outstanding operation count.
                try
                {
                    System.Threading.Tasks.Task.ThrowAsync(exception, targetContext: _synchronizationContext);
                }
                finally
                {
                    NotifySynchronizationContextOfCompletion();
                }
            }
            else
            {
                // Otherwise, queue the exception to be thrown on the ThreadPool.  This will
                // result in a crash unless legacy exception behavior is enabled by a config
                // file or a CLR host.
                System.Threading.Tasks.Task.ThrowAsync(exception, targetContext: null);
            }

            // The exception was propagated already; we don't need or want to fault the builder, just mark it as completed.
            _builder.SetResult();
        }

        /// <summary>Notifies the current synchronization context that the operation completed.</summary>
        private void NotifySynchronizationContextOfCompletion()
        {
            Debug.Assert(_synchronizationContext != null, "Must only be used with a non-null context.");
            try
            {
                _synchronizationContext.OperationCompleted();
            }
            catch (Exception exc)
            {
                // If the interaction with the SynchronizationContext goes awry,
                // fall back to propagating on the ThreadPool.
                Task.ThrowAsync(exc, targetContext: null);
            }
        }

        /// <summary>Lazily instantiate the Task in a non-thread-safe manner.</summary>
        private Task Task => _builder.Task;

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and AsyncCausalityTracer in a single-threaded manner.
        /// </remarks>
        internal object ObjectIdForDebugger => _builder.ObjectIdForDebugger;
    }
}
