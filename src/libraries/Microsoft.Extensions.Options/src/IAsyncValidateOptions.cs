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
    /// <remarks>
    /// Implementations must be registered through <see cref="OptionsBuilder{TOptions}.Validate{TValidateOptions}()"/>
    /// or as <see cref="IValidateOptions{TOptions}"/>, not directly as <see cref="IAsyncValidateOptions{TOptions}"/>.
    /// </remarks>
    public interface IAsyncValidateOptions<TOptions> : IValidateOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Asynchronously validates a specified named options instance (or all if <paramref name="name"/> is <see langword="null"/>).
        /// </summary>
        /// <param name="name">The name of the options instance being validated.</param>
        /// <param name="options">The options instance.</param>
        /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
        /// <returns>The <see cref="ValidateOptionsResult"/> result.</returns>
        /// <remarks>
        /// This method must perform all applicable checks for this validator, including checks that can complete
        /// synchronously. The built-in asynchronous options creation path invokes this method instead of
        /// invoking <see cref="IValidateOptions{TOptions}.Validate"/> on the same validator. Share common checks
        /// through a helper when needed.
        /// Synchronous options creation invokes the inherited <see cref="IValidateOptions{TOptions}.Validate"/> method.
        /// Implement that method with equivalent synchronous validation when possible. If a required rule applies but
        /// cannot be evaluated synchronously, return a failed <see cref="ValidateOptionsResult"/> indicating that
        /// synchronous validation is unsupported; do not return <see cref="ValidateOptionsResult.Success"/>.
        /// </remarks>
        Task<ValidateOptionsResult> ValidateAsync(string? name, TOptions options, CancellationToken cancellationToken = default);
    }
}
