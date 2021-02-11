// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.Threading.Channels
{
    /// <summary>Shims the non-generic TaskCompletionSource for targets that don't provide it.</summary>
    internal sealed class TaskCompletionSource : TaskCompletionSource<VoidResult>
    {
        /// <summary>Creates a <see cref="TaskCompletionSource"/> with the specified options.</summary>
        /// <remarks>
        /// The <see cref="Tasks.Task"/> created by this instance and accessible through its <see cref="Task"/> property
        /// will be instantiated using the specified <paramref name="creationOptions"/>.
        /// </remarks>
        /// <param name="creationOptions">The options to use when creating the underlying <see cref="Tasks.Task"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The <paramref name="creationOptions"/> represent options invalid for use
        /// with a <see cref="TaskCompletionSource"/>.
        /// </exception>
        public TaskCompletionSource(TaskCreationOptions creationOptions) : base(creationOptions)
        {
        }

        /// <summary>
        /// Attempts to transition the underlying <see cref="Tasks.Task"/> into the <see cref="TaskStatus.RanToCompletion"/> state.
        /// </summary>
        /// <returns>True if the operation was successful; otherwise, false.</returns>
        /// <remarks>
        /// This operation will return false if the <see cref="Tasks.Task"/> is already in one of the three final states:
        /// <see cref="TaskStatus.RanToCompletion"/>,
        /// <see cref="TaskStatus.Faulted"/>, or
        /// <see cref="TaskStatus.Canceled"/>.
        /// </remarks>
        public bool TrySetResult() => TrySetResult(default);
    }
}
