// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    internal interface IOptionsValidatorNameMetadata
    {
        string? Name { get; }
    }

    internal sealed class NamedAsyncValidateOptionsFilter<TOptions> :
        IAsyncValidateOptions<TOptions>,
        IOptionsValidatorNameMetadata
        where TOptions : class
    {
        private readonly string _name;
        private readonly IAsyncValidateOptions<TOptions> _inner;

        internal NamedAsyncValidateOptionsFilter(string name, IAsyncValidateOptions<TOptions> inner)
        {
            ArgumentNullException.ThrowIfNull(inner);

            _name = name;
            _inner = inner;
        }

        string? IOptionsValidatorNameMetadata.Name => _name;

        public ValidateOptionsResult Validate(string? name, TOptions options) =>
            name is null || name == _name
                ? _inner.Validate(name, options)
                : ValidateOptionsResult.Skip;

        public Task<ValidateOptionsResult> ValidateAsync(
            string? name,
            TOptions options,
            CancellationToken cancellationToken = default) =>
            name is null || name == _name
                ? _inner.ValidateAsync(name, options, cancellationToken)
                : Task.FromResult(ValidateOptionsResult.Skip);
    }
}
