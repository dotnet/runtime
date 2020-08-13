// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;
using Internal.Runtime.CompilerServices;

namespace System.Threading.Tasks
{
    // TYPE SAFETY WARNING:
    // This code uses Unsafe.As to cast _obj.  This is done in order to minimize the costs associated with
    // casting _obj to a variety of different types that can be stored in a ValueTask, e.g. Task<TResult>
    // vs IValueTaskSource<TResult>.  Previous attempts at this were faulty due to using a separate field
    // to store information about the type of the object in _obj; this is faulty because if the ValueTask
    // is stored into a field, concurrent read/writes can result in tearing the _obj from the type information
    // stored in a separate field.  This means we can rely only on the _obj field to determine how to handle
    // it.  As such, the pattern employed is to copy _obj into a local obj, and then check it for null and
    // type test against Task/Task<TResult>.  Since the ValueTask can only be constructed with null, Task,
    // or IValueTaskSource, we can then be confident in knowing that if it doesn't match one of those values,
    // it must be an IValueTaskSource, and we can use Unsafe.As.  This could be defeated by other unsafe means,
    // like private reflection or using Unsafe.As manually, but at that point you're already doing things
    // that can violate type safety; we only care about getting correct behaviors when using "safe" code.
    // There are still other race conditions in user's code that can result in errors, but such errors don't
    // cause ValueTask to violate type safety.

    /// <summary>Provides an awaitable result of an asynchronous operation.</summary>
    /// <remarks>
    /// <see cref="ValueTask"/> instances are meant to be directly awaited.  To do more complicated operations with them, a <see cref="Task"/>
    /// should be extracted using <see cref="AsTask"/>.  Such operations might include caching a task instance to be awaited later,
    /// registering multiple continuations with a single task, awaiting the same task multiple times, and using combinators over
    /// multiple operations:
    /// <list type="bullet">
    /// <item>
    /// Once the result of a <see cref="ValueTask"/> instance has been retrieved, do not attempt to retrieve it again.
    /// <see cref="ValueTask"/> instances may be backed by <see cref="IValueTaskSource"/> instances that are reusable, and such
    /// instances may use the act of retrieving the instances result as a notification that the instance may now be reused for
    /// a different operation.  Attempting to then reuse that same <see cref="ValueTask"/> results in undefined behavior.
    /// </item>
    /// <item>
    /// Do not attempt to add multiple continuations to the same <see cref="ValueTask"/>.  While this might work if the
    /// <see cref="ValueTask"/> wraps a <code>T</code> or a <see cref="Task"/>, it may not work if the <see cref="ValueTask"/>
    /// was constructed from an <see cref="IValueTaskSource"/>.
    /// </item>
    /// <item>
    /// Some operations that return a <see cref="ValueTask"/> may invalidate it based on some subsequent operation being performed.
    /// Unless otherwise documented, assume that a <see cref="ValueTask"/> should be awaited prior to performing any additional operations
    /// on the instance from which it was retrieved.
    /// </item>
    /// </list>
    /// </remarks>
    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ValueTask : IEquatable<ValueTask>
    {
        /// <summary>A task canceled using `new CancellationToken(true)`.</summary>
        private static readonly Task s_canceledTask = Task.FromCanceled(new CancellationToken(canceled: true));

        /// <summary>null if representing a successful synchronous completion, otherwise a <see cref="Task"/> or a <see cref="IValueTaskSource"/>.</summary>
        internal readonly object? _obj;
        /// <summary>Opaque value passed through to the <see cref="IValueTaskSource"/>.</summary>
        internal readonly short _token;
        /// <summary>true to continue on the captured context; otherwise, false.</summary>
        /// <remarks>Stored in the <see cref="ValueTask"/> rather than in the configured awaiter to utilize otherwise padding space.</remarks>
        internal readonly bool _continueOnCapturedContext;

        // An instance created with the default ctor (a zero init'd struct) represents a synchronously, successfully completed operation.

        /// <summary>Initialize the <see cref="ValueTask"/> with a <see cref="Task"/> that represents the operation.</summary>
        /// <param name="task">The task.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask(Task task)
        {
            if (task == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.task);
            }

            _obj = task;

            _continueOnCapturedContext = true;
            _token = 0;
        }

