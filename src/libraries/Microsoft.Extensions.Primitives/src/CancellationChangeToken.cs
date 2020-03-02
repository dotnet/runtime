// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;

namespace Microsoft.Extensions.Primitives
{
    /// <summary>
    /// A <see cref="IChangeToken"/> implementation using <see cref="CancellationToken"/>.
    /// </summary>
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
        public IDisposable RegisterChangeCallback(Action<object> callback, object state)
        {
#if NETCOREAPP
            try
            {
                return Token.UnsafeRegister(callback, state);
            }
            catch (ObjectDisposedException)
            {
                // Reset the flag so that we can indicate to future callers that this wouldn't work.
                ActiveChangeCallbacks = false;
            }
#else
            // Don't capture the current ExecutionContext and its AsyncLocals onto the token registration causing them to live forever
            var restoreFlow = false;
            if (!ExecutionContext.IsFlowSuppressed())
            {
                ExecutionContext.SuppressFlow();
                restoreFlow = true;
            }

            try
            {
                return Token.Register(callback, state);
            }
            catch (ObjectDisposedException)
            {
                // Reset the flag so that we can indicate to future callers that this wouldn't work.
                ActiveChangeCallbacks = false;
            }
            finally
            {
                // Restore the current ExecutionContext
                if (restoreFlow)
                {
                    ExecutionContext.RestoreFlow();
                }
            }
#endif
            return NullDisposable.Instance;
        }

        private class NullDisposable : IDisposable
        {
            public static readonly NullDisposable Instance = new NullDisposable();

            public void Dispose()
            {
            }
        }
    }
}
