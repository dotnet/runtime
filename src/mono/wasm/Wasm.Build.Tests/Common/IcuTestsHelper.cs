// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public static class IcuTestsHelper
{
    // custom file contains only locales "cy-GB", "is-IS", "bs-BA", "lb-LU" and fallback locale: "en-US":
    public static string CustomIcuPath = Path.Combine(BuildEnvironment.TestAssetsPath, "icudt_custom.dat");
}