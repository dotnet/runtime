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
        private sealed unsafe class ValueTaskContinuation : Continuation
        {
            internal object? Source;
            internal short Token;
            internal delegate*<object, Action<object?>, object?, short, ValueTaskSourceOnCompletedFlags, void> OnCompletedValueTaskSource;
            private delegate*<object, short, ref byte, void> _getResult;

            public ValueTaskContinuation()
            {
                ResumeInfo = (ResumeInfo*)Unsafe.AsPointer(in ValueTaskContinuationResume.ResumeInfo);
            }

            public void GetResult(ref byte returnValue)
            {
                Debug.Assert(Source != null);

                // Avoid retaining source. The call below may throw.
                object source = Source;
                Source = null;

                _getResult(source, Token, ref returnValue);
            }

            public void Initialize(object source, short token)
            {
                Source = source;
                Token = token;
                OnCompletedValueTaskSource = &OnCompleted;
                _getResult = &GetResult;
            }

            public void Initialize<T>(object source, short token)
            {
                Source = source;
                Token = token;
                OnCompletedValueTaskSource = &OnCompleted<T>;
                _getResult = &GetResult<T>;
            }

            private static void OnCompleted(object source, Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags)
            {
                Debug.Assert(source is IValueTaskSource);
                IValueTaskSource typedSource = Unsafe.As<object, IValueTaskSource>(ref source);
                typedSource.OnCompleted(continuation, state, token, flags);
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
                Debug.Assert(source is IValueTaskSource<T>);
                IValueTaskSource<T> typedSource = Unsafe.As<object, IValueTaskSource<T>>(ref source);
                typedSource.OnCompleted(continuation, state, token, flags);
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

                private static Continuation? ResumeValueTaskContinuation(Continuation cont, ref byte result)
                {
                    var vtsCont = (ValueTaskContinuation)cont;
                    vtsCont.Next = null;

                    Debug.Assert((vtsCont.Flags & ContinuationFlags.AllContinuationFlags) == 0);

                    t_runtimeAsyncAwaitState.CachedValueTaskContinuation = vtsCont;

                    vtsCont.GetResult(ref result);
                    return null;
                }
            }
        }
    }
}
