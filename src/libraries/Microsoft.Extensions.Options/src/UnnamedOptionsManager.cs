// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.Options
{
    internal class UnnamedOptionsManager<[DynamicallyAccessedMembers(Options.DynamicallyAccessedMembers)] TOptions> :
        IOptions<TOptions>
        where TOptions : class
    {
        private readonly Lazy<TOptions> _lazy;

        public UnnamedOptionsManager(IOptionsFactory<TOptions> factory)
        {
            _lazy = new Lazy<TOptions>(() => factory.Create(Options.DefaultName));
        }

        /// <summary>
        /// The default configured <typeparamref name="TOptions"/> instance, equivalent to Get(Options.DefaultName).
        /// </summary>
        public TOptions Value => _lazy.Value;
    }
}
