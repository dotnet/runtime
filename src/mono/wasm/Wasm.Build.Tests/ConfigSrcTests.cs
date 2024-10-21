// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class ConfigSrcTests : WasmTemplateTestsBase
{
    public ConfigSrcTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext) : base(output, buildContext)
    { }

    // INFO FOR REVIWER:
    // This class can be deleted and will be after your approval. Justification:
    // It is testing the --config-src argument, which was supposed to be passed to test-main.js
    // but does not make sense in the current form of testing where we are using "dotnet new" templates
}
