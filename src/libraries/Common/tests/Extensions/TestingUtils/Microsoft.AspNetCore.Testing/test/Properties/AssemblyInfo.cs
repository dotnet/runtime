// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Testing;
using Xunit;

[assembly: Repeat(1)]
[assembly: AssemblyFixture(typeof(TestAssemblyFixture))]
[assembly: TestFramework("Microsoft.AspNetCore.Testing.AspNetTestFramework", "Microsoft.AspNetCore.Testing")]
