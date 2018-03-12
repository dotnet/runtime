// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks.Sources;

#if !netstandard
using Internal.Runtime.CompilerServices;
#endif

namespace System.Threading.Tasks
{
    /// <summary>Provides an awaitable result of an asynchronous operation.</summary>
    /// <remarks>
    /// <see cref="ValueTask"/>s are meant to be directly awaited.  To do more complicated operations with them, a <see cref="Task"/>
    /// should be extracted using <see cref="AsTask"/>.  Such operations might include caching an instance to be awaited later,
    /// registering multiple continuations with a single operation, awaiting the same task multiple times, and using combinators over
    /// multiple operations.
    /// </remarks>
    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ValueTask : IEquatable<ValueTask>
    {
        /// <summary>A task canceled using `new CancellationToken(true)`.</summary>
        private static readonly Task s_canceledTask =
#if netstandard
            Task.Delay(Timeout.Infinite, new CancellationToken(canceled: true));
#else
            Task.FromCanceled(new CancellationToken(canceled: true));
#endif
        /// <summary>A successfully completed task.</summary>
        internal static Task CompletedTask
#if netstandard
            { get; } = Task.Delay(0);
#else
            => Task.CompletedTask;
#endif

        /// <summary>null if representing a successful synchronous completion, otherwise a <see cref="Task"/> or a <see cref="IValueTaskSource"/>.</summary>
        internal readonly object _obj;
        /// <summary>Flags providing additional details about the ValueTask's contents and behavior.</summary>
        internal readonly ValueTaskFlags _flags;
        /// <summary>Opaque value passed through to the <see cref="IValueTaskSource"/>.</summary>
        internal readonly short _token;

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

            _flags = ValueTaskFlags.ObjectIsTask;
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

            _flags = 0;
        }

        /// <summary>Non-verified initialization of the struct to the specified values.</summary>
        /// <param name="obj">The object.</param>
        /// <param name="token">The token.</param>
        /// <param name="flags">The flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask(object obj, short token, ValueTaskFlags flags)
        {
            _obj = obj;
            _token = token;
            _flags = flags;
        }

        /// <summary>Gets whether the contination should be scheduled to the current context.</summary>
        internal bool ContinueOnCapturedContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & ValueTaskFlags.AvoidCapturedContext) == 0;
        }

        /// <summary>Gets whether the object in the <see cref="_obj"/> field is a <see cref="Task"/>.</summary>
        internal bool ObjectIsTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & ValueTaskFlags.ObjectIsTask) != 0;
        }

        /// <summary>Returns the <see cref="Task"/> stored in <see cref="_obj"/>.  This uses <see cref="Unsafe"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task UnsafeGetTask()
        {
            Debug.Assert(ObjectIsTask);
            Debug.Assert(_obj is Task);
            return Unsafe.As<Task>(_obj);
        }

        /// <summary>Returns the <see cref="IValueTaskSource"/> stored in <see cref="_obj"/>.  This uses <see cref="Unsafe"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IValueTaskSource UnsafeGetValueTaskSource()
        {
            Debug.Assert(!ObjectIsTask);
            Debug.Assert(_obj is IValueTaskSource);
            return Unsafe.As<IValueTaskSource>(_obj);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() => _obj?.GetHashCode() ?? 0;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj) =>
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
        public Task AsTask() =>
            _obj == null ? ValueTask.CompletedTask :
            ObjectIsTask ? UnsafeGetTask() :
            GetTaskForValueTaskSource();

        /// <summary>Gets a <see cref="ValueTask"/> that may be used at any point in the future.</summary>
        public ValueTask Preserve() => _obj == null ? this : new ValueTask(AsTask());

        /// <summary>Creates a <see cref="Task"/> to represent the <see cref="IValueTaskSource"/>.</summary>
        private Task GetTaskForValueTaskSource()
        {
            IValueTaskSource t = UnsafeGetValueTaskSource();
            ValueTaskSourceStatus status = t.GetStatus(_token);
            if (status != ValueTaskSourceStatus.Pending)
            {
                try
                {
                    // Propagate any exceptions that may have occurred, then return
                    // an already successfully completed task.
                    t.GetResult(_token);
                    return ValueTask.CompletedTask;

                    // If status is Faulted or Canceled, GetResult should throw.  But
                    // we can't guarantee every implementation will do the "right thing".
                    // If it doesn't throw, we just treat that as success and ignore
                    // the status.
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
#if !netstandard
                        if (exc is OperationCanceledException oce)
                        {
                            var task = new Task<VoidTaskResult>();
                            task.TrySetCanceled(oce.CancellationToken, oce);
                            return task;
                        }
#endif
                        return s_canceledTask;
                    }
                    else
                    {
#if netstandard
                        var tcs = new TaskCompletionSource<bool>();
                        tcs.TrySetException(exc);
                        return tcs.Task;
#else
                        return Task.FromException(exc);
#endif
                    }
                }
            }

            var m = new ValueTaskSourceTask(t, _token);
            return
