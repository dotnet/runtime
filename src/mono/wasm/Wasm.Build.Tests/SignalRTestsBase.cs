// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.RegularExpressions;
using Xunit.Abstractions;
using Wasm.Build.Tests.TestAppScenarios;
using Xunit;
#nullable enable

namespace Wasm.Build.Tests;

public class SignalRTestsBase : AppTestBase
{
    public SignalRTestsBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    protected string GetThreadOfAction(string testOutput, string pattern, string actionDescription)
    {
        Match match = Regex.Match(testOutput, pattern);
        Assert.True(match.Success, $"Expected to find a log that {actionDescription}. TestOutput: {testOutput}.");
        return match.Groups[1].Value ?? "";
    }
}
