// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
