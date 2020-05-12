// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// Used to access the value of <typeparamref name="TOptions"/> for the lifetime of a request.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    public interface IOptionsSnapshot<out TOptions> : IOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Returns a configured <typeparamref name="TOptions"/> instance with the given name.
        /// </summary>
        TOptions Get(string name);
    }
}
