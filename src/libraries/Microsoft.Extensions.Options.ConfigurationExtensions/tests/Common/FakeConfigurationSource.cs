// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Memory;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    internal class FakeConfigurationSource : MemoryConfigurationSource, IConfigurationSource
    {
        internal IConfigurationProvider Provider { get; private set; } = null!;

        public new IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            Provider = new FakeConfigurationProvider(this);
            return Provider;
        }
    }
}
