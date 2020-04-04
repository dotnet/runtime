// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// <see cref="IOptions{TOptions}"/> wrapper that returns the options instance.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    public class OptionsWrapper<TOptions> : IOptions<TOptions> where TOptions : class
    {
        /// <summary>
        /// Intializes the wrapper with the options instance to return.
        /// </summary>
        /// <param name="options">The options instance to return.</param>
        public OptionsWrapper(TOptions options)
        {
            Value = options;
        }

        /// <summary>
        /// The options instance.
        /// </summary>
        public TOptions Value { get; }
    }
}
