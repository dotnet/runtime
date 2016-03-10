// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Extensions.DependencyModel
{
    public class RuntimeFallbacks
    {
        public string Runtime { get; set; }
        public IEnumerable<string> Fallbacks { get; set; }

        public RuntimeFallbacks(string runtime, IEnumerable<string> fallbacks)
        {
            Runtime = runtime;
            Fallbacks = fallbacks;
        }
    }
}