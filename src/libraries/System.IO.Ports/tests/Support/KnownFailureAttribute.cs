// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit.v3;

namespace Legacy.Support
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    class KnownFailureAttribute : Attribute, ITraitAttribute
    {
        public KnownFailureAttribute() { }

        public IReadOnlyCollection<KeyValuePair<string, string>> GetTraits() =>
            [new("KnownFailure", "true")];
    }
}
