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
        /// Calls all registered <see cref="IAsyncValidateOptions{TOptions}"/> validators.
        /// </summary>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <exception cref="OptionsValidationException">
        /// One or more validators return failed <see cref="ValidateOptionsResult"/> when validating.
        /// </exception>
        Task ValidateAsync(CancellationToken cancellationToken = default);
    }
}
