// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used by hosts to asynchronously validate options during startup.
    /// </summary>
    public interface IAsyncStartupValidator
    {
        /// <summary>
        /// Asynchronously validates each options type and name configured for startup validation. When the built-in
        /// options factory creates a value, it invokes each registered <see cref="IValidateOptions{TOptions}"/> once,
        /// preferring <see cref="IAsyncValidateOptions{TOptions}.ValidateAsync"/> when available.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
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
