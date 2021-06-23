// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    internal class FakeConfigurationProvider : MemoryConfigurationProvider, IConfigurationProvider
    {
        public FakeConfigurationProvider(MemoryConfigurationSource source)
            : base(source) { }

        public new void Set(string key, string value)
        {
            base.Set(key, value);
            OnReload();
        }
    }
}
