// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;

namespace System.Diagnostics.SymbolStore.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/39223", TestPlatforms.Browser)]
    public class StackTraceSymbolsTests
    {
        [Fact]
        public void StackTraceSymbolsDoNotLockFile()
        {
            var asmPath = typeof(StackTraceSymbolsTests).Assembly.Location;
            var pdbPath = Path.ChangeExtension(asmPath, ".pdb");

            Assert.True(File.Exists(pdbPath));
            new StackTrace(true).GetFrames();
            File.Move(pdbPath, pdbPath);
        }
    }
}
