// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.Configuration
{
    /// <summary>
    /// Implements <see cref="IChangeToken"/>
    /// </summary>
    public class ConfigurationReloadToken : IChangeToken
    {
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        /// <summary>
        /// Indicates if this token will proactively raise callbacks. Callbacks are still guaranteed to be invoked, eventually.
        /// </summary>
        /// <returns>True if the token will proactively raise callbacks.</returns>
        public bool ActiveChangeCallbacks { get; private set; } = true;

        /// <summary>
        /// Gets a value that indicates if a change has occurred.
        /// </summary>
        /// <returns>True if a change has occurred.</returns>
        public bool HasChanged => _cts.IsCancellationRequested;

        /// <summary>
        /// Registers for a callback that will be invoked when the entry has changed. <see cref="IChangeToken.HasChanged"/>
        /// MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="state">State to be passed into the callback.</param>
        /// <returns>The <see cref="CancellationToken"/> registration.</returns>
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return ChangeCallbackRegistrar.UnsafeRegisterChangeCallback(
                callback,
                state,
                _cts.Token,
                static s => s.ActiveChangeCallbacks = false, // Reset the flag to indicate to future callers that this wouldn't work.
                this);
        }

        /// <summary>
        /// Used to trigger the change token when a reload occurs.
        /// </summary>
        public void OnReload() => _cts.Cancel();
    }
}
