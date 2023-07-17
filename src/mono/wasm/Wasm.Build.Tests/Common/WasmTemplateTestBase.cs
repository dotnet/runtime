// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmTemplateTestBase : BuildTestBase
{
    protected readonly WasmSdkBasedProjectProvider _provider;
    protected WasmTemplateTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext, WasmSdkBasedProjectProvider? projectProvider = null)
                : base(projectProvider ?? new WasmSdkBasedProjectProvider(output), output, buildContext)
    {
        _provider = GetProvider<WasmSdkBasedProjectProvider>();
    }
}
