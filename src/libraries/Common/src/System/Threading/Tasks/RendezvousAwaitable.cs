// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CA1852 // some projects include types derived from this

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace System.Threading.Tasks
{
    /// <summary>Provides a reusable object that can be awaited by a consumer and manually completed by a producer.</summary>
    /// <typeparam name="TResult">The type of data being passed from producer to consumer.</typeparam>
    internal class RendezvousAwaitable<TResult> : ICriticalNotifyCompletion
    {
        /// <summary>Sentinel object indicating that the operation has completed prior to OnCompleted being called.</summary>
        private static readonly Action s_completionSentinel = static () => Debug.Fail("Completion sentinel should never be invoked");

        /// <summary>
        /// The continuation to invoke when the operation completes, or <see cref="s_completionSentinel"/> if the operation
        /// has completed before OnCompleted is called.
        /// </summary>
        private Action? _continuation;
        /// <summary>The exception representing the failed async operation, if it failed.</summary>
        private ExceptionDispatchInfo? _error;
        /// <summary>The result of the async operation, if it succeeded.</summary>
        private TResult? _result;
#if DEBUG
        private bool _resultSet;
#endif

        /// <summary>true if the producer should invoke the continuation asynchronously; otherwise, false.</summary>
        public bool RunContinuationsAsynchronously { get; set; } = true;

        /// <summary>Gets this instance as an awaiter.</summary>
        public RendezvousAwaitable<TResult> GetAwaiter() => this;

        /// <summary>Whether the operation has already completed.</summary>
        public bool IsCompleted
        {
            get
            {
                Action? c = Volatile.Read(ref _continuation);
                Debug.Assert(c == null || c == s_completionSentinel);
                return c != null;
            }
        }

        public TResult GetResult()
        {
            AssertResultConsistency(expectedCompleted: true);

            // Clear out the continuation to prepare for another use
            Debug.Assert(_continuation != null);
            _continuation = null;

            // Propagate any error if there is one, clearing it out first to prepare for reuse.
            // We don't need to clear a result, as result and error are mutually exclusive.
            ExceptionDispatchInfo? error = _error;
            if (error != null)
            {
                _error = null;
                error.Throw();
            }

            // The operation completed successfully.  Clear and return the result.
            TResult result = _result!;
            _result = default(TResult);
#if DEBUG
            _resultSet = false;
#endif
            return result;
        }

        /// <summary>Set the result of the operation.</summary>
        public void SetResult(TResult result)
        {
            AssertResultConsistency(expectedCompleted: false);
            _result = result;
#if DEBUG
            _resultSet = true;
#endif
            NotifyAwaiter();
        }

        /// <summary>Set that the operation was canceled.</summary>
        public void SetCanceled(CancellationToken token = default(CancellationToken))
        {
            SetException(token.IsCancellationRequested ? new OperationCanceledException(token) : new OperationCanceledException());
        }

        /// <summary>Set the failure for the operation.</summary>
        public void SetException(Exception exception)
        {
            Debug.Assert(exception != null);
            AssertResultConsistency(expectedCompleted: false);

            _error = ExceptionDispatchInfo.Capture(exception);
            NotifyAwaiter();
        }

        /// <summary>Alerts any awaiter that the operation has completed.</summary>
        private void NotifyAwaiter()
        {
            Action? c = _continuation ?? Interlocked.CompareExchange(ref _continuation, s_completionSentinel, null);
            if (c != null)
            {
                Debug.Assert(c != s_completionSentinel);

                if (RunContinuationsAsynchronously)
                {
                    Task.Run(c);
                }
                else
                {
                    c();
                }
            }
        }

        /// <summary>Register the continuation to invoke when the operation completes.</summary>
        public void OnCompleted(Action continuation)
        {
            Debug.Assert(continuation != null);

            Action? c = _continuation ?? Interlocked.CompareExchange(ref _continuation, continuation, null);
            if (c != null)
            {
                Debug.Assert(c == s_completionSentinel);
                Task.Run(continuation);
            }
        }

        /// <summary>Register the continuation to invoke when the operation completes.</summary>
        public void UnsafeOnCompleted(Action continuation) => OnCompleted(continuation);

#pragma warning disable CA1822
        [Conditional("DEBUG")]
        private void AssertResultConsistency(bool expectedCompleted)
        {
#if DEBUG
            if (expectedCompleted)
            {
                Debug.Assert(_resultSet ^ (_error != null));
            }
            else
            {
                Debug.Assert(!_resultSet && _error == null);
            }
#endif
        }
#pragma warning restore CA1822
    }
}
