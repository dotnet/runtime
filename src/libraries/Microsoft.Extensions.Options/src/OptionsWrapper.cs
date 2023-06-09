// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    /// <summary>
    /// <see cref="IOptions{TOptions}"/> wrapper that returns the options instance.
    /// </summary>
    /// <typeparam name="TOptions">Options type.</typeparam>
    public class OptionsWrapper<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>
        where TOptions : class
    {
        /// <summary>
        /// Initializes the wrapper with the options instance to return.
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

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        [Obsolete("This method is retained only for compatibility.", error: true)]
        public void Add(string name, TOptions options)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        /// <param name="name">This parameter is ignored.</param>
        /// <returns>The <see cref="Value"/>.</returns>
        [Obsolete("This method is retained only for compatibility.", error: true)]
        public TOptions Get(string name)
        {
            return Value;
        }

        /// <summary>
        /// This method is retained only for compatibility.
        /// </summary>
        [Obsolete("This method is retained only for compatibility.", error: true)]
        public bool Remove(string name)
        {
            throw new NotImplementedException();
        }
    }
}
