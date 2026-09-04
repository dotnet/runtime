// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to access the value of <typeparamref name="TOptions"/> for the lifetime of a request.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    /// <remarks>
    /// The default implementation, <see cref="OptionsManager{TOptions}"/>, has a separate options cache in each
    /// scope. Startup validation does not populate this cache, and the default implementation creates and validates
    /// options synchronously. Validators with a usable synchronous <see cref="IValidateOptions{TOptions}.Validate"/>
    /// implementation continue to work. Validators that require asynchronous validation fail through their
    /// synchronous validation result because the default implementation cannot execute or await
    /// <see cref="IAsyncValidateOptions{TOptions}.ValidateAsync"/>.
    /// </remarks>
    public interface IOptionsSnapshot<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] out TOptions> :
        IOptions<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given <paramref name="name"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance. If <see langword="null"/>, <see cref="Options.DefaultName"/>, which is the empty string, is used.</param>
        /// <returns>The <typeparamref name="TOptions"/> instance that matches the given <paramref name="name"/>.</returns>
        TOptions Get(string? name);
    }
}
