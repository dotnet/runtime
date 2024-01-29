// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting.Fakes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Xunit;

namespace Microsoft.Extensions.Hosting.Tests;

public class IHostApplicationBuilderTests
{
    [Fact]
    public void TestIHostApplicationBuilderCanBeUsedInExtensionMethod()
    {
        HostApplicationBuilder builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Development"
        });

        builder.VerifyBuilderWorks();

        using IHost host = builder.Build();

        // VerifyBuilderWorks should have configured a FakeServiceProviderFactory with the following State.
        FakeServiceCollection fakeServices = host.Services.GetRequiredService<FakeServiceCollection>();
        Assert.Equal("Hi!", fakeServices.State);
    }
}

internal static class HostBuilderExtensions
{
    public static void VerifyBuilderWorks(this IHostApplicationBuilder builder)
    {
        var propertyKey = typeof(HostBuilderExtensions);
        builder.Properties[propertyKey] = 3;
        Assert.Equal(3, builder.Properties[propertyKey]);

        Assert.Equal(1, builder.Configuration.GetChildren().Count());
        Assert.Equal(2, builder.Configuration.Sources.Count); // there's an empty source by default
        Assert.Equal("Development", builder.Configuration[HostDefaults.EnvironmentKey]);

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>
        {
            ["Key1"] = "value1"
        });

        Assert.Equal(2, builder.Configuration.GetChildren().Count());
        Assert.Equal(3, builder.Configuration.Sources.Count);
        Assert.Equal("value1", builder.Configuration["Key1"]);
        Assert.Null(builder.Configuration["Key2"]);

        Assert.True(builder.Environment.IsDevelopment());
        Assert.NotNull(builder.Environment.ContentRootFileProvider);

        Assert.DoesNotContain(builder.Services, sd => sd.ImplementationType == typeof(ConsoleLoggerProvider));
        builder.Logging.AddConsole();
        Assert.Contains(builder.Services, sd => sd.ImplementationType == typeof(ConsoleLoggerProvider));

        builder.Services.AddSingleton(typeof(IHostApplicationBuilderTests));

        builder.ConfigureContainer(new FakeServiceProviderFactory(), container => container.State = "Hi!");
    }
}