        /// <summary>Initialize the <see cref="ValueTask"/> with a <see cref="IValueTaskSource"/> object that represents the operation.</summary>
        /// <param name="source">The source.</param>
        /// <param name="token">Opaque value passed through to the <see cref="IValueTaskSource"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask(IValueTaskSource source, short token)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            _obj = source;
            _token = token;

            _continueOnCapturedContext = true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask(object? obj, short token, bool continueOnCapturedContext)
        {
            _obj = obj;
            _token = token;
            _continueOnCapturedContext = continueOnCapturedContext;
        }

        /// <summary>Gets a task that has already completed successfully.</summary>
        public static ValueTask CompletedTask => default;

        /// <summary>Creates a <see cref="ValueTask{TResult}"/> that's completed successfully with the specified result.</summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        public static ValueTask<TResult> FromResult<TResult>(TResult result) =>
            new ValueTask<TResult>(result);

        /// <summary>Creates a <see cref="ValueTask"/> that has completed due to cancellation with the specified cancellation token.</summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        public static ValueTask FromCanceled(CancellationToken cancellationToken) =>
            new ValueTask(Task.FromCanceled(cancellationToken));

        /// <summary>Creates a <see cref="ValueTask{TResult}"/> that has completed due to cancellation with the specified cancellation token.</summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        public static ValueTask<TResult> FromCanceled<TResult>(CancellationToken cancellationToken) =>
            new ValueTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));

        /// <summary>Creates a <see cref="ValueTask"/> that has completed with the specified exception.</summary>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static ValueTask FromException(Exception exception) =>
            new ValueTask(Task.FromException(exception));

        /// <summary>Creates a <see cref="ValueTask{TResult}"/> that has completed with the specified exception.</summary>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        public static ValueTask<TResult> FromException<TResult>(Exception exception) =>
            new ValueTask<TResult>(Task.FromException<TResult>(exception));

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() => _obj?.GetHashCode() ?? 0;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object? obj) =>
            obj is ValueTask &&
            Equals((ValueTask)obj);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ValueTask"/> value.</summary>
        public bool Equals(ValueTask other) => _obj == other._obj && _token == other._token;

        /// <summary>Returns a value indicating whether two <see cref="ValueTask"/> values are equal.</summary>
        public static bool operator ==(ValueTask left, ValueTask right) =>
            left.Equals(right);

        /// <summary>Returns a value indicating whether two <see cref="ValueTask"/> values are not equal.</summary>
        public static bool operator !=(ValueTask left, ValueTask right) =>
            !left.Equals(right);

        /// <summary>
        /// Gets a <see cref="Task"/> object to represent this ValueTask.
        /// </summary>
        /// <remarks>
        /// It will either return the wrapped task object if one exists, or it'll
        /// manufacture a new task object to represent the result.
        /// </remarks>
        public Task AsTask()
        {
            object? obj = _obj;
            Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);
            return
                obj == null ? Task.CompletedTask :
                obj as Task ??
                GetTaskForValueTaskSource(Unsafe.As<IValueTaskSource>(obj));
        }

        /// <summary>Gets a <see cref="ValueTask"/> that may be used at any point in the future.</summary>
        public ValueTask Preserve() => _obj == null ? this : new ValueTask(AsTask());

        /// <summary>Creates a <see cref="Task"/> to represent the <see cref="IValueTaskSource"/>.</summary>
        /// <remarks>
        /// The <see cref="IValueTaskSource"/> is passed in rather than reading and casting <see cref="_obj"/>
        /// so that the caller can pass in an object it's already validated.
        /// </remarks>
        private Task GetTaskForValueTaskSource(IValueTaskSource t)
        {
            ValueTaskSourceStatus status = t.GetStatus(_token);
            if (status != ValueTaskSourceStatus.Pending)
            {
                try
                {
                    // Propagate any exceptions that may have occurred, then return
                    // an already successfully completed task.
                    t.GetResult(_token);
                    return Task.CompletedTask;

                    // If status is Faulted or Canceled, GetResult should throw.  But
                    // we can't guarantee every implementation will do the "right thing".
                    // If it doesn't throw, we just treat that as success and ignore
                    // the status.
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
                        if (exc is OperationCanceledException oce)
                        {
                            var task = new Task();
                            task.TrySetCanceled(oce.CancellationToken, oce);
                            return task;
                        }

                        return s_canceledTask;
                    }
                    else
                    {
                        return Task.FromException(exc);
                    }
                }
            }

            return new ValueTaskSourceAsTask(t, _token);
        }

        /// <summary>Type used to create a <see cref="Task"/> to represent a <see cref="IValueTaskSource"/>.</summary>
        private sealed class ValueTaskSourceAsTask : Task
        {
            private static readonly Action<object?> s_completionAction = static state =>
            {
                if (!(state is ValueTaskSourceAsTask vtst) ||
                    !(vtst._source is IValueTaskSource source))
                {
                    // This could only happen if the IValueTaskSource passed the wrong state
                    // or if this callback were invoked multiple times such that the state
                    // was previously nulled out.
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
                    return;
                }

                vtst._source = null;
                ValueTaskSourceStatus status = source.GetStatus(vtst._token);
                try
                {
                    source.GetResult(vtst._token);
                    vtst.TrySetResult();
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
                        if (exc is OperationCanceledException oce)
                        {
                            vtst.TrySetCanceled(oce.CancellationToken, oce);
                        }
                        else
                        {
                            vtst.TrySetCanceled(new CancellationToken(true));
                        }
                    }
                    else
                    {
                        vtst.TrySetException(exc);
                    }
                }
            };

            /// <summary>The associated <see cref="IValueTaskSource"/>.</summary>
            private IValueTaskSource? _source;
            /// <summary>The token to pass through to operations on <see cref="_source"/></summary>
            private readonly short _token;

            internal ValueTaskSourceAsTask(IValueTaskSource source, short token)
            {
                _token = token;
                _source = source;
                source.OnCompleted(s_completionAction, this, token, ValueTaskSourceOnCompletedFlags.None);
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a completed operation.</summary>
        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

                if (obj == null)
                {
                    return true;
                }

                if (obj is Task t)
                {
                    return t.IsCompleted;
                }

                return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) != ValueTaskSourceStatus.Pending;
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a successfully completed operation.</summary>
        public bool IsCompletedSuccessfully
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

                if (obj == null)
                {
                    return true;
                }

                if (obj is Task t)
                {
                    return t.IsCompletedSuccessfully;
                }

                return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) == ValueTaskSourceStatus.Succeeded;
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a failed operation.</summary>
        public bool IsFaulted
        {
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

                if (obj == null)
                {
                    return false;
                }

                if (obj is Task t)
                {
                    return t.IsFaulted;
                }

                return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) == ValueTaskSourceStatus.Faulted;
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a canceled operation.</summary>
        /// <remarks>
        /// If the <see cref="ValueTask"/> is backed by a result or by a <see cref="IValueTaskSource"/>,
        /// this will always return false.  If it's backed by a <see cref="Task"/>, it'll return the
        /// value of the task's <see cref="Task.IsCanceled"/> property.
        /// </remarks>
        public bool IsCanceled
        {
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

                if (obj == null)
                {
                    return false;
                }

                if (obj is Task t)
                {
                    return t.IsCanceled;
                }

                return Unsafe.As<IValueTaskSource>(obj).GetStatus(_token) == ValueTaskSourceStatus.Canceled;
            }
        }

        /// <summary>Throws the exception that caused the <see cref="ValueTask"/> to fail.  If it completed successfully, nothing is thrown.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal void ThrowIfCompletedUnsuccessfully()
        {
            object? obj = _obj;
            Debug.Assert(obj == null || obj is Task || obj is IValueTaskSource);

            if (obj != null)
            {
                if (obj is Task t)
                {
                    TaskAwaiter.ValidateEnd(t);
                }
                else
                {
                    Unsafe.As<IValueTaskSource>(obj).GetResult(_token);
                }
            }
        }

        /// <summary>Gets an awaiter for this <see cref="ValueTask"/>.</summary>
        public ValueTaskAwaiter GetAwaiter() => new ValueTaskAwaiter(in this);

        /// <summary>Configures an awaiter for this <see cref="ValueTask"/>.</summary>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the captured context; otherwise, false.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable(new ValueTask(_obj, _token, continueOnCapturedContext));
    }

    /// <summary>Provides a value type that can represent a synchronously available value or a task object.</summary>
    /// <typeparam name="TResult">Specifies the type of the result.</typeparam>
    /// <remarks>
    /// <see cref="ValueTask{TResult}"/> instances are meant to be directly awaited.  To do more complicated operations with them, a <see cref="Task{TResult}"/>
    /// should be extracted using <see cref="AsTask"/>.  Such operations might include caching a task instance to be awaited later,
    /// registering multiple continuations with a single task, awaiting the same task multiple times, and using combinators over
    /// multiple operations:
    /// <list type="bullet">
    /// <item>
    /// Once the result of a <see cref="ValueTask{TResult}"/> instance has been retrieved, do not attempt to retrieve it again.
    /// <see cref="ValueTask{TResult}"/> instances may be backed by <see cref="IValueTaskSource{TResult}"/> instances that are reusable, and such
    /// instances may use the act of retrieving the instances result as a notification that the instance may now be reused for
    /// a different operation.  Attempting to then reuse that same <see cref="ValueTask{TResult}"/> results in undefined behavior.
    /// </item>
    /// <item>
    /// Do not attempt to add multiple continuations to the same <see cref="ValueTask{TResult}"/>.  While this might work if the
    /// <see cref="ValueTask{TResult}"/> wraps a <code>T</code> or a <see cref="Task{TResult}"/>, it may not work if the <see cref="Task{TResult}"/>
    /// was constructed from an <see cref="IValueTaskSource{TResult}"/>.
    /// </item>
    /// <item>
    /// Some operations that return a <see cref="ValueTask{TResult}"/> may invalidate it based on some subsequent operation being performed.
    /// Unless otherwise documented, assume that a <see cref="ValueTask{TResult}"/> should be awaited prior to performing any additional operations
    /// on the instance from which it was retrieved.
    /// </item>
    /// </list>
    /// </remarks>
    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder<>))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
    {
        /// <summary>A task canceled using `new CancellationToken(true)`. Lazily created only when first needed.</summary>
        private static Task<TResult>? s_canceledTask;
        /// <summary>null if <see cref="_result"/> has the result, otherwise a <see cref="Task{TResult}"/> or a <see cref="IValueTaskSource{TResult}"/>.</summary>
        internal readonly object? _obj;
        /// <summary>The result to be used if the operation completed successfully synchronously.</summary>
        internal readonly TResult? _result;
        /// <summary>Opaque value passed through to the <see cref="IValueTaskSource{TResult}"/>.</summary>
        internal readonly short _token;
        /// <summary>true to continue on the captured context; otherwise, false.</summary>
        /// <remarks>Stored in the <see cref="ValueTask{TResult}"/> rather than in the configured awaiter to utilize otherwise padding space.</remarks>
        internal readonly bool _continueOnCapturedContext;

        // An instance created with the default ctor (a zero init'd struct) represents a synchronously, successfully completed operation
        // with a result of default(TResult).

        /// <summary>Initialize the <see cref="ValueTask{TResult}"/> with a <typeparamref name="TResult"/> result value.</summary>
        /// <param name="result">The result.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask(TResult result)
        {
            _result = result;

            _obj = null;
            _continueOnCapturedContext = true;
            _token = 0;
        }

        /// <summary>Initialize the <see cref="ValueTask{TResult}"/> with a <see cref="Task{TResult}"/> that represents the operation.</summary>
        /// <param name="task">The task.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask(Task<TResult> task)
        {
            if (task == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.task);
            }

            _obj = task;

            _result = default;
            _continueOnCapturedContext = true;
            _token = 0;
        }

        /// <summary>Initialize the <see cref="ValueTask{TResult}"/> with a <see cref="IValueTaskSource{TResult}"/> object that represents the operation.</summary>
        /// <param name="source">The source.</param>
        /// <param name="token">Opaque value passed through to the <see cref="IValueTaskSource"/>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask(IValueTaskSource<TResult> source, short token)
        {
            if (source == null)
            {
                ThrowHelper.ThrowArgumentNullException(ExceptionArgument.source);
            }

            _obj = source;
            _token = token;

            _result = default;
            _continueOnCapturedContext = true;
        }

        /// <summary>Non-verified initialization of the struct to the specified values.</summary>
        /// <param name="obj">The object.</param>
        /// <param name="result">The result.</param>
        /// <param name="token">The token.</param>
        /// <param name="continueOnCapturedContext">true to continue on captured context; otherwise, false.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask(object? obj, TResult? result, short token, bool continueOnCapturedContext)
        {
            _obj = obj;
            _result = result;
            _token = token;
            _continueOnCapturedContext = continueOnCapturedContext;
        }


        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() =>
            _obj != null ? _obj.GetHashCode() :
            _result != null ? _result.GetHashCode() :
            0;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object? obj) =>
            obj is ValueTask<TResult> &&
            Equals((ValueTask<TResult>)obj);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ValueTask{TResult}"/> value.</summary>
        public bool Equals(ValueTask<TResult> other) =>
            _obj != null || other._obj != null ?
                _obj == other._obj && _token == other._token :
                EqualityComparer<TResult>.Default.Equals(_result, other._result);

        /// <summary>Returns a value indicating whether two <see cref="ValueTask{TResult}"/> values are equal.</summary>
        public static bool operator ==(ValueTask<TResult> left, ValueTask<TResult> right) =>
            left.Equals(right);

        /// <summary>Returns a value indicating whether two <see cref="ValueTask{TResult}"/> values are not equal.</summary>
        public static bool operator !=(ValueTask<TResult> left, ValueTask<TResult> right) =>
            !left.Equals(right);

        /// <summary>
        /// Gets a <see cref="Task{TResult}"/> object to represent this ValueTask.
        /// </summary>
        /// <remarks>
        /// It will either return the wrapped task object if one exists, or it'll
        /// manufacture a new task object to represent the result.
        /// </remarks>
        public Task<TResult> AsTask()
        {
            object? obj = _obj;
            Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

            if (obj == null)
            {
                return AsyncTaskMethodBuilder<TResult>.GetTaskForResult(_result!);
            }

            if (obj is Task<TResult> t)
            {
                return t;
            }

            return GetTaskForValueTaskSource(Unsafe.As<IValueTaskSource<TResult>>(obj));
        }

        /// <summary>Gets a <see cref="ValueTask{TResult}"/> that may be used at any point in the future.</summary>
        public ValueTask<TResult> Preserve() => _obj == null ? this : new ValueTask<TResult>(AsTask());

        /// <summary>Creates a <see cref="Task{TResult}"/> to represent the <see cref="IValueTaskSource{TResult}"/>.</summary>
        /// <remarks>
        /// The <see cref="IValueTaskSource{TResult}"/> is passed in rather than reading and casting <see cref="_obj"/>
        /// so that the caller can pass in an object it's already validated.
        /// </remarks>
        private Task<TResult> GetTaskForValueTaskSource(IValueTaskSource<TResult> t)
        {
            ValueTaskSourceStatus status = t.GetStatus(_token);
            if (status != ValueTaskSourceStatus.Pending)
            {
                try
                {
                    // Get the result of the operation and return a task for it.
                    // If any exception occurred, propagate it
                    return AsyncTaskMethodBuilder<TResult>.GetTaskForResult(t.GetResult(_token));

                    // If status is Faulted or Canceled, GetResult should throw.  But
                    // we can't guarantee every implementation will do the "right thing".
                    // If it doesn't throw, we just treat that as success and ignore
                    // the status.
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
                        if (exc is OperationCanceledException oce)
                        {
                            var task = new Task<TResult>();
                            task.TrySetCanceled(oce.CancellationToken, oce);
                            return task;
                        }

                        Task<TResult>? canceledTask = s_canceledTask;
                        if (canceledTask == null)
                        {
                            // Benign race condition to initialize cached task, as identity doesn't matter.
                            s_canceledTask = canceledTask = Task.FromCanceled<TResult>(new CancellationToken(true));
                        }
                        return canceledTask;
                    }
                    else
                    {
                        return Task.FromException<TResult>(exc);
                    }
                }
            }

            return new ValueTaskSourceAsTask(t, _token);
        }

        /// <summary>Type used to create a <see cref="Task{TResult}"/> to represent a <see cref="IValueTaskSource{TResult}"/>.</summary>
        private sealed class ValueTaskSourceAsTask : Task<TResult>
        {
            private static readonly Action<object?> s_completionAction = static state =>
            {
                if (!(state is ValueTaskSourceAsTask vtst) ||
                    !(vtst._source is IValueTaskSource<TResult> source))
                {
                    // This could only happen if the IValueTaskSource<TResult> passed the wrong state
                    // or if this callback were invoked multiple times such that the state
                    // was previously nulled out.
                    ThrowHelper.ThrowArgumentOutOfRangeException(ExceptionArgument.state);
                    return;
                }

                vtst._source = null;
                ValueTaskSourceStatus status = source.GetStatus(vtst._token);
                try
                {
                    vtst.TrySetResult(source.GetResult(vtst._token));
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
                        if (exc is OperationCanceledException oce)
                        {
                            vtst.TrySetCanceled(oce.CancellationToken, oce);
                        }
                        else
                        {
                            vtst.TrySetCanceled(new CancellationToken(true));
                        }
                    }
                    else
                    {
                        vtst.TrySetException(exc);
                    }
                }
            };

            /// <summary>The associated <see cref="IValueTaskSource"/>.</summary>
            private IValueTaskSource<TResult>? _source;
            /// <summary>The token to pass through to operations on <see cref="_source"/></summary>
            private readonly short _token;

            public ValueTaskSourceAsTask(IValueTaskSource<TResult> source, short token)
            {
                _source = source;
                _token = token;
                source.OnCompleted(s_completionAction, this, token, ValueTaskSourceOnCompletedFlags.None);
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a completed operation.</summary>
        public bool IsCompleted
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

                if (obj == null)
                {
                    return true;
                }

                if (obj is Task<TResult> t)
                {
                    return t.IsCompleted;
                }

                return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) != ValueTaskSourceStatus.Pending;
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a successfully completed operation.</summary>
        public bool IsCompletedSuccessfully
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

                if (obj == null)
                {
                    return true;
                }

                if (obj is Task<TResult> t)
                {
                    return t.IsCompletedSuccessfully;
                }

                return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) == ValueTaskSourceStatus.Succeeded;
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a failed operation.</summary>
        public bool IsFaulted
        {
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

                if (obj == null)
                {
                    return false;
                }

                if (obj is Task<TResult> t)
                {
                    return t.IsFaulted;
                }

                return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) == ValueTaskSourceStatus.Faulted;
            }
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a canceled operation.</summary>
        /// <remarks>
        /// If the <see cref="ValueTask{TResult}"/> is backed by a result or by a <see cref="IValueTaskSource{TResult}"/>,
        /// this will always return false.  If it's backed by a <see cref="Task"/>, it'll return the
        /// value of the task's <see cref="Task.IsCanceled"/> property.
        /// </remarks>
        public bool IsCanceled
        {
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

                if (obj == null)
                {
                    return false;
                }

                if (obj is Task<TResult> t)
                {
                    return t.IsCanceled;
                }

                return Unsafe.As<IValueTaskSource<TResult>>(obj).GetStatus(_token) == ValueTaskSourceStatus.Canceled;
            }
        }

        /// <summary>Gets the result.</summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)] // prevent debugger evaluation from invalidating an underling IValueTaskSource<T>
        public TResult Result
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                object? obj = _obj;
                Debug.Assert(obj == null || obj is Task<TResult> || obj is IValueTaskSource<TResult>);

                if (obj == null)
                {
                    return _result!;
                }

                if (obj is Task<TResult> t)
                {
                    TaskAwaiter.ValidateEnd(t);
                    return t.ResultOnSuccess;
                }

                return Unsafe.As<IValueTaskSource<TResult>>(obj).GetResult(_token);
            }
        }

        /// <summary>Gets an awaiter for this <see cref="ValueTask{TResult}"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter<TResult> GetAwaiter() => new ValueTaskAwaiter<TResult>(in this);

        /// <summary>Configures an awaiter for this <see cref="ValueTask{TResult}"/>.</summary>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the captured context; otherwise, false.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable<TResult>(new ValueTask<TResult>(_obj, _result, _token, continueOnCapturedContext));

        /// <summary>Gets a string-representation of this <see cref="ValueTask{TResult}"/>.</summary>
        public override string? ToString()
        {
            if (IsCompletedSuccessfully)
            {
                Debugger.NotifyOfCrossThreadDependency(); // prevent debugger evaluation from invalidating an underling IValueTaskSource<T> unless forced

                TResult result = Result;
                if (result != null)
                {
                    return result.ToString();
                }
            }

            return string.Empty;
        }
    }
}
