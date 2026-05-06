// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Http
{
    /// <summary>Provides utilities related to cancellation.</summary>
    internal static class CancellationHelper
    {
        /// <summary>The default message used by <see cref="OperationCanceledException"/>.</summary>
        private static readonly string s_cancellationMessage = new OperationCanceledException().Message; // use same message as the default ctor

        /// <summary>Determines whether to wrap an <see cref="Exception"/> in a cancellation exception.</summary>
        /// <param name="exception">The exception.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that may have triggered the exception.</param>
        /// <returns>true if the exception should be wrapped; otherwise, false.</returns>
        internal static bool ShouldWrapInOperationCanceledException(Exception exception, CancellationToken cancellationToken) =>
            !(exception is OperationCanceledException) && cancellationToken.IsCancellationRequested;

        /// <summary>Creates a cancellation exception.</summary>
        /// <param name="innerException">The inner exception to wrap. May be null.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that triggered the cancellation.</param>
        /// <returns>The cancellation exception.</returns>
        internal static Exception CreateOperationCanceledException(Exception? innerException, CancellationToken cancellationToken) =>
            new TaskCanceledException(s_cancellationMessage, innerException, cancellationToken); // TCE for compatibility with other handlers that use TaskCompletionSource.TrySetCanceled()

        /// <summary>Throws a cancellation exception.</summary>
        /// <param name="innerException">The inner exception to wrap. May be null.</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> that triggered the cancellation.</param>
        private static void ThrowOperationCanceledException(Exception? innerException, CancellationToken cancellationToken) =>
            throw CreateOperationCanceledException(innerException, cancellationToken);

        /// <summary>Throws a cancellation exception if cancellation has been requested via <paramref name="cancellationToken"/>.</summary>
        /// <param name="cancellationToken">The token to check for a cancellation request.</param>
        internal static void ThrowIfCancellationRequested(CancellationToken cancellationToken)
        {
            ThrowIfCancellationRequested(innerException: null, cancellationToken);
        }

        /// <summary>Throws a cancellation exception if cancellation has been requested via <paramref name="cancellationToken"/>.</summary>
        /// <param name="innerException">The inner exception to wrap. May be null.</param>
        /// <param name="cancellationToken">The token to check for a cancellation request.</param>
        internal static void ThrowIfCancellationRequested(Exception? innerException, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                ThrowOperationCanceledException(innerException, cancellationToken);
            }
        }
    }
}
