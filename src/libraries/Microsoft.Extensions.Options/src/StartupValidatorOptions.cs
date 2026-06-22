// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Options
{
    internal sealed class StartupValidatorOptions
    {
        // Maps each pair of a) options type and b) options name to a method that forces its evaluation, e.g. IOptionsMonitor<TOptions>.Get(name)
        public Dictionary<(Type, string), Action> _validators { get; } = new Dictionary<(Type, string), Action>();

        // Maps each pair of a) options type and b) options name to an async method that forces evaluation and runs async validators
        public Dictionary<(Type, string), Func<CancellationToken, Task>> _asyncValidators { get; } = new Dictionary<(Type, string), Func<CancellationToken, Task>>();
    }
}
