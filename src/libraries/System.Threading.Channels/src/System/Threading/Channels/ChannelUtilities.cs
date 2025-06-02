// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Provides internal helper methods for implementing channels.</summary>
    internal static partial class ChannelUtilities
    {
        /// <summary>Sentinel object used to indicate being done writing.</summary>
        internal static readonly Exception s_doneWritingSentinel = new Exception(nameof(s_doneWritingSentinel));
        /// <summary>A cached task with a Boolean true result.</summary>
        internal static readonly Task<bool> s_trueTask = Task.FromResult(result: true);
        /// <summary>A cached task with a Boolean false result.</summary>
        internal static readonly Task<bool> s_falseTask = Task.FromResult(result: false);
        /// <summary>A cached task that never completes.</summary>
        internal static readonly Task s_neverCompletingTask = new TaskCompletionSource<bool>().Task;

        /// <summary>Completes the specified TaskCompletionSource.</summary>
        /// <param name="tcs">The source to complete.</param>
        /// <param name="error">
        /// The optional exception with which to complete.
        /// If this is null or the DoneWritingSentinel, the source will be completed successfully.
        /// If this is an OperationCanceledException, it'll be completed with the exception's token.
        /// Otherwise, it'll be completed as faulted with the exception.
        /// </param>
        internal static void Complete(TaskCompletionSource tcs, Exception? error = null)
        {
            if (error is OperationCanceledException oce)
            {
                tcs.TrySetCanceled(oce.CancellationToken);
            }
            else if (error is not null && error != s_doneWritingSentinel)
            {
                if (tcs.TrySetException(error))
                {
                    // Suppress unobserved exceptions from Completion tasks, as the exceptions will generally
                    // have been surfaced elsewhere (which may end up making a consumer not consume the completion
                    // task), and even if they weren't, they're created by a producer who will have "seen" them (in
                    // contrast to them being created by some method call failing as part of user code).
                    _ = tcs.Task.Exception;
                }
            }
            else
            {
                tcs.TrySetResult();
            }
        }

        /// <summary>Gets a value task representing an error.</summary>
        /// <typeparam name="T">Specifies the type of the value that would have been returned.</typeparam>
        /// <param name="error">The error.  This may be <see cref="s_doneWritingSentinel"/>.</param>
        /// <returns>The failed task.</returns>
        internal static ValueTask<T> GetInvalidCompletionValueTask<T>(Exception error)
        {
            Debug.Assert(error is not null);

            Task<T> t =
                error == s_doneWritingSentinel ? Task.FromException<T>(CreateInvalidCompletionException()) :
                error is OperationCanceledException oce ? Task.FromCanceled<T>(oce.CancellationToken.IsCancellationRequested ? oce.CancellationToken : new CancellationToken(true)) :
                Task.FromException<T>(CreateInvalidCompletionException(error));

            return new ValueTask<T>(t);
        }

        /// <summary>Dequeues from <paramref name="head"/> until an element is dequeued that can have completion reserved.</summary>
        /// <param name="head">The head of the list, with items dequeued up through the returned element, or entirely if <see langword="null"/> is returned.</param>
        /// <returns>The operation on which completion has been reserved, or null if none can be found.</returns>
        internal static TAsyncOp? TryDequeueAndReserveCompletionIfCancelable<TAsyncOp>(ref TAsyncOp? head)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            while (ChannelUtilities.TryDequeue(ref head, out var op))
            {
                if (op.TryReserveCompletionIfCancelable())
                {
                    return op;
                }
            }

            return null;
        }

        /// <summary>Dequeues an operation from the circular doubly-linked list referenced by <paramref name="head"/>.</summary>
        /// <param name="head">The head of the list.</param>
        /// <param name="op">The dequeued operation.</param>
        /// <returns>true if an operation could be dequeued; otherwise, false.</returns>
        internal static bool TryDequeue<TAsyncOp>(ref TAsyncOp? head, [NotNullWhen(true)] out TAsyncOp? op)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            op = head;

            if (head is null)
            {
                return false;
            }

            Debug.Assert(head.Previous is not null);
            Debug.Assert(head.Next is not null);

            if (head.Next == head)
            {
                head = null;
            }
            else
            {
                TAsyncOp last = head.Previous;

                head = head.Next;
                head.Previous = last;
                last.Next = head;
            }

            Debug.Assert(op is not null);
            op.Next = op.Previous = null;
            return true;
        }

        /// <summary>Enqueues an operation onto the circular doubly-linked list referenced by <paramref name="head"/>.</summary>
        /// <param name="head">The head of the list.</param>
        /// <param name="op">The operation to enqueue.</param>
        internal static void Enqueue<TAsyncOp>(ref TAsyncOp? head, TAsyncOp op)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            Debug.Assert(op.Next is null && op.Previous is null);

            if (head is null)
            {
                head = op.Next = op.Previous = op;
            }
            else
            {
                TAsyncOp last = head.Previous!;
                Debug.Assert(last is not null);

                op.Next = head;
                op.Previous = last;
                last.Next = op;
                head.Previous = op;
            }
        }

        /// <summary>Removes the specified operation from the circular doubly-linked list referenced by <paramref name="head"/>.</summary>
        /// <param name="head">The head of the list.</param>
        /// <param name="op">The operation to remove.</param>
        internal static void Remove<TAsyncOp>(ref TAsyncOp? head, TAsyncOp op)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            Debug.Assert(op is not null);
            Debug.Assert(op.Next is null == op.Previous is null);

            // If the operation is known to not be in the list referenced by head, avoid further manipulating the instance.
            if (head is null || op.Next is null)
            {
                return;
            }

            Debug.Assert(op.Previous is not null);

            if (op.Next == op)
            {
                Debug.Assert(op.Previous == op);
                Debug.Assert(head == op);
                head = null;
            }
            else
            {
                op.Previous.Next = op.Next;
                op.Next.Previous = op.Previous;

                if (head == op)
                {
                    head = op.Next;
                }
            }

            op.Next = op.Previous = null;
        }

        /// <summary>Iterates through the linked list, successfully completing or failing each operation based on whether <paramref name="error"/> is <see langword="null"/>.</summary>
        /// <param name="head">The head of the queue of operations to complete.</param>
        /// <param name="result">The result with which to complete each operations.</param>
        /// <param name="error">The error with which to complete each operations.</param>
        internal static void SetOrFailOperations<TAsyncOp, T>(TAsyncOp? head, T result, Exception? error = null)
            where TAsyncOp : AsyncOperation<TAsyncOp, T>
        {
            if (error is not null)
            {
                FailOperations(head, error);
            }
            else
            {
                SetOperations(ref head, result);
            }
        }

        /// <summary>Iterates through the linked list, successfully completing each operation.</summary>
        /// <param name="head">The head of the queue of operations to complete.</param>
        /// <param name="result">The result with which to complete each operations.</param>
        internal static void SetOperations<TAsyncOp, TResult>(ref TAsyncOp? head, TResult result)
            where TAsyncOp : AsyncOperation<TAsyncOp, TResult>
        {
            TAsyncOp? current = head;
            if (current is not null)
            {
                do
                {
                    Debug.Assert(current is not null);

                    TAsyncOp? next = current.Next;
                    Debug.Assert(next is not null);

                    current.Next = current.Previous = null;

                    current.TrySetResult(result);

                    current = next;
                }
                while (current != head);

                head = null;
            }
        }

        /// <summary>Iterates through the linked list, successfully completing each operation that should have already had completion reserved.</summary>
        /// <param name="head">The head of the queue of operations to complete.</param>
        /// <param name="result">The result with which to complete each operations.</param>
        internal static void DangerousSetOperations<TAsyncOp, TResult>(TAsyncOp? head, TResult result)
            where TAsyncOp : AsyncOperation<TAsyncOp, TResult>
        {
            TAsyncOp? current = head;
            if (current is not null)
            {
                do
                {
                    Debug.Assert(current is not null);

                    TAsyncOp? next = current.Next;
                    Debug.Assert(next is not null);

                    current.Next = current.Previous = null;

                    current.DangerousSetResult(result);

                    current = next;
                }
                while (current != head);
            }
        }

        /// <summary>Iterates through the linked list, reserving completion for every element.</summary>
        /// <param name="head">The head of the queue of operations to complete.</param>
        /// <returns>A linked list of all successfully reserved operations. All other operations are ignored and dropped.</returns>
        internal static TAsyncOp? TryReserveCompletionIfCancelable<TAsyncOp>(ref TAsyncOp? head)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            TAsyncOp? reserved = null;

            TAsyncOp? current = head;
            if (current is not null)
            {
                do
                {
                    Debug.Assert(current is not null);

                    TAsyncOp? next = current.Next;
                    Debug.Assert(next is not null);

                    current.Next = current.Previous = null;

                    if (current.TryReserveCompletionIfCancelable())
                    {
                        Enqueue(ref reserved, current);
                    }

                    current = next;
                }
                while (current != head);

                head = null;
            }

            return reserved;
        }

        /// <summary>Iterates through the linked list, failing each operation.</summary>
        /// <param name="head">The head of the queue of operations to complete.</param>
        /// <param name="error">The error with which to complete each operations.</param>
        internal static void FailOperations<TAsyncOp>(TAsyncOp? head, Exception error)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            Debug.Assert(error is not null);

            TAsyncOp? current = head;
            if (current is not null)
            {
                do
                {
                    Debug.Assert(current is not null);

                    TAsyncOp? next = current.Next;
                    Debug.Assert(next is not null);

                    current.Next = current.Previous = null;

                    current.TrySetException(error);

                    current = next;
                }
                while (current != head);
            }
        }

        /// <summary>Asserts that all operations in the list pass the specified condition.</summary>
        /// <param name="head">The head of the queue of operations to analyze.</param>
        /// <param name="condition">The condition with which to evaluate each operation.</param>
        /// <param name="message">The assert message to use in the case of failure.</param>
        [Conditional("DEBUG")]
        internal static void AssertAll<TAsyncOp>(TAsyncOp? head, Func<TAsyncOp, bool> condition, string message)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            TAsyncOp? current = head;
            if (current is not null)
            {
                do
                {
                    Debug.Assert(current is not null);
                    Debug.Assert(condition(current), message);
                    current = current.Next;
                }
                while (current != head);
            }
        }

        /// <summary>Counts the number of operations in the list.</summary>
        /// <param name="head">The head of the queue of operations to count.</param>
        internal static long CountOperations<TAsyncOp>(TAsyncOp? head)
            where TAsyncOp : AsyncOperation<TAsyncOp>
        {
            TAsyncOp? current = head;
            long count = 0;

            if (current is not null)
            {
                do
                {
                    count++;

                    Debug.Assert(current is not null);
                    current = current.Next;
                }
                while (current != head);
            }

            return count;
        }

        /// <summary>Creates and returns an exception object to indicate that a channel has been closed.</summary>
        internal static Exception CreateInvalidCompletionException(Exception? inner = null) =>
            inner is OperationCanceledException ? inner :
            inner is not null && inner != s_doneWritingSentinel ? new ChannelClosedException(inner) :
            new ChannelClosedException();
    }
}