#if netstandard
                m.Task;
#else
                m;
#endif
        }

        /// <summary>Type used to create a <see cref="Task"/> to represent a <see cref="IValueTaskSource"/>.</summary>
        private sealed class ValueTaskSourceTask :
#if netstandard
            TaskCompletionSource<bool>
#else
            Task<VoidTaskResult>
#endif
        {
            private static readonly Action<object> s_completionAction = state =>
            {
                if (!(state is ValueTaskSourceTask vtst) ||
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
                    vtst.TrySetResult(default);
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
#if netstandard
                        vtst.TrySetCanceled();
#else
                        if (exc is OperationCanceledException oce)
                        {
                            vtst.TrySetCanceled(oce.CancellationToken, oce);
                        }
                        else
                        {
                            vtst.TrySetCanceled(new CancellationToken(true));
                        }
#endif
                    }
                    else
                    {
                        vtst.TrySetException(exc);
                    }
                }
            };

            /// <summary>The associated <see cref="IValueTaskSource"/>.</summary>
            private IValueTaskSource _source;
            /// <summary>The token to pass through to operations on <see cref="_source"/></summary>
            private readonly short _token;

            public ValueTaskSourceTask(IValueTaskSource source, short token)
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
            get => _obj == null || (ObjectIsTask ? UnsafeGetTask().IsCompleted : UnsafeGetValueTaskSource().GetStatus(_token) != ValueTaskSourceStatus.Pending);
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a successfully completed operation.</summary>
        public bool IsCompletedSuccessfully
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                _obj == null ||
                (ObjectIsTask ?
#if netstandard
                    UnsafeGetTask().Status == TaskStatus.RanToCompletion :
#else
                    UnsafeGetTask().IsCompletedSuccessfully :
#endif
                    UnsafeGetValueTaskSource().GetStatus(_token) == ValueTaskSourceStatus.Succeeded);
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a failed operation.</summary>
        public bool IsFaulted
        {
            get =>
                _obj != null &&
                (ObjectIsTask ? UnsafeGetTask().IsFaulted : UnsafeGetValueTaskSource().GetStatus(_token) == ValueTaskSourceStatus.Faulted);
        }

        /// <summary>Gets whether the <see cref="ValueTask"/> represents a canceled operation.</summary>
        /// <remarks>
        /// If the <see cref="ValueTask"/> is backed by a result or by a <see cref="IValueTaskSource"/>,
        /// this will always return false.  If it's backed by a <see cref="Task"/>, it'll return the
        /// value of the task's <see cref="Task.IsCanceled"/> property.
        /// </remarks>
        public bool IsCanceled
        {
            get =>
                _obj != null &&
                (ObjectIsTask ? UnsafeGetTask().IsCanceled : UnsafeGetValueTaskSource().GetStatus(_token) == ValueTaskSourceStatus.Canceled);
        }

        /// <summary>Throws the exception that caused the <see cref="ValueTask"/> to fail.  If it completed successfully, nothing is thrown.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [StackTraceHidden]
        internal void ThrowIfCompletedUnsuccessfully()
        {
            if (_obj != null)
            {
                if (ObjectIsTask)
                {
#if netstandard
                    UnsafeGetTask().GetAwaiter().GetResult();
#else
                    TaskAwaiter.ValidateEnd(UnsafeGetTask());
#endif
                }
                else
                {
                    UnsafeGetValueTaskSource().GetResult(_token);
                }
            }
        }

        /// <summary>Gets an awaiter for this <see cref="ValueTask"/>.</summary>
        public ValueTaskAwaiter GetAwaiter() => new ValueTaskAwaiter(this);

        /// <summary>Configures an awaiter for this <see cref="ValueTask"/>.</summary>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the captured context; otherwise, false.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable ConfigureAwait(bool continueOnCapturedContext)
        {
            // TODO: Simplify once https://github.com/dotnet/coreclr/pull/16138 is fixed.
            bool avoidCapture = !continueOnCapturedContext;
            return new ConfiguredValueTaskAwaitable(new ValueTask(_obj, _token, _flags | Unsafe.As<bool, ValueTaskFlags>(ref avoidCapture)));
        }
    }

    /// <summary>Provides a value type that can represent a synchronously available value or a task object.</summary>
    /// <typeparam name="TResult">Specifies the type of the result.</typeparam>
    /// <remarks>
    /// <see cref="ValueTask{TResult}"/>s are meant to be directly awaited.  To do more complicated operations with them, a <see cref="Task"/>
    /// should be extracted using <see cref="AsTask"/> or <see cref="Preserve"/>.  Such operations might include caching an instance to
    /// be awaited later, registering multiple continuations with a single operation, awaiting the same task multiple times, and using
    /// combinators over multiple operations.
    /// </remarks>
    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder<>))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
    {
        /// <summary>A task canceled using `new CancellationToken(true)`. Lazily created only when first needed.</summary>
        private static Task<TResult> s_canceledTask;
        /// <summary>null if <see cref="_result"/> has the result, otherwise a <see cref="Task{TResult}"/> or a <see cref="IValueTaskSource{TResult}"/>.</summary>
        internal readonly object _obj;
        /// <summary>The result to be used if the operation completed successfully synchronously.</summary>
        internal readonly TResult _result;
        /// <summary>Flags providing additional details about the ValueTask's contents and behavior.</summary>
        internal readonly ValueTaskFlags _flags;
        /// <summary>Opaque value passed through to the <see cref="IValueTaskSource{TResult}"/>.</summary>
        internal readonly short _token;

        // An instance created with the default ctor (a zero init'd struct) represents a synchronously, successfully completed operation
        // with a result of default(TResult).

        /// <summary>Initialize the <see cref="ValueTask{TResult}"/> with a <typeparamref name="TResult"/> result value.</summary>
        /// <param name="result">The result.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTask(TResult result)
        {
            _result = result;

            _obj = null;
            _flags = 0;
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
            _flags = ValueTaskFlags.ObjectIsTask;
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
            _flags = 0;
        }

        /// <summary>Non-verified initialization of the struct to the specified values.</summary>
        /// <param name="obj">The object.</param>
        /// <param name="result">The result.</param>
        /// <param name="token">The token.</param>
        /// <param name="flags">The flags.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private ValueTask(object obj, TResult result, short token, ValueTaskFlags flags)
        {
            _obj = obj;
            _result = result;
            _token = token;
            _flags = flags;
        }

        /// <summary>Gets whether the contination should be scheduled to the current context.</summary>
        internal bool ContinueOnCapturedContext
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & ValueTaskFlags.AvoidCapturedContext) == 0;
        }

        /// <summary>Gets whether the object in the <see cref="_obj"/> field is a <see cref="Task{TResult}"/>.</summary>
        internal bool ObjectIsTask
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (_flags & ValueTaskFlags.ObjectIsTask) != 0;
        }

        /// <summary>Returns the <see cref="Task{TResult}"/> stored in <see cref="_obj"/>.  This uses <see cref="Unsafe"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal Task<TResult> UnsafeGetTask()
        {
            Debug.Assert(ObjectIsTask);
            Debug.Assert(_obj is Task<TResult>);
            return Unsafe.As<Task<TResult>>(_obj);
        }

        /// <summary>Returns the <see cref="IValueTaskSource{TResult}"/> stored in <see cref="_obj"/>.  This uses <see cref="Unsafe"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal IValueTaskSource<TResult> UnsafeGetValueTaskSource()
        {
            Debug.Assert(!ObjectIsTask);
            Debug.Assert(_obj is IValueTaskSource<TResult>);
            return Unsafe.As<IValueTaskSource<TResult>>(_obj);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() =>
            _obj != null ? _obj.GetHashCode() :
            _result != null ? _result.GetHashCode() :
            0;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj) =>
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
        public Task<TResult> AsTask() =>
            _obj == null ?
