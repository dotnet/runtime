// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Options
{
    internal sealed class NamedValidateOptionsFilter<TOptions, TInner> : IValidateOptions<TOptions>
        where TOptions : class
        where TInner : IValidateOptions<TOptions>
    {
        private readonly string _name;
        private readonly TInner _inner;

        internal NamedValidateOptionsFilter(string name, TInner inner)
        {
            ArgumentNullException.ThrowIfNull(inner);

            _name = name;
            _inner = inner;
        }

        public ValidateOptionsResult Validate(string? name, TOptions options)
        {
            if (name is null || name == _name)
            {
                return _inner.Validate(name, options);
            }

            // ignored if not validating this instance
            return ValidateOptionsResult.Skip;
        }
    }
}
