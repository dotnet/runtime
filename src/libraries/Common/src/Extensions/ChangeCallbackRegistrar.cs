// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Internal
{
    internal static class ChangeCallbackRegistrar
    {
        /// <summary>
        /// Registers for a callback that will be invoked when the entry has changed. <see cref="Primitives.IChangeToken.HasChanged"/>
        /// MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="state">State to be passed into the callback.</param>
        /// <param name="token">The <see cref="CancellationToken"/> to invoke the callback with.</param>
        /// <param name="onFailure">The action to execute when an <see cref="ObjectDisposedException"/> is thrown. Should be used to set the IChangeToken's ActiveChangeCallbacks property to false.</param>
        /// <param name="onFailureState">The state to be passed into the <paramref name="onFailure"/> action.</param>
        /// <returns>The <see cref="CancellationToken"/> registration.</returns>
        internal static IDisposable UnsafeRegisterChangeCallback<T>(Action<object?> callback, object? state, CancellationToken token, Action<T> onFailure, T onFailureState)
        {
#if NET || NETSTANDARD2_1
            try
            {
                return token.UnsafeRegister(callback, state);
            }
            catch (ObjectDisposedException)
            {
                onFailure(onFailureState);
            }
#else
            // Don't capture the current ExecutionContext and its AsyncLocals onto the token registration causing them to live forever
            bool restoreFlow = false;
            if (!ExecutionContext.IsFlowSuppressed())
            {
                ExecutionContext.SuppressFlow();
                restoreFlow = true;
            }

            try
            {
                return token.Register(callback, state);
            }
            catch (ObjectDisposedException)
            {
                onFailure(onFailureState);
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
            return EmptyDisposable.Instance;
        }
    }
}
