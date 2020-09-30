// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents something that configures the <typeparamref name="TOptions"/> type.
    /// Note: These are run after all <see cref="IConfigureOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">Options type being configured.</typeparam>
    public interface IPostConfigureOptions<in TOptions> where TOptions : class
    {
        /// <summary>
        /// Invoked to configure a <typeparamref name="TOptions"/> instance.
        /// </summary>
        /// <param name="name">The name of the options instance being configured.</param>
        /// <param name="options">The options instance to configured.</param>
        void PostConfigure(string name, TOptions options);
    }
}
