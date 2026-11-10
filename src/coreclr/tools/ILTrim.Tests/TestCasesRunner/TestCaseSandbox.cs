// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public partial class TestCaseSandbox
    {
        private const string _linkerAssemblyPath = "";

        private static partial NPath GetArtifactsTestPath()
        {
            string artifacts = (string)AppContext.GetData("Mono.Linker.Tests.ArtifactsBinDir")!;
            string tests = Path.Combine(artifacts, "ILLink.testcases");
            return new NPath(tests);
        }
    }
}
