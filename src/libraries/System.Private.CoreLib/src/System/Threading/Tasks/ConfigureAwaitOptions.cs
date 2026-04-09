// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading.Tasks
{
    /// <summary>Options to control behavior when awaiting.</summary>
    [Flags]
    public enum ConfigureAwaitOptions
    {
        /// <summary>No options specified.</summary>
        /// <remarks>
        /// <see cref="Task.ConfigureAwait(ConfigureAwaitOptions)"/> with a <see cref="None"/> argument behaves
        /// identically to using <see cref="Task.ConfigureAwait(bool)"/> with a <see langword="false"/> argument.
        /// </remarks>
        None = 0x0,

        /// <summary>
        /// Attempt to marshal the continuation back to the original <see cref="SynchronizationContext"/> or
        /// <see cref="TaskScheduler"/> present on the originating thread at the time of the await.
        /// </summary>
        /// <remarks>
        /// If there is no such context/scheduler, or if this option is not specified, the thread on
        /// which the continuation is invoked is unspecified and left up to the determination of the system.
        /// <see cref="Task.ConfigureAwait(ConfigureAwaitOptions)"/> with a <see cref="ContinueOnCapturedContext"/> argument
        /// behaves identically to using <see cref="Task.ConfigureAwait(bool)"/> with a <see langword="true"/> argument.
        /// </remarks>
        ContinueOnCapturedContext = 0x1,

        /// <summary>
        /// Avoids throwing an exception at the completion of awaiting a <see cref="Task"/> that ends
        /// in the <see cref="TaskStatus.Faulted"/> or <see cref="TaskStatus.Canceled"/> state.
        /// </summary>
        /// <remarks>
        /// This option is supported only for <see cref="Task.ConfigureAwait(ConfigureAwaitOptions)"/>,
        /// not <see cref="Task{TResult}.ConfigureAwait(ConfigureAwaitOptions)"/>, as for a <see cref="Task{TResult}"/> the
        /// operation could end up returning an incorrect and/or invalid result. To use with a <see cref="Task{TResult}"/>,
        /// cast to the base <see cref="Task"/> type in order to use its <see cref="Task.ConfigureAwait(ConfigureAwaitOptions)"/>.
        /// </remarks>
        SuppressThrowing = 0x2,

        /// <summary>
        /// Forces an await on an already completed <see cref="Task"/> to behave as if the <see cref="Task"/>
        /// wasn't yet completed, such that the current asynchronous method will be forced to yield its execution.
        /// </summary>
        ForceYielding = 0x4,
    }
}
