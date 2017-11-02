// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks
{
    /// <summary>
    /// Provides a value type that wraps a <see cref="Task{TResult}"/> and a <typeparamref name="TResult"/>,
    /// only one of which is used.
    /// </summary>
    /// <typeparam name="TResult">The type of the result.</typeparam>
    /// <remarks>
    /// <para>
    /// Methods may return an instance of this value type when it's likely that the result of their
    /// operations will be available synchronously and when the method is expected to be invoked so
    /// frequently that the cost of allocating a new <see cref="Task{TResult}"/> for each call will
    /// be prohibitive.
    /// </para>
    /// <para>
    /// There are tradeoffs to using a <see cref="ValueTask{TResult}"/> instead of a <see cref="Task{TResult}"/>.
    /// For example, while a <see cref="ValueTask{TResult}"/> can help avoid an allocation in the case where the 
    /// successful result is available synchronously, it also contains two fields whereas a <see cref="Task{TResult}"/>
    /// as a reference type is a single field.  This means that a method call ends up returning two fields worth of
    /// data instead of one, which is more data to copy.  It also means that if a method that returns one of these
    /// is awaited within an async method, the state machine for that async method will be larger due to needing
    /// to store the struct that's two fields instead of a single reference.
    /// </para>
    /// <para>
    /// Further, for uses other than consuming the result of an asynchronous operation via await, 
    /// <see cref="ValueTask{TResult}"/> can lead to a more convoluted programming model, which can in turn actually 
    /// lead to more allocations.  For example, consider a method that could return either a <see cref="Task{TResult}"/> 
    /// with a cached task as a common result or a <see cref="ValueTask{TResult}"/>.  If the consumer of the result 
    /// wants to use it as a <see cref="Task{TResult}"/>, such as to use with in methods like Task.WhenAll and Task.WhenAny, 
    /// the <see cref="ValueTask{TResult}"/> would first need to be converted into a <see cref="Task{TResult}"/> using 
    /// <see cref="ValueTask{TResult}.AsTask"/>, which leads to an allocation that would have been avoided if a cached 
    /// <see cref="Task{TResult}"/> had been used in the first place.
    /// </para>
    /// <para>
    /// As such, the default choice for any asynchronous method should be to return a <see cref="Task"/> or 
    /// <see cref="Task{TResult}"/>. Only if performance analysis proves it worthwhile should a <see cref="ValueTask{TResult}"/> 
    /// be used instead of <see cref="Task{TResult}"/>.  There is no non-generic version of <see cref="ValueTask{TResult}"/> 
    /// as the Task.CompletedTask property may be used to hand back a successfully completed singleton in the case where
    /// a <see cref="Task"/>-returning method completes synchronously and successfully.
    /// </para>
    /// </remarks>
    [AsyncMethodBuilder(typeof(AsyncValueTaskMethodBuilder<>))]
    [StructLayout(LayoutKind.Auto)]
    public readonly struct ValueTask<TResult> : IEquatable<ValueTask<TResult>>
    {
        /// <summary>The task to be used if the operation completed asynchronously or if it completed synchronously but non-successfully.</summary>
        internal readonly Task<TResult> _task;
        /// <summary>The result to be used if the operation completed successfully synchronously.</summary>
        internal readonly TResult _result;

        /// <summary>Initialize the <see cref="ValueTask{TResult}"/> with the result of the successful operation.</summary>
        /// <param name="result">The result.</param>
        public ValueTask(TResult result)
        {
            _task = null;
            _result = result;
        }

        /// <summary>
        /// Initialize the <see cref="ValueTask{TResult}"/> with a <see cref="Task{TResult}"/> that represents the operation.
        /// </summary>
        /// <param name="task">The task.</param>
        public ValueTask(Task<TResult> task)
        {
            _task = task ?? throw new ArgumentNullException(nameof(task));
            _result = default(TResult);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        public override int GetHashCode() =>
            _task != null ? _task.GetHashCode() :
            _result != null ? _result.GetHashCode() :
            0;

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="object"/>.</summary>
        public override bool Equals(object obj) =>
            obj is ValueTask<TResult> && 
            Equals((ValueTask<TResult>)obj);

        /// <summary>Returns a value indicating whether this value is equal to a specified <see cref="ValueTask{TResult}"/> value.</summary>
        public bool Equals(ValueTask<TResult> other) =>
            _task != null || other._task != null ? 
                _task == other._task :
                EqualityComparer<TResult>.Default.Equals(_result, other._result);

        /// <summary>Returns a value indicating whether two <see cref="ValueTask{TResult}"/> values are equal.</summary>
        public static bool operator==(ValueTask<TResult> left, ValueTask<TResult> right) =>
            left.Equals(right);

        /// <summary>Returns a value indicating whether two <see cref="ValueTask{TResult}"/> values are not equal.</summary>
        public static bool operator!=(ValueTask<TResult> left, ValueTask<TResult> right) =>
            !left.Equals(right);

        /// <summary>
        /// Gets a <see cref="Task{TResult}"/> object to represent this ValueTask.  It will
        /// either return the wrapped task object if one exists, or it'll manufacture a new
        /// task object to represent the result.
        /// </summary>
        public Task<TResult> AsTask() =>
            // Return the task if we were constructed from one, otherwise manufacture one.  We don't
            // cache the generated task into _task as it would end up changing both equality comparison
            // and the hash code we generate in GetHashCode.
            _task ?? Task.FromResult(_result);

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a completed operation.</summary>
        public bool IsCompleted => _task == null || _task.IsCompleted;

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a successfully completed operation.</summary>
        public bool IsCompletedSuccessfully => _task == null || _task.Status == TaskStatus.RanToCompletion;

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a failed operation.</summary>
        public bool IsFaulted => _task != null && _task.IsFaulted;

        /// <summary>Gets whether the <see cref="ValueTask{TResult}"/> represents a canceled operation.</summary>
        public bool IsCanceled => _task != null && _task.IsCanceled;

        /// <summary>Gets the result.</summary>
        public TResult Result => _task == null ? _result : _task.GetAwaiter().GetResult();

        /// <summary>Gets an awaiter for this value.</summary>
        public ValueTaskAwaiter<TResult> GetAwaiter() => new ValueTaskAwaiter<TResult>(this);

        /// <summary>Configures an awaiter for this value.</summary>
        /// <param name="continueOnCapturedContext">
        /// true to attempt to marshal the continuation back to the captured context; otherwise, false.
        /// </param>
        public ConfiguredValueTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredValueTaskAwaitable<TResult>(this, continueOnCapturedContext: continueOnCapturedContext);

        /// <summary>Gets a string-representation of this <see cref="ValueTask{TResult}"/>.</summary>
        public override string ToString()
        {
            if (_task != null)
            {
                return _task.Status == TaskStatus.RanToCompletion && _task.Result != null ?
                    _task.Result.ToString() :
                    string.Empty;
            }
            else
            {
                return _result != null ?
                    _result.ToString() :
                    string.Empty;
            }
        }

        // TODO: Remove CreateAsyncMethodBuilder once the C# compiler relies on the AsyncBuilder attribute.

        /// <summary>Creates a method builder for use with an async method.</summary>
        /// <returns>The created builder.</returns>
        [EditorBrowsable(EditorBrowsableState.Never)] // intended only for compiler consumption
        public static AsyncValueTaskMethodBuilder<TResult> CreateAsyncMethodBuilder() => AsyncValueTaskMethodBuilder<TResult>.Create();
    }
}
