// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Represents something that initialize the <typeparamref name="TOptions"/> type.
    /// </summary>
    /// <typeparam name="TOptions">Options type being initialized.</typeparam>
    /// <remarks>
    /// This is run before all <see cref="IConfigureOptions{TOptions}"/>.
    /// </remarks>
    public interface IInitializationOptions<out TOptions> where TOptions : class
    {
        /// <summary>
        /// Creates a new instance of type <typeparamref name="TOptions"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance to create.</param>
        /// <returns>The created <typeparamref name="TOptions"/> instance.</returns>
        TOptions Initialize(string name);
    }
}
