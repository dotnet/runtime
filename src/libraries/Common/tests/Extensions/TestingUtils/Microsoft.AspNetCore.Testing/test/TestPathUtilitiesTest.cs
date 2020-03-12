﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Xunit;

namespace Microsoft.AspNetCore.Testing
{
    public class TestPathUtilitiesTest
    {
        // Entire test pending removal - see https://github.com/dotnet/extensions/issues/1697
#pragma warning disable 0618

        [Fact]
        public void GetSolutionRootDirectory_ResolvesSolutionRoot()
        {
            // Directory.GetCurrentDirectory() gives:
            // Testing\test\Microsoft.AspNetCore.Testing.Tests\bin\Debug\netcoreapp2.0
            // Testing\test\Microsoft.AspNetCore.Testing.Tests\bin\Debug\net461
            // Testing\test\Microsoft.AspNetCore.Testing.Tests\bin\Debug\net46
            var expectedPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "..", "..", ".."));

            Assert.Equal(expectedPath, TestPathUtilities.GetSolutionRootDirectory("Extensions"));
        }

        [Fact]
        public void GetSolutionRootDirectory_Throws_IfNotFound()
        {
            var exception = Assert.Throws<Exception>(() => TestPathUtilities.GetSolutionRootDirectory("NotTesting"));
            Assert.Equal($"Solution file NotTesting.sln could not be found in {AppContext.BaseDirectory} or its parent directories.", exception.Message);
        }
#pragma warning restore 0618
    }
}
