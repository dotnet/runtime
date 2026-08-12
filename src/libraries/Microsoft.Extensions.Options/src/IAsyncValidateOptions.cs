// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Asynchronously validates options.
    /// </summary>
    /// <typeparam name="TOptions">The options type to validate.</typeparam>
    public interface IAsyncValidateOptions<in TOptions> where TOptions : class
    {
        /// <summary>
        /// Asynchronously validates a specified named options instance (or all if <paramref name="name"/> is <see langword="null"/>).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default);
    }
}
