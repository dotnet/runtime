// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.AspNetCore.Testing;
using Xunit;

[assembly: Repeat(1)]
[assembly: AssemblyFixture(typeof(TestAssemblyFixture))]
[assembly: TestFramework("Microsoft.AspNetCore.Testing.AspNetTestFramework", "Microsoft.AspNetCore.Testing")]
