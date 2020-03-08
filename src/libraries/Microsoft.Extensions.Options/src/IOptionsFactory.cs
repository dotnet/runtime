// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to create <typeparamref name="TOptions"/> instances.
    /// </summary>
    /// <typeparam name="TOptions">The type of options being requested.</typeparam>
    public interface IOptionsFactory<TOptions> where TOptions : class
    {
        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given name.
        /// </summary>
        TOptions Create(string name);
    }
}