#if netstandard
                Task.FromResult(_result) :
#else
                AsyncTaskMethodBuilder<TResult>.GetTaskForResult(_result) :
#endif
            ObjectIsTask ? UnsafeGetTask() :
            GetTaskForValueTaskSource();

        /// <summary>Gets a <see cref="ValueTask{TResult}"/> that may be used at any point in the future.</summary>
        public ValueTask<TResult> Preserve() => _obj == null ? this : new ValueTask<TResult>(AsTask());

        /// <summary>Creates a <see cref="Task{TResult}"/> to represent the <see cref="IValueTaskSource{TResult}"/>.</summary>
        private Task<TResult> GetTaskForValueTaskSource()
        {
            IValueTaskSource<TResult> t = UnsafeGetValueTaskSource();
            ValueTaskSourceStatus status = t.GetStatus(_token);
            if (status != ValueTaskSourceStatus.Pending)
            {
                try
                {
                    // Get the result of the operation and return a task for it.
                    // If any exception occurred, propagate it
                    return
#if netstandard
                        Task.FromResult(t.GetResult(_token));
#else
                        AsyncTaskMethodBuilder<TResult>.GetTaskForResult(t.GetResult(_token));
#endif

                    // If status is Faulted or Canceled, GetResult should throw.  But
                    // we can't guarantee every implementation will do the "right thing".
                    // If it doesn't throw, we just treat that as success and ignore
                    // the status.
                }
                catch (Exception exc)
                {
                    if (status == ValueTaskSourceStatus.Canceled)
                    {
#if !netstandard
                        if (exc is OperationCanceledException oce)
                        {
                            var task = new Task<TResult>();
                            task.TrySetCanceled(oce.CancellationToken, oce);
                            return task;
                        }
#endif

                        Task<TResult> canceledTask = s_canceledTask;
                        if (canceledTask == null)
                        {
#if netstandard
                            var tcs = new TaskCompletionSource<TResult>();
                            tcs.TrySetCanceled();
                            canceledTask = tcs.Task;
#else
                            canceledTask = Task.FromCanceled<TResult>(new CancellationToken(true));
#endif
                            // Benign race condition to initialize cached task, as identity doesn't matter.
                            s_canceledTask = canceledTask;
                        }
                        return canceledTask;
                    }
                    else
                    {
#if netstandard
                        var tcs = new TaskCompletionSource<TResult>();
                        tcs.TrySetException(exc);
                        return tcs.Task;
#else
                        return Task.FromException<TResult>(exc);
#endif
                    }
                }
            }

            var m = new ValueTaskSourceTask(t, _token);
            return
#if netstandard
                m.Task;
#else
                m;
#endif
        }

        /// <summary>Type used to create a <see cref="Task{TResult}"/> to represent a <see cref="IValueTaskSource{TResult}"/>.</summary>
        private sealed class ValueTaskSourceTask :
