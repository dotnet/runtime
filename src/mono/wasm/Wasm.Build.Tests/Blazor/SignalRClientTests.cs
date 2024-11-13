// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class SignalRClientTests : SignalRTestsBase
{
    public SignalRClientTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/100445")] // to be fixed by: "https://github.com/dotnet/aspnetcore/issues/54365"
    [InlineData("Debug", "LongPolling")]
    [InlineData("Release", "LongPolling")]
    [InlineData("Debug", "WebSockets")]
    [InlineData("Release", "WebSockets")]
    public async Task SignalRPassMessageBlazor(string config, string transport) =>
        await SignalRPassMessage("blazorclient", config, transport);
}

