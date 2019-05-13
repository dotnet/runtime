// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading.Tasks.Sources
{
    /// <summary>
    /// Flags passed from <see cref="ValueTask"/> and <see cref="ValueTask{TResult}"/> to
    /// <see cref="IValueTaskSource.OnCompleted"/> and <see cref="IValueTaskSource{TResult}.OnCompleted"/>
    /// to control behavior.
    /// </summary>
    [Flags]
    public enum ValueTaskSourceOnCompletedFlags
    {
        /// <summary>
        /// No requirements are placed on how the continuation is invoked.
        /// </summary>
        None,
        /// <summary>
        /// Set if OnCompleted should capture the current scheduling context (e.g. SynchronizationContext)
        /// and use it when queueing the continuation for execution.  If this is not set, the implementation
        /// may choose to execute the continuation in an arbitrary location.
        /// </summary>
        UseSchedulingContext = 0x1,
        /// <summary>
        /// Set if OnCompleted should capture the current ExecutionContext and use it to run the continuation.
        /// </summary>
        FlowExecutionContext = 0x2,
    }

    /// <summary>Indicates the status of an <see cref="IValueTaskSource"/> or <see cref="IValueTaskSource{TResult}"/>.</summary>
    public enum ValueTaskSourceStatus
    {
        /// <summary>The operation has not yet completed.</summary>
        Pending = 0,
        /// <summary>The operation completed successfully.</summary>
        Succeeded = 1,
        /// <summary>The operation completed with an error.</summary>
        Faulted = 2,
        /// <summary>The operation completed due to cancellation.</summary>
        Canceled = 3
    }

    /// <summary>Represents an object that can be wrapped by a <see cref="ValueTask"/>.</summary>
    public interface IValueTaskSource
    {
        /// <summary>Gets the status of the current operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        ValueTaskSourceStatus GetStatus(short token);

        /// <summary>Schedules the continuation action for this <see cref="IValueTaskSource"/>.</summary>
        /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
        /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        /// <param name="flags">The flags describing the behavior of the continuation.</param>
        void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

        /// <summary>Gets the result of the <see cref="IValueTaskSource"/>.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        void GetResult(short token);
    }

    /// <summary>Represents an object that can be wrapped by a <see cref="ValueTask{TResult}"/>.</summary>
    /// <typeparam name="TResult">Specifies the type of data returned from the object.</typeparam>
    public interface IValueTaskSource<out TResult>
    {
        /// <summary>Gets the status of the current operation.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        ValueTaskSourceStatus GetStatus(short token);

        /// <summary>Schedules the continuation action for this <see cref="IValueTaskSource{TResult}"/>.</summary>
        /// <param name="continuation">The continuation to invoke when the operation has completed.</param>
        /// <param name="state">The state object to pass to <paramref name="continuation"/> when it's invoked.</param>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        /// <param name="flags">The flags describing the behavior of the continuation.</param>
        void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags); // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761

        /// <summary>Gets the result of the <see cref="IValueTaskSource{TResult}"/>.</summary>
        /// <param name="token">Opaque value that was provided to the <see cref="ValueTask"/>'s constructor.</param>
        TResult GetResult(short token);
    }
}
