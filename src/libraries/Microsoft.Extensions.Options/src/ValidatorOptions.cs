// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.DependencyInjection
{
    internal class ValidatorOptions
    {
        // The key maps to the TOptions in IOptions<TOptions> and the value is a method
        // that accesses the IOptions<TOptions>.Value property in order to force evaluation of
        // the options type.
        public ConcurrentDictionary<Type, Action> Validators { get; } = new ConcurrentDictionary<Type, Action>();
    }
}