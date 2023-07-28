// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Internal;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// A <see cref="IChangeToken"/> implementation using <see cref="CancellationToken"/>.
    /// </summary>
    [DebuggerDisplay("HasChanged = {HasChanged}")]
    public class CancellationChangeToken : IChangeToken
    {
        /// <summary>
        /// Initializes a new instance of <see cref="CancellationChangeToken"/>.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/>.</param>
        public CancellationChangeToken(CancellationToken cancellationToken)
        {
            Token = cancellationToken;
        }

        /// <inheritdoc />
        public bool ActiveChangeCallbacks { get; private set; } = true;

        /// <inheritdoc />
        public bool HasChanged => Token.IsCancellationRequested;

        private CancellationToken Token { get; }

        /// <inheritdoc />
        public IDisposable RegisterChangeCallback(Action<object?> callback, object? state)
        {
            return ChangeCallbackRegistrar.UnsafeRegisterChangeCallback(
                callback,
                state,
                Token,
                static s => s.ActiveChangeCallbacks = false, // Reset the flag to indicate to future callers that this wouldn't work.
                this);
        }
    }
}
