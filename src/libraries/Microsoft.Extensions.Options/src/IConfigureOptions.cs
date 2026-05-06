// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents something that configures the <typeparamref name="TOptions"/> type.
    /// </summary>
    /// <typeparam name="TOptions">The options type being configured.</typeparam>
    /// <remarks>
    /// These are run before all <see cref="IPostConfigureOptions{TOptions}"/>.
    /// </remarks>
    public interface IConfigureOptions<in TOptions> where TOptions : class
    {
        /// <summary>
        /// Configures a <typeparamref name="TOptions"/> instance.
        /// </summary>
        /// <param name="options">The options instance to configure.</param>
        void Configure(TOptions options);
    }
}
