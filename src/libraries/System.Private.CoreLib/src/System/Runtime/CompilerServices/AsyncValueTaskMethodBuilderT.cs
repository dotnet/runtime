// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;
using Internal.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    /// <summary>Represents a builder for asynchronous methods that returns a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    [StructLayout(LayoutKind.Auto)]
    public struct AsyncValueTaskMethodBuilder<TResult>
    {
        /// <summary>Sentinel object used to indicate that the builder completed synchronously and successfully.</summary>
        /// <remarks>
        /// To avoid memory safety issues even in the face of invalid race conditions, we ensure that the type of this object
        /// is valid for the mode in which we're operating.  As such, it's cached on the generic builder per TResult
        /// rather than having one sentinel instance for all types.
        /// </remarks>
        internal static readonly object s_syncSuccessSentinel = AsyncTaskCache.s_valueTaskPoolingEnabled ? (object)
            new SyncSuccessSentinelStateMachineBox() :
            new Task<TResult>(default(TResult)!);

        /// <summary>The wrapped state machine or task.  If the operation completed synchronously and successfully, this will be a sentinel object compared by reference identity.</summary>
        private object? m_task; // Debugger depends on the exact name of this field.
        /// <summary>The result for this builder if it's completed synchronously, in which case <see cref="m_task"/> will be <see cref="s_syncSuccessSentinel"/>.</summary>
        private TResult _result;

        /// <summary>Creates an instance of the <see cref="AsyncValueTaskMethodBuilder{TResult}"/> struct.</summary>
        /// <returns>The initialized instance.</returns>
        public static AsyncValueTaskMethodBuilder<TResult> Create() => default;

        /// <summary>Begins running the builder with the associated state machine.</summary>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="stateMachine">The state machine instance, passed by reference.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine =>
            AsyncMethodBuilderCore.Start(ref stateMachine);

        /// <summary>Associates the builder with the specified state machine.</summary>
        /// <param name="stateMachine">The state machine instance to associate with the builder.</param>
        public void SetStateMachine(IAsyncStateMachine stateMachine) =>
            AsyncMethodBuilderCore.SetStateMachine(stateMachine, task: null);

        /// <summary>Marks the value task as successfully completed.</summary>
        /// <param name="result">The result to use to complete the value task.</param>
        public void SetResult(TResult result)
        {
            if (m_task is null)
            {
                _result = result;
                m_task = s_syncSuccessSentinel;
            }
            else if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                Unsafe.As<StateMachineBox>(m_task).SetResult(result);
            }
            else
            {
                AsyncTaskMethodBuilder<TResult>.SetExistingTaskResult(Unsafe.As<Task<TResult>>(m_task), result);
            }
        }

        /// <summary>Marks the value task as failed and binds the specified exception to the value task.</summary>
        /// <param name="exception">The exception to bind to the value task.</param>
        public void SetException(Exception exception)
        {
            if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                SetException(exception, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
            {
                AsyncTaskMethodBuilder<TResult>.SetException(exception, ref Unsafe.As<object?, Task<TResult>?>(ref m_task));
            }
        }

        internal static void SetException(Exception exception, [NotNull] ref StateMachineBox? boxFieldRef)
        {
            if (exception is null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.exception);
            }

            (boxFieldRef ??= CreateWeaklyTypedStateMachineBox()).SetException(exception);
        }

        /// <summary>Gets the value task for this builder.</summary>
        public ValueTask<TResult> Task
        {
            get
            {
                if (m_task == s_syncSuccessSentinel)
                {
                    return new ValueTask<TResult>(_result);
                }

                // With normal access paterns, m_task should always be non-null here: the async method should have
                // either completed synchronously, in which case SetResult would have set m_task to a non-null object,
                // or it should be completing asynchronously, in which case AwaitUnsafeOnCompleted would have similarly
                // initialized m_task to a state machine object.  However, if the type is used manually (not via
                // compiler-generated code) and accesses Task directly, we force it to be initialized.  Things will then
                // "work" but in a degraded mode, as we don't know the TStateMachine type here, and thus we use a box around
                // the interface instead.

                if (AsyncTaskCache.s_valueTaskPoolingEnabled)
                {
                    var box = Unsafe.As<StateMachineBox?>(m_task);
                    if (box is null)
                    {
                        m_task = box = CreateWeaklyTypedStateMachineBox();
                    }
                    return new ValueTask<TResult>(box, box.Version);
                }
                else
                {
                    var task = Unsafe.As<Task<TResult>?>(m_task);
                    if (task is null)
                    {
                        m_task = task = new Task<TResult>(); // base task used rather than box to minimize size when used as manual promise
                    }
                    return new ValueTask<TResult>(task);
                }
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">the awaiter</param>
        /// <param name="stateMachine">The state machine.</param>
        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                AwaitOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
            {
                AsyncTaskMethodBuilder<TResult>.AwaitOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, Task<TResult>?>(ref m_task));
            }
        }

        internal static void AwaitOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine, ref StateMachineBox? box)
            where TAwaiter : INotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            try
            {
                awaiter.OnCompleted(GetStateMachineBox(ref stateMachine, ref box).MoveNextAction);
            }
            catch (Exception e)
            {
                System.Threading.Tasks.Task.ThrowAsync(e, targetContext: null);
            }
        }

        /// <summary>Schedules the state machine to proceed to the next action when the specified awaiter completes.</summary>
        /// <typeparam name="TAwaiter">The type of the awaiter.</typeparam>
        /// <typeparam name="TStateMachine">The type of the state machine.</typeparam>
        /// <param name="awaiter">the awaiter</param>
        /// <param name="stateMachine">The state machine.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            if (AsyncTaskCache.s_valueTaskPoolingEnabled)
            {
                AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, StateMachineBox?>(ref m_task));
            }
            else
            {
                AsyncTaskMethodBuilder<TResult>.AwaitUnsafeOnCompleted(ref awaiter, ref stateMachine, ref Unsafe.As<object?, Task<TResult>?>(ref m_task));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(
            ref TAwaiter awaiter, ref TStateMachine stateMachine, [NotNull] ref StateMachineBox? boxRef)
            where TAwaiter : ICriticalNotifyCompletion
            where TStateMachine : IAsyncStateMachine
        {
            IAsyncStateMachineBox box = GetStateMachineBox(ref stateMachine, ref boxRef);
            AsyncTaskMethodBuilder<VoidTaskResult>.AwaitUnsafeOnCompleted(ref awaiter, box);
        }

        /// <summary>Gets the "boxed" state machine object.</summary>
        /// <typeparam name="TStateMachine">Specifies the type of the async state machine.</typeparam>
        /// <param name="stateMachine">The state machine.</param>
        /// <param name="boxFieldRef">A reference to the field containing the initialized state machine box.</param>
        /// <returns>The "boxed" state machine.</returns>
        private static IAsyncStateMachineBox GetStateMachineBox<TStateMachine>(
            ref TStateMachine stateMachine,
            [NotNull] ref StateMachineBox? boxFieldRef)
            where TStateMachine : IAsyncStateMachine
        {
            ExecutionContext? currentContext = ExecutionContext.Capture();

            // Check first for the most common case: not the first yield in an async method.
            // In this case, the first yield will have already "boxed" the state machine in
            // a strongly-typed manner into an AsyncStateMachineBox.  It will already contain
            // the state machine as well as a MoveNextDelegate and a context.  The only thing
            // we might need to do is update the context if that's changed since it was stored.
            if (boxFieldRef is StateMachineBox<TStateMachine> stronglyTypedBox)
            {
                if (stronglyTypedBox.Context != currentContext)
                {
                    stronglyTypedBox.Context = currentContext;
                }

                return stronglyTypedBox;
            }

            // The least common case: we have a weakly-typed boxed.  This results if the debugger
            // or some other use of reflection accesses a property like ObjectIdForDebugger.  In
            // such situations, we need to get an object to represent the builder, but we don't yet
            // know the type of the state machine, and thus can't use TStateMachine.  Instead, we
            // use the IAsyncStateMachine interface, which all TStateMachines implement.  This will
            // result in a boxing allocation when storing the TStateMachine if it's a struct, but
            // this only happens in active debugging scenarios where such performance impact doesn't
            // matter.
            if (boxFieldRef is StateMachineBox<IAsyncStateMachine> weaklyTypedBox)
            {
                // If this is the first await, we won't yet have a state machine, so store it.
                if (weaklyTypedBox.StateMachine is null)
                {
                    Debugger.NotifyOfCrossThreadDependency(); // same explanation as with usage below
                    weaklyTypedBox.StateMachine = stateMachine;
                }

                // Update the context.  This only happens with a debugger, so no need to spend
                // extra IL checking for equality before doing the assignment.
                weaklyTypedBox.Context = currentContext;
                return weaklyTypedBox;
            }

            // Alert a listening debugger that we can't make forward progress unless it slips threads.
            // If we don't do this, and a method that uses "await foo;" is invoked through funceval,
            // we could end up hooking up a callback to push forward the async method's state machine,
            // the debugger would then abort the funceval after it takes too long, and then continuing
            // execution could result in another callback being hooked up.  At that point we have
            // multiple callbacks registered to push the state machine, which could result in bad behavior.
            Debugger.NotifyOfCrossThreadDependency();

            // At this point, m_task should really be null, in which case we want to create the box.
            // However, in a variety of debugger-related (erroneous) situations, it might be non-null,
            // e.g. if the Task property is examined in a Watch window, forcing it to be lazily-intialized
            // as a Task<TResult> rather than as an ValueTaskStateMachineBox.  The worst that happens in such
            // cases is we lose the ability to properly step in the debugger, as the debugger uses that
            // object's identity to track this specific builder/state machine.  As such, we proceed to
            // overwrite whatever's there anyway, even if it's non-null.
            var box = StateMachineBox<TStateMachine>.GetOrCreateBox();
            boxFieldRef = box; // important: this must be done before storing stateMachine into box.StateMachine!
            box.StateMachine = stateMachine;
            box.Context = currentContext;

            return box;
        }

        /// <summary>
        /// Creates a box object for use when a non-standard access pattern is employed, e.g. when Task
        /// is evaluated in the debugger prior to the async method yielding for the first time.
        /// </summary>
        internal static StateMachineBox CreateWeaklyTypedStateMachineBox() => new StateMachineBox<IAsyncStateMachine>();

        /// <summary>
        /// Gets an object that may be used to uniquely identify this builder to the debugger.
        /// </summary>
        /// <remarks>
        /// This property lazily instantiates the ID in a non-thread-safe manner.
        /// It must only be used by the debugger and tracing purposes, and only in a single-threaded manner
        /// when no other threads are in the middle of accessing this or other members that lazily initialize the box.
        /// </remarks>
        internal object ObjectIdForDebugger
        {
            get
            {
                if (m_task is null)
                {
                    m_task = AsyncTaskCache.s_valueTaskPoolingEnabled ? (object)
                        CreateWeaklyTypedStateMachineBox() :
                        AsyncTaskMethodBuilder<TResult>.CreateWeaklyTypedStateMachineBox();
                }

                return m_task;
            }
        }

        /// <summary>The base type for all value task box reusable box objects, regardless of state machine type.</summary>
        internal abstract class StateMachineBox :
            IValueTaskSource<TResult>, IValueTaskSource
        {
            /// <summary>A delegate to the MoveNext method.</summary>
            protected Action? _moveNextAction;
            /// <summary>Captured ExecutionContext with which to invoke MoveNext.</summary>
            public ExecutionContext? Context;
            /// <summary>Implementation for IValueTaskSource interfaces.</summary>
            protected ManualResetValueTaskSourceCore<TResult> _valueTaskSource;

            /// <summary>Completes the box with a result.</summary>
            /// <param name="result">The result.</param>
            public void SetResult(TResult result) =>
                _valueTaskSource.SetResult(result);

            /// <summary>Completes the box with an error.</summary>
            /// <param name="error">The exception.</param>
            public void SetException(Exception error) =>
                _valueTaskSource.SetException(error);

            /// <summary>Gets the status of the box.</summary>
            public ValueTaskSourceStatus GetStatus(short token) => _valueTaskSource.GetStatus(token);

            /// <summary>Schedules the continuation action for this box.</summary>
            public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) =>
                _valueTaskSource.OnCompleted(continuation, state, token, flags);

            /// <summary>Gets the current version number of the box.</summary>
            public short Version => _valueTaskSource.Version;

            /// <summary>Implemented by derived type.</summary>
            TResult IValueTaskSource<TResult>.GetResult(short token) => throw NotImplemented.ByDesign;

            /// <summary>Implemented by derived type.</summary>
            void IValueTaskSource.GetResult(short token) => throw NotImplemented.ByDesign;
        }

        private sealed class SyncSuccessSentinelStateMachineBox : StateMachineBox
        {
            public SyncSuccessSentinelStateMachineBox() => SetResult(default!);
        }

        /// <summary>Provides a strongly-typed box object based on the specific state machine type in use.</summary>
        private sealed class StateMachineBox<TStateMachine> :
            StateMachineBox,
            IValueTaskSource<TResult>, IValueTaskSource, IAsyncStateMachineBox, IThreadPoolWorkItem
            where TStateMachine : IAsyncStateMachine
        {
            /// <summary>Delegate used to invoke on an ExecutionContext when passed an instance of this box type.</summary>
            private static readonly ContextCallback s_callback = ExecutionContextCallback;
            /// <summary>Lock used to protected the shared cache of boxes.</summary>
            /// <remarks>The code that uses this assumes a runtime without thread aborts.</remarks>
            private static int s_cacheLock;
            /// <summary>Singly-linked list cache of boxes.</summary>
            private static StateMachineBox<TStateMachine>? s_cache;
            /// <summary>The number of items stored in <see cref="s_cache"/>.</summary>
            private static int s_cacheSize;

            // TODO:
            // AsyncTaskMethodBuilder logs about the state machine box lifecycle; AsyncValueTaskMethodBuilder currently
            // does not when it employs these pooled boxes.  That logging is based on Task IDs, which we lack here.
            // We could use the box's Version, but that is very likely to conflict with the IDs of other tasks in the system.
            // For now, we don't log, but should we choose to we'll probably want to store an int ID on the state machine box,
            // and initialize it an ID from Task's generator.

            /// <summary>If this box is stored in the cache, the next box in the cache.</summary>
            private StateMachineBox<TStateMachine>? _next;
            /// <summary>The state machine itself.</summary>
            public TStateMachine? StateMachine;

            /// <summary>Gets a box object to use for an operation.  This may be a reused, pooled object, or it may be new.</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // only one caller
            internal static StateMachineBox<TStateMachine> GetOrCreateBox()
            {
                // Try to acquire the lock to access the cache.  If there's any contention, don't use the cache.
                if (Interlocked.CompareExchange(ref s_cacheLock, 1, 0) == 0)
                {
                    // If there are any instances cached, take one from the cache stack and use it.
                    StateMachineBox<TStateMachine>? box = s_cache;
                    if (!(box is null))
                    {
                        s_cache = box._next;
                        box._next = null;
                        s_cacheSize--;
                        Debug.Assert(s_cacheSize >= 0, "Expected the cache size to be non-negative.");

                        // Release the lock and return the box.
                        Volatile.Write(ref s_cacheLock, 0);
                        return box;
                    }

                    // No objects were cached.  We'll just create a new instance.
                    Debug.Assert(s_cacheSize == 0, "Expected cache size to be 0.");

                    // Release the lock.
                    Volatile.Write(ref s_cacheLock, 0);
                }

                // Couldn't quickly get a cached instance, so create a new instance.
                return new StateMachineBox<TStateMachine>();
            }

            private void ReturnOrDropBox()
            {
                Debug.Assert(_next is null, "Expected box to not be part of cached list.");

                // Clear out the state machine and associated context to avoid keeping arbitrary state referenced by
                // lifted locals.  We want to do this regardless of whether we end up caching the box or not, in case
                // the caller keeps the box alive for an arbitrary period of time.
                StateMachine = default;
                Context = default;

                // Reset the MRVTSC.  We can either do this here, in which case we may be paying the (small) overhead
                // to reset the box even if we're going to drop it, or we could do it while holding the lock, in which
                // case we'll only reset it if necessary but causing the lock to be held for longer, thereby causing
                // more contention.  For now at least, we do it outside of the lock. (This must not be done after
                // the lock is released, since at that point the instance could already be in use elsewhere.)
                // We also want to increment the version number even if we're going to drop it, to maximize the chances
                // that incorrectly double-awaiting a ValueTask will produce an error.
                _valueTaskSource.Reset();

                // If reusing the object would result in potentially wrapping around its version number, just throw it away.
                // This provides a modicum of additional safety when ValueTasks are misused (helping to avoid the case where
                // a ValueTask is illegally re-awaited and happens to do so at exactly 2^16 uses later on this exact same instance),
                // at the expense of potentially incurring an additional allocation every 65K uses.
                if ((ushort)_valueTaskSource.Version == ushort.MaxValue)
                {
                    return;
                }

                // Try to acquire the cache lock.  If there's any contention, or if the cache is full, we just throw away the object.
                if (Interlocked.CompareExchange(ref s_cacheLock, 1, 0) == 0)
                {
                    if (s_cacheSize < AsyncTaskCache.s_valueTaskPoolingCacheSize)
                    {
                        // Push the box onto the cache stack for subsequent reuse.
                        _next = s_cache;
                        s_cache = this;
                        s_cacheSize++;
                        Debug.Assert(s_cacheSize > 0 && s_cacheSize <= AsyncTaskCache.s_valueTaskPoolingCacheSize, "Expected cache size to be within bounds.");
                    }

                    // Release the lock.
                    Volatile.Write(ref s_cacheLock, 0);
                }
            }

            /// <summary>
            /// Used to initialize s_callback above. We don't use a lambda for this on purpose: a lambda would
            /// introduce a new generic type behind the scenes that comes with a hefty size penalty in AOT builds.
            /// </summary>
            private static void ExecutionContextCallback(object? s)
            {
                // Only used privately to pass directly to EC.Run
                Debug.Assert(s is StateMachineBox<TStateMachine>);
                Unsafe.As<StateMachineBox<TStateMachine>>(s).StateMachine!.MoveNext();
            }

            /// <summary>A delegate to the <see cref="MoveNext()"/> method.</summary>
            public Action MoveNextAction => _moveNextAction ??= new Action(MoveNext);

            /// <summary>Invoked to run MoveNext when this instance is executed from the thread pool.</summary>
            void IThreadPoolWorkItem.Execute() => MoveNext();

            /// <summary>Calls MoveNext on <see cref="StateMachine"/></summary>
            public void MoveNext()
            {
                ExecutionContext? context = Context;

                if (context is null)
                {
                    Debug.Assert(!(StateMachine is null));
                    StateMachine.MoveNext();
                }
                else
                {
                    ExecutionContext.RunInternal(context, s_callback, this);
                }
            }

            /// <summary>Get the result of the operation.</summary>
            TResult IValueTaskSource<TResult>.GetResult(short token)
            {
                try
                {
                    return _valueTaskSource.GetResult(token);
                }
                finally
                {
                    // Reuse this instance if possible, otherwise clear and drop it.
                    ReturnOrDropBox();
                }
            }

            /// <summary>Get the result of the operation.</summary>
            void IValueTaskSource.GetResult(short token)
            {
                try
                {
                    _valueTaskSource.GetResult(token);
                }
                finally
                {
                    // Reuse this instance if possible, otherwise clear and drop it.
                    ReturnOrDropBox();
                }
            }

            /// <summary>Gets the state machine as a boxed object.  This should only be used for debugging purposes.</summary>
            IAsyncStateMachine IAsyncStateMachineBox.GetStateMachineObject() => StateMachine!; // likely boxes, only use for debugging
        }
    }
}
