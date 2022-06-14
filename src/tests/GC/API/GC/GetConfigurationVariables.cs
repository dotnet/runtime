// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

namespace ConfigurationVariables
{
    public class Test_GetConfigurationVariables : IDisposable 
    {
        [Fact]
        public void CollectAllVariables()
        {
            var configurations = GC.GetConfigurationVariables();
            Assert.True(configurations != null, "Configuration is null.");
            Assert.True(configurations.Count >= 0, "Configuration count is <= 0.");
            foreach(var kvp in configurations)
            {
                Assert.True(kvp.Key != null, "The name of the configuration is null.");
                Assert.True(kvp.Value != null, $"The value of configuration: {kvp.Key} is null.");
            }
        }

        public void Dispose() {}
    }
}