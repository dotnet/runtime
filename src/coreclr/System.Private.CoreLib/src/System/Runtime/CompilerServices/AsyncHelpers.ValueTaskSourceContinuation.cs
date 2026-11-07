// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        private sealed unsafe class ValueTaskSourceContinuation : Continuation
        {
            internal object? Source;
            private delegate*<object, short, ref byte, void> _getResult;
            private short _token;

            // Whether the source is being awaited, i.e. the dispatcher has yet to fetch its result.
            internal bool IsPending;

            // This is never linked into a continuation chain or resumed. Instead, the suspended
            // RuntimeAsyncTask holds it while awaiting the source, and the dispatcher calls GetResult
            // to store the result directly into the awaiting continuation. The RuntimeAsyncTask keeps
            // holding it after that, so that awaiting the same source again (as is common in loops)
            // needn't store anything that requires a write barrier.

            [StackTraceHidden]
            public void GetResult(ref byte returnValue)
            {
                Debug.Assert(IsPending && Source != null);
                IsPending = false;
                _getResult(Source, _token, ref returnValue);
            }

            public void Initialize(object source, short token, delegate*<object, short, ref byte, void> getResult)
            {
                Debug.Assert(!IsPending);
                if (Source != source)
                {
                    Source = source;
                }

                _token = token;
                _getResult = getResult;
                IsPending = true;
            }

            // Registers the continuation with the source. Like any notifier, a source that throws from OnCompleted
            // has its exception propagated to the thread pool; returns false in that case.
            internal static bool OnCompleted(object source, Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                Debug.Assert(source is IValueTaskSource);
                IValueTaskSource typedSource = Unsafe.As<object, IValueTaskSource>(ref source);
                try
                {
                    typedSource.OnCompleted(continuation, state, token, flags);
                    return true;
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                    return false;
                }
            }

            [StackTraceHidden]
            internal static void GetResult(object source, short token, ref byte _)
            {
                Debug.Assert(source is IValueTaskSource);
                IValueTaskSource typedSource = Unsafe.As<object, IValueTaskSource>(ref source);
                typedSource.GetResult(token);
            }

            internal static bool OnCompleted<T>(object source, Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                Debug.Assert(source is IValueTaskSource<T>);
                IValueTaskSource<T> typedSource = Unsafe.As<object, IValueTaskSource<T>>(ref source);
                try
                {
                    typedSource.OnCompleted(continuation, state, token, flags);
                    return true;
                }
                catch (Exception ex)
                {
                    Task.ThrowAsync(ex, targetContext: null);
                    return false;
                }
            }

            [StackTraceHidden]
            internal static void GetResult<T>(object source, short token, ref byte result)
            {
                Debug.Assert(source is IValueTaskSource<T>);
                IValueTaskSource<T> typedSource = Unsafe.As<object, IValueTaskSource<T>>(ref source);
                T value = typedSource.GetResult(token);

                // The awaiting continuation has no result storage if it ignores the result.
                if (!Unsafe.IsNullRef(ref result))
                {
                    Unsafe.As<byte, T>(ref result) = value;
                }
            }
        }
    }
}
