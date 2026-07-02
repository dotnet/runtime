// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Asynchronously validates options.
    /// </summary>
    /// <remarks>
    /// Singleton implementations may be used concurrently by options services, such as <see cref="IOptionsMonitor{TOptions}"/>.
    /// Synchronous <see cref="IOptionsMonitor{TOptions}"/> access may wait for asynchronous validation when no previously
    /// validated value is available or when a configuration change is being validated.
    /// The default options services do not silently ignore asynchronous-only validators during synchronous options creation.
    /// Synchronous factory and snapshot access throws if an applicable asynchronous validator does not also implement
    /// <see cref="IValidateOptions{TOptions}"/>. <see cref="IOptions{TOptions}"/> can return a previously asynchronously
    /// validated default instance, such as one validated during startup or by <see cref="IOptionsMonitor{TOptions}"/>;
    /// otherwise synchronous access throws until asynchronous validation has established a valid value.
    /// Synchronous guard checks can determine name applicability for validators registered by <see cref="OptionsBuilder{TOptions}"/>;
    /// custom asynchronous-only validators are conservatively treated as applicable to every name.
    /// </remarks>
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
