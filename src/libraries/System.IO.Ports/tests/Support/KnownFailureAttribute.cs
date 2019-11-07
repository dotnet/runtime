// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit.Sdk;
using Xunit.Abstractions;

namespace Legacy.Support
{
    [TraitDiscoverer("Legacy.Support.KnownFailureDiscoverer", "System.IO.Ports.Tests")]
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    class KnownFailureAttribute : Attribute, ITraitAttribute
    {
        public KnownFailureAttribute() { }
    }

    public class KnownFailureDiscoverer : ITraitDiscoverer
    {
        public IEnumerable<KeyValuePair<string, string>> GetTraits(IAttributeInfo traitAttribute)
        {
            yield return new KeyValuePair<string, string>("KnownFailure", "true");
        }
    }
}
