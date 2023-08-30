// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Microsoft.Extensions.Options.ConfigurationExtensions.Tests
{
    public partial class ConfigurationExtensionsTests
    {
        private static IConfiguration s_emptyConfig { get; } = new ConfigurationBuilder().Build();

        private static OptionsBuilder<FakeOptions> CreateOptionsBuilder()
        {
            var services = new ServiceCollection();
            return new OptionsBuilder<FakeOptions>(services, Options.DefaultName);
        }
    }
}
