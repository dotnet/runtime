// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;

namespace Microsoft.Extensions.Configuration.Test
{
    public class ConfigurationProviderMemoryTest : ConfigurationProviderTestBase
    {
        public override void Null_values_are_included_in_the_config()
        {
            AssertConfig(BuildConfigRoot(LoadThroughProvider(TestSection.NullsTestConfig)), expectNulls: true);
        }

        protected override (IConfigurationProvider Provider, Action Initializer) LoadThroughProvider(
            TestSection testConfig)
            => LoadUsingMemoryProvider(testConfig);
    }
}
