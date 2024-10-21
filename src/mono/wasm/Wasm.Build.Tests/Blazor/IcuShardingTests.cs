// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

// these tests only check if correct ICU files got copied
public class IcuShardingTests : BlazorWasmTestBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) {}

    // FOR REVIEWER:
    // These tests are not specific for Blazor, they are testing ICU mechanisms that are commond for all apps using browser SDK
    // From this reason, tests in this file are duplicates of IcuShardingTests.cs and IcuShardingTests2.cs in mono/wasm/Wasm.Build.Tests
    // This file will get removed after approval of the PR
}
