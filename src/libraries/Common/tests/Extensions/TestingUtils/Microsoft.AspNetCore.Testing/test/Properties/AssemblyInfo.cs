// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.AspNetCore.Testing;
using Xunit;

[assembly: Repeat(1)]
[assembly: AssemblyFixture(typeof(TestAssemblyFixture))]
[assembly: TestFramework("Microsoft.AspNetCore.Testing.AspNetTestFramework", "Microsoft.AspNetCore.Testing")]
