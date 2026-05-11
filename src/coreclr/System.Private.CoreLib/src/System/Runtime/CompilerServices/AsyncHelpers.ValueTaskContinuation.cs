// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Runtime.CompilerServices
{
    public static partial class AsyncHelpers
    {
        private sealed unsafe class ValueTaskContinuation : Continuation
        {
            // Currently all continuations are expected to capture and restore
            // ExecutionContext, even though we do not actually need it here.
            public ExecutionContext? ExecutionContext;
            private object? _source;
            private short _token;
            private delegate* managed<object, Action<object?>, object?, short, ValueTaskSourceOnCompletedFlags, void> _onCompleted;
            private delegate* managed<object, short, ref byte, void> _getResult;

            public ValueTaskContinuation()
            {
                ResumeInfo = (ResumeInfo*)Unsafe.AsPointer(in ValueTaskContinuationResume.ResumeInfo);

                EncodeFieldOffsetInFlags(
                    ref Unsafe.As<ExecutionContext?, byte>(ref ExecutionContext),
                    ContinuationFlags.ExecutionContextIndexFirstBit,
                    ContinuationFlags.ExecutionContextIndexNumBits);
            }

            public void OnCompleted(Action<object?> continuation, object? state, ValueTaskSourceOnCompletedFlags flags)
            {
                Debug.Assert(_source != null);
                _onCompleted(_source, continuation, state, _token, flags);
            }

            public void GetResult(ref byte returnValue)
            {
                Debug.Assert(_source != null);

                // Avoid retaining source. The call below may throw.
                object source = _source;
                _source = null;

                _getResult(source, _token, ref returnValue);
            }

            public void Initialize(object source, short token)
            {
                _source = source;
                _token = token;
                _onCompleted = &OnCompleted;
                _getResult = &GetResult;
            }

            public void Initialize<T>(object source, short token)
            {
                _source = source;
                _token = token;
                _onCompleted = &OnCompleted<T>;
                _getResult = &GetResult<T>;
            }

            private static void OnCompleted(object source, Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                if (source is Task t)
                {
                    Debug.Assert(state is ITaskCompletionAction);
                    if (!t.TryAddCompletionAction(Unsafe.As<object, ITaskCompletionAction>(ref state)))
                    {
                        ThreadPool.UnsafeQueueUserWorkItemInternal(state, preferLocal: true);
                    }
                }
                else
                {
                    Debug.Assert(source is IValueTaskSource);
                    IValueTaskSource typedSource = Unsafe.As<object, IValueTaskSource>(ref source);
                    typedSource.OnCompleted(continuation, state, token, flags);
                }
            }

            private static void GetResult(object source, short token, ref byte result)
            {
                if (source is Task t)
                {
                    TaskAwaiter.ValidateEnd(t);
                }
                else
                {
                    Debug.Assert(source is IValueTaskSource);
                    IValueTaskSource typedSource = Unsafe.As<object, IValueTaskSource>(ref source);
                    typedSource.GetResult(token);
                }
            }

            private static void OnCompleted<T>(object source, Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                if (source is Task t)
                {
                    Debug.Assert(state is ITaskCompletionAction);
                    if (!t.TryAddCompletionAction(Unsafe.As<object, ITaskCompletionAction>(ref state)))
                    {
                        ThreadPool.UnsafeQueueUserWorkItemInternal(state, preferLocal: true);
                    }
                }
                else
                {
                    Debug.Assert(source is IValueTaskSource<T>);
                    IValueTaskSource<T> typedSource = Unsafe.As<object, IValueTaskSource<T>>(ref source);
                    typedSource.OnCompleted(continuation, state, token, flags);
                }
            }

            private static void GetResult<T>(object source, short token, ref byte result)
            {
                if (source is Task<T> t)
                {
                    TaskAwaiter.ValidateEnd(t);
                    Unsafe.As<byte, T>(ref result) = t.ResultOnSuccess;
                }
                else
                {
                    Debug.Assert(source is IValueTaskSource<T>);
                    IValueTaskSource<T> typedSource = Unsafe.As<object, IValueTaskSource<T>>(ref source);
                    Unsafe.As<byte, T>(ref result) = typedSource.GetResult(token);
                }
            }

            private static class ValueTaskContinuationResume
            {
                [FixedAddressValueType]
                public static readonly ResumeInfo ResumeInfo = new ResumeInfo
                {
                    DiagnosticIP = null,
                    Resume = &ResumeValueTaskContinuation,
                };

                public static Continuation? ResumeValueTaskContinuation(Continuation cont, ref byte result)
                {
                    var vtsCont = (ValueTaskContinuation)cont;
                    vtsCont.Next = null;
                    vtsCont.ExecutionContext = null;
                    t_runtimeAsyncAwaitState.CachedValueTaskContinuation = vtsCont;

                    vtsCont.GetResult(ref result);
                    return null;
                }
            }
        }
    }
}
