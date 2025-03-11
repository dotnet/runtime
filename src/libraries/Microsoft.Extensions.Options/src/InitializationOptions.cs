// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Implements <see cref="IInitializationOptions{TOptions}"/>.
    /// </summary>
    /// <typeparam name="TOptions">The options type.</typeparam>
    public class InitializationOptions<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IInitializationOptions<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Creates a new instance of type <typeparamref name="TOptions"/>.
        /// </summary>
        /// <param name="name">The name of the <typeparamref name="TOptions"/> instance to create.</param>
        /// <returns>The created <typeparamref name="TOptions"/> instance.</returns>
        /// <exception cref="MissingMethodException">The <typeparamref name="TOptions"/> does not have a public parameterless constructor or <typeparamref name="TOptions"/> is <see langword="abstract"/>.</exception>
        public TOptions Initialize(string name)
        {
            return Activator.CreateInstance<TOptions>();
        }
    }
}
