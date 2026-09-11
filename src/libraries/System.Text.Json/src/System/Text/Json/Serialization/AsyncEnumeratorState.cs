// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Text.Json
{
    /// <summary>
    /// Tracks the state of an async enumerator within a <see cref="WriteStackFrame"/>.
    /// </summary>
    internal enum AsyncEnumeratorState : byte
    {
        /// <summary>
        /// No async enumerator is active; the enumerator has not been created yet.
        /// </summary>
        None,

        /// <summary>
        /// The async enumerator has been created and is actively being iterated.
        /// </summary>
        Enumerating,

        /// <summary>
        /// The converter has been suspended due to a pending MoveNextAsync() task.
        /// </summary>
        PendingMoveNext,

        /// <summary>
        /// The converter has been suspended due to a pending DisposeAsync() task.
        /// </summary>
        PendingDisposal,
    }
}
