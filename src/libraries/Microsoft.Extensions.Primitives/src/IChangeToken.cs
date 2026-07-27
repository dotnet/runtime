// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// Propagates notifications that a change has occurred.
    /// </summary>
    public interface IChangeToken
    {
        /// <summary>
        /// Gets a value that indicates if a change has occurred.
        /// </summary>
        bool HasChanged { get; }

        /// <summary>
        /// Gets a value that indicates whether this token will proactively raise callbacks. If <see langword="false" />, the token consumer must
        /// poll <see cref="HasChanged" /> to detect changes.
        /// </summary>
        /// <remarks>
        /// A <see langword="true" /> value does not guarantee that callbacks will be raised for all changes.
        /// Consumers should also check <see cref="HasChanged" /> when complete accuracy is required.
        /// </remarks>
        bool ActiveChangeCallbacks { get; }

        /// <summary>
        /// Registers for a callback that will be invoked when the entry has changed.
        /// <see cref="HasChanged"/> MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback">The <see cref="Action{Object}"/> to invoke.</param>
        /// <param name="state">State to be passed into the callback.</param>
        /// <returns>An <see cref="IDisposable"/> that is used to unregister the callback.</returns>
        IDisposable RegisterChangeCallback(Action<object?> callback, object? state);
    }
}
