// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

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