#if netstandard
            TaskCompletionSource<TResult>
#else
            Task<TResult>
#endif
        {
            private static readonly Action<object> s_completionAction = state =>
            {
                if (!(state is ValueTaskSourceTask vtst) ||
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
#if netstandard
                        vtst.TrySetCanceled();
#else
                        if (exc is OperationCanceledException oce)
                        {
                            vtst.TrySetCanceled(oce.CancellationToken, oce);
                        }
                        else
                        {
                            vtst.TrySetCanceled(new CancellationToken(true));
                        }
#endif
                    }
                    else
                    {
                        vtst.TrySetException(exc);
                    }
                }
            };

            /// <summary>The associated <see cref="IValueTaskSource"/>.</summary>
            private IValueTaskSource<TResult> _source;
            /// <summary>The token to pass through to operations on <see cref="_source"/></summary>
            private readonly short _token;

            public ValueTaskSourceTask(IValueTaskSource<TResult> source, short token)
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
            get => _obj == null || (ObjectIsTask ? UnsafeGetTask().IsCompleted : UnsafeGetValueTaskSource().GetStatus(_token) != ValueTaskSourceStatus.Pending);
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a successfully completed operation.</summary>
        public bool IsCompletedSuccessfully
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get =>
                _obj == null ||
                (ObjectIsTask ?
#if netstandard
                    UnsafeGetTask().Status == TaskStatus.RanToCompletion :
#else
                    UnsafeGetTask().IsCompletedSuccessfully :
#endif
                    UnsafeGetValueTaskSource().GetStatus(_token) == ValueTaskSourceStatus.Succeeded);
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a failed operation.</summary>
        public bool IsFaulted
        {
            get =>
                _obj != null &&
                (ObjectIsTask ? UnsafeGetTask().IsFaulted : UnsafeGetValueTaskSource().GetStatus(_token) == ValueTaskSourceStatus.Faulted);
        }

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a canceled operation.</summary>
        /// <remarks>
        /// If the <see cref="ValueTask{TResult}"/> is backed by a result or by a <see cref="IValueTaskSource{TResult}"/>,
        /// this will always return false.  If it's backed by a <see cref="Task"/>, it'll return the
        /// value of the task's <see cref="Task.IsCanceled"/> property.
        /// </remarks>
        public bool IsCanceled
        {
            get =>
                _obj != null &&
                (ObjectIsTask ? UnsafeGetTask().IsCanceled : UnsafeGetValueTaskSource().GetStatus(_token) == ValueTaskSourceStatus.Canceled);
        }

        /// <summary>Gets the result.</summary>
        public TResult Result
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                if (_obj == null)
                {
                    return _result;
                }

                if (ObjectIsTask)
                {
#if netstandard
                    return UnsafeGetTask().GetAwaiter().GetResult();
#else
                    Task<TResult> t = UnsafeGetTask();
                    TaskAwaiter.ValidateEnd(t);
                    return t.ResultOnSuccess;
#endif
                }

                return UnsafeGetValueTaskSource().GetResult(_token);
            }
        }

        /// <summary>Gets an awaiter for this <see cref="ValueTask{TResult}"/>.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ValueTaskAwaiter<TResult> GetAwaiter() => new ValueTaskAwaiter<TResult>(this);

        /// <summary>Configures an awaiter for this <see cref="ValueTask{TResult}"/>.</summary>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the captured context; otherwise, false.
        /// </param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext)
        {
            // TODO: Simplify once https://github.com/dotnet/coreclr/pull/16138 is fixed.
            bool avoidCapture = !continueOnCapturedContext;
            return new ConfiguredValueTaskAwaitable<TResult>(new ValueTask<TResult>(_obj, _result, _token, _flags | Unsafe.As<bool, ValueTaskFlags>(ref avoidCapture)));
        }

        /// <summary>Gets a string-representation of this <see cref="ValueTask{TResult}"/>.</summary>
        public override string ToString()
        {
            if (IsCompletedSuccessfully)
            {
                TResult result = Result;
                if (result != null)
                {
                    return result.ToString();
                }
            }

            return string.Empty;
        }
    }

    /// <summary>Internal flags used in the implementation of <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/>.</summary>
    [Flags]
    internal enum ValueTaskFlags : byte
    {
        /// <summary>
        /// Indicates that context (e.g. SynchronizationContext) should not be captured when adding
        /// a continuation.
        /// </summary>
        /// <remarks>
        /// The value here must be 0x1, to match the value of a true Boolean reinterpreted as a byte.
        /// This only has meaning when awaiting a ValueTask, with ConfigureAwait creating a new
        /// ValueTask setting or not setting this flag appropriately.
        /// </remarks>
        AvoidCapturedContext = 0x1,

        /// <summary>
        /// Indicates that the ValueTask's object field stores a Task.  This is used to avoid
        /// a type check on whatever is stored in the object field.
        /// </summary>
        ObjectIsTask = 0x2
    }
}
