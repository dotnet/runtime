// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationProviderMemoryTest : ConfigurationProviderTestBase
    {
        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(
            TestSection testConfig)
            => LoadUsingMemoryProvider(testConfig);
    }
}
