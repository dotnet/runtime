// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Provides asynchronous options validation during host startup.
    /// </summary>
    /// <remarks>
    /// New implementations should be registered only as <see cref="IAsyncStartupValidator"/>. Do not additionally
    /// register a custom implementation as <see cref="IStartupValidator"/>; that interface is retained for
    /// compatibility with existing synchronous startup validators.
    /// </remarks>
    public interface IAsyncStartupValidator
    {
        /// <summary>
        /// Validates options asynchronously during host startup.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>A task that represents the asynchronous validation operation.</returns>
        /// <exception cref="OptionsValidationException">
        /// A single validator returns a failed <see cref="ValidateOptionsResult"/> when validating.
        /// </exception>
        /// <exception cref="System.AggregateException">
        /// Multiple option instances fail validation, each producing an
        /// <see cref="OptionsValidationException"/>.
        /// </exception>
        Task ValidateAsync(CancellationToken cancellationToken = default);
    }
}
