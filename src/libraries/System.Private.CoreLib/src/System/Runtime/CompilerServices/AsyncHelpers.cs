// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [System.Diagnostics.CodeAnalysis.ExperimentalAttribute("SYSLIB5007", UrlFormat = "https://aka.ms/dotnet-warnings/{0}")]
    public static partial class AsyncHelpers
    {
#if CORECLR
        // "BypassReadyToRun" is until AOT/R2R typesystem has support for MethodImpl.Async
        // Must be NoInlining because we use AsyncSuspend to manufacture an explicit suspension point.
        // It will not capture/restore any local state that is live across it.
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation;
            if (sentinelContinuation == null)
                state.SentinelContinuation = sentinelContinuation = new Continuation();

            state.Notifier = awaiter;
            AsyncSuspend(sentinelContinuation);
        }

        // Must be NoInlining because we use AsyncSuspend to manufacture an explicit suspension point.
        // It will not capture/restore any local state that is live across it.
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion
        {
            ref RuntimeAsyncAwaitState state = ref t_runtimeAsyncAwaitState;
            Continuation? sentinelContinuation = state.SentinelContinuation;
            if (sentinelContinuation == null)
                state.SentinelContinuation = sentinelContinuation = new Continuation();

            state.CriticalNotifier = awaiter;
            AsyncSuspend(sentinelContinuation);
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static T Await<T>(Task<T> task)
        {
            TaskAwaiter<T> awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            return awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static void Await(Task task)
        {
            TaskAwaiter awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static T Await<T>(ValueTask<T> task)
        {
            ValueTaskAwaiter<T> awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            return awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static void Await(ValueTask task)
        {
            ValueTaskAwaiter awaiter = task.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static void Await(ConfiguredTaskAwaitable configuredAwaitable)
        {
            ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter = configuredAwaitable.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static void Await(ConfiguredValueTaskAwaitable configuredAwaitable)
        {
            ConfiguredValueTaskAwaitable.ConfiguredValueTaskAwaiter awaiter = configuredAwaitable.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static T Await<T>(ConfiguredTaskAwaitable<T> configuredAwaitable)
        {
            ConfiguredTaskAwaitable<T>.ConfiguredTaskAwaiter awaiter = configuredAwaitable.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            return awaiter.GetResult();
        }

        [Intrinsic]
        [BypassReadyToRun]
        [MethodImpl(MethodImplOptions.Async)]
        [RequiresPreviewFeatures]
        public static T Await<T>(ConfiguredValueTaskAwaitable<T> configuredAwaitable)
        {
            ConfiguredValueTaskAwaitable<T>.ConfiguredValueTaskAwaiter awaiter = configuredAwaitable.GetAwaiter();
            if (!awaiter.IsCompleted)
            {
                UnsafeAwaitAwaiter(awaiter);
            }

            return awaiter.GetResult();
        }
#else
        [RequiresPreviewFeatures]
        public static void UnsafeAwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : ICriticalNotifyCompletion { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static void AwaitAwaiter<TAwaiter>(TAwaiter awaiter) where TAwaiter : INotifyCompletion { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static void Await(System.Threading.Tasks.Task task) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static T Await<T>(System.Threading.Tasks.Task<T> task) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static void Await(System.Threading.Tasks.ValueTask task) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static T Await<T>(System.Threading.Tasks.ValueTask<T> task) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static void Await(System.Runtime.CompilerServices.ConfiguredTaskAwaitable configuredAwaitable) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static void Await(System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable configuredAwaitable) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static T Await<T>(System.Runtime.CompilerServices.ConfiguredTaskAwaitable<T> configuredAwaitable) { throw new NotImplementedException(); }
        [RequiresPreviewFeatures]
        public static T Await<T>(System.Runtime.CompilerServices.ConfiguredValueTaskAwaitable<T> configuredAwaitable) { throw new NotImplementedException(); }
#endif
    }
}
