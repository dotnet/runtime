// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Internal
{
    internal static partial class ChangeCallbackRegistrar
    {
        /// <summary>
        /// Registers for a callback that will be invoked when the entry has changed. <see cref="Primitives.IChangeToken.HasChanged"/>
        /// MUST be set before the callback is invoked.
        /// </summary>
        /// <param name="callback">The callback to invoke.</param>
        /// <param name="state">State to be passed into the callback.</param>
        /// <param name="_cts"></param>
        /// <param name="onFailure"></param>
        /// <param name="onFailureState"></param>
        /// <returns>The <see cref="CancellationToken"/> registration.</returns>
        internal static IDisposable UnsafeRegisterChangeCallback<T>(Action<object?> callback, object? state, CancellationTokenSource _cts, Action<T> onFailure, T onFailureState)
        {
#if NETCOREAPP || NETSTANDARD2_1
            try
            {
                return _cts.Token.UnsafeRegister(callback, state);
            }
            catch (ObjectDisposedException)
            {
                // Reset the flag so that we can indicate to future callers that this wouldn't work.
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
                return _cts.Token.Register(callback, state);
            }
            catch (ObjectDisposedException)
            {
                // Reset the flag so that we can indicate to future callers that this wouldn't work.
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
