// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyInjection
{
    internal sealed class ValidatorOptions
    {
        // Maps each pair of a) options type and b) options name to a method that forces its evaluation, e.g. IOptionsMonitor<TOptions>.Get(name)
        public IDictionary<(Type optionsType, string optionsName), Action> Validators { get; } = new Dictionary<(Type, string), Action>();
    }
}
