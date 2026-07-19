// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Runtime.CompilerServices
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static partial class AsyncHelpers
    {
#if CORECLR || NATIVEAOT
        // "BypassReadyToRun" is until AOT/R2R typesystem has support for MethodImpl.Async
        // Must be NoInlining because we use AsyncSuspend to manufacture an explicit suspension point.
        // It will not capture/restore any local state that is live across it.

        /// <summary>
        /// Awaits the specified awaiter and returns when the awaiter has completed.
        /// </summary>
        /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
        /// <param name="awaiter">The awaiter to await.</param>
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        [StackTraceHidden]
        public static unsafe void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();
            state.StackState->Notifier = awaiter;
            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
        }

        // Must be NoInlining because we use AsyncSuspend to manufacture an explicit suspension point.
        // It will not capture/restore any local state that is live across it.

        /// <summary>
        /// Awaits the specified awaiter without capturing the execution context and returns when the awaiter has completed.
        /// </summary>
        /// <typeparam name="TAwaiter">The awaiter type.</typeparam>
        /// <param name="awaiter">The awaiter to await.</param>
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        [StackTraceHidden]
        public static unsafe void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation ??= new Continuation();

            // We optimize specially for "await Task.Yield()" -- in the same
            // way that YieldAwaiter implements IStateMachineBoxAwareAwaiter
            // for async1. This avoids allocating internal thread pool objects
            // for this case.
            if (typeof(TAwaiter) == typeof(YieldAwaitable.YieldAwaiter))
            {
                state.StackState->CriticalNotifier = RuntimeAsyncYielder.Instance;
            }
            else
            {
                state.StackState->CriticalNotifier = awaiter;
            }

            state.CaptureContexts();
            AsyncSuspend(sentinelContinuation);
        }

        /// <summary>
        /// Awaits the specified <see cref="Task{T}"/> and returns its result, throwing any exception produced by the task.
        /// </summary>
        /// <typeparam name="T">The result type produced by the task.</typeparam>
        /// <param name="task">The task to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static T Await<T>(Task<T> task)
        {
            if (!task.IsCompleted)
            {
                TailAwait();
                return Suspend(task, ConfigureAwaitOptions.ContinueOnCapturedContext);
            }

            TaskAwaiter.ValidateEnd(task);
            return task.ResultOnSuccess;
        }

        /// <summary>
        /// Awaits the specified <see cref="Task"/> and throws any exception produced by the operation.
        /// </summary>
        /// <param name="task">The task to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static void Await(Task task)
        {
            if (!task.IsCompleted)
            {
                TailAwait();
                Suspend(task, ConfigureAwaitOptions.ContinueOnCapturedContext);
                return;
            }

            TaskAwaiter.ValidateEnd(task);
        }

        /// <summary>
        /// Awaits the specified <see cref="ValueTask{T}"/> and returns its result, throwing any exception produced by the operation.
        /// </summary>
        /// <typeparam name="T">The result type produced by the value task.</typeparam>
        /// <param name="task">The value task to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static T Await<T>(ValueTask<T> task)
        {
            object? obj = task._obj;
            if (obj == null)
            {
                return task._result!;
            }

            if (obj is Task<T> t)
            {
                if (t.IsCompleted)
                {
                    TaskAwaiter.ValidateEnd(t);
                    return t.ResultOnSuccess;
                }

                TailAwait();
                return Suspend(t, ConfigureAwaitOptions.ContinueOnCapturedContext);
            }

            Debug.Assert(obj is IValueTaskSource<T>);
            IValueTaskSource<T> vts = Unsafe.As<object, IValueTaskSource<T>>(ref obj);
            if (vts.GetStatus(task._token) == ValueTaskSourceStatus.Pending)
            {
                TailAwait();
                return Suspend(vts, task._token, true);
            }

            return vts.GetResult(task._token);
        }

        /// <summary>
        /// Awaits the specified <see cref="ValueTask"/> and throws any exception produced by the operation.
        /// </summary>
        /// <param name="task">The value task to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static void Await(ValueTask task)
        {
            object? obj = task._obj;
            if (obj == null)
            {
                return;
            }

            if (obj is Task t)
            {
                if (t.IsCompleted)
                {
                    TaskAwaiter.ValidateEnd(t);
                    return;
                }

                TailAwait();
                Suspend(t, ConfigureAwaitOptions.ContinueOnCapturedContext);
                return;
            }

            Debug.Assert(obj is IValueTaskSource);
            IValueTaskSource vts = Unsafe.As<object, IValueTaskSource>(ref obj);
            if (vts.GetStatus(task._token) == ValueTaskSourceStatus.Pending)
            {
                TailAwait();
                Suspend(vts, task._token, true);
                return;
            }

            vts.GetResult(task._token);
        }

        /// <summary>
        /// Awaits the specified configured task awaitable without capturing the execution context and throws any exception produced by the operation.
        /// </summary>
        /// <param name="configuredAwaitable">The configured awaitable to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static void Await(ConfiguredTaskAwaitable configuredAwaitable)
        {
            ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = configuredAwaitable.GetAwaiter();
            if ((awaiter.m_options & ConfigureAwaitOptions.SuppressThrowing) != 0)
            {
                TailAwait();
                AwaitTaskWithRareOptions(awaiter);
                return;
            }

            if (!awaiter.IsCompleted)
            {
                TailAwait();
                Suspend(awaiter.m_task, awaiter.m_options);
                return;
            }

            awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async | MethodImplOptions.NoInlining)]
        [StackTraceHidden]
        private static void AwaitTaskWithRareOptions(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter)
        {
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            awaiter.GetResult();
        }

        /// <summary>
        /// Awaits the specified configured value task awaitable without capturing the execution context and throws any exception produced by the operation.
        /// </summary>
        /// <param name="configuredAwaitable">The configured value task awaitable to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static void Await(ConfiguredValueTaskAwaitable configuredAwaitable)
        {
            ValueTask task = configuredAwaitable._value;
            object? obj = task._obj;
            if (obj == null)
            {
                return;
            }

            if (obj is Task t)
            {
                if (t.IsCompleted)
                {
                    TaskAwaiter.ValidateEnd(t);
                    return;
                }

                TailAwait();
                Suspend(t, task._continueOnCapturedContext ? ConfigureAwaitOptions.ContinueOnCapturedContext : ConfigureAwaitOptions.None);
                return;
            }

            Debug.Assert(obj is IValueTaskSource);
            IValueTaskSource vts = Unsafe.As<object, IValueTaskSource>(ref obj);
            if (vts.GetStatus(task._token) == ValueTaskSourceStatus.Pending)
            {
                TailAwait();
                Suspend(vts, task._token, task._continueOnCapturedContext);
                return;
            }

            vts.GetResult(task._token);
        }

        /// <summary>
        /// Awaits the specified configured task awaitable and returns its result, throwing any exception produced by the operation.
        /// </summary>
        /// <typeparam name="T">The result type produced by the awaitable.</typeparam>
        /// <param name="configuredAwaitable">The configured awaitable to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static T Await<T>(ConfiguredTaskAwaitable<T> configuredAwaitable)
        {
            ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter awaiter = configuredAwaitable.GetAwaiter();
            // For the T version of ConfiguredTaskAwaiter we do not need to
            // support ConfigureAwaitOptions.SuppressThrowing, but we do need
            // to support ForceYielding. That one gets folded into the
            // awaiter's IsCompleted here.
            if (!awaiter.IsCompleted)
            {
                TailAwait();
                return Suspend(awaiter.m_task, awaiter.m_options);
            }

            return awaiter.GetResult();
        }

        /// <summary>
        /// Awaits the specified configured value task awaitable and returns its result, throwing any exception produced by the operation.
        /// </summary>
        /// <typeparam name="T">The result type produced by the awaitable.</typeparam>
        /// <param name="configuredAwaitable">The configured awaitable to await.</param>
        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [StackTraceHidden]
        public static T Await<T>(ConfiguredValueTaskAwaitable<T> configuredAwaitable)
        {
            ValueTask<T> task = configuredAwaitable._value;
            object? obj = task._obj;
            if (obj == null)
            {
                return task._result!;
            }

            if (obj is Task<T> t)
            {
                if (t.IsCompleted)
                {
                    TaskAwaiter.ValidateEnd(t);
                    return t.ResultOnSuccess;
                }

                TailAwait();
                return Suspend(t, task._continueOnCapturedContext ? ConfigureAwaitOptions.ContinueOnCapturedContext : ConfigureAwaitOptions.None);
            }

            Debug.Assert(obj is IValueTaskSource<T>);
            IValueTaskSource<T> vts = Unsafe.As<object, IValueTaskSource<T>>(ref obj);
            if (vts.GetStatus(task._token) == ValueTaskSourceStatus.Pending)
            {
                TailAwait();
                return Suspend(vts, task._token, task._continueOnCapturedContext);
            }

            return vts.GetResult(task._token);
        }
#else
        public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static void Await(System.Threading.Tasks.Task task) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static T Await<T>(System.Threading.Tasks.Task<T> task) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static void Await(System.Threading.Tasks.ValueTask task) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static T Await<T>(System.Threading.Tasks.ValueTask<T> task) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static void Await(System.Runtime.CompilerServices.ConfiguredTaskAwaitable configuredAwaitable) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static void Await(System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable configuredAwaitable) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static T Await<T>(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<T> configuredAwaitable) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
        public static T Await<T>(System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<T> configuredAwaitable) { throw new PlatformNotSupportedException("Runtime Async is not supported on this platform."); }
#endif
    }
}
