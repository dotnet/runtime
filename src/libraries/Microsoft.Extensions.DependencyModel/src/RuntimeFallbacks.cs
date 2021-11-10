// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeFallbacks
    {
        public string Runtime { get; set; }
        public IReadOnlyList<string?> Fallbacks { get; set; }

        public RuntimeFallbacks(string runtime, params string?[] fallbacks) : this(runtime, (IEnumerable<string?>)fallbacks) { }
        public RuntimeFallbacks(string runtime, IEnumerable<string?> fallbacks)
        {
            if (string.IsNullOrEmpty(runtime))
            {
                throw new ArgumentException(null, nameof(runtime));
            }
            if (fallbacks == null)
            {
                throw new ArgumentNullException(nameof(fallbacks));
            }
            Runtime = runtime;
            Fallbacks = fallbacks.ToArray();
        }
    }
}
