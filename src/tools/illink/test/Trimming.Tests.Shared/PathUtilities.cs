// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable

using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public static class PathUtilities
    {
        public static string TargetFramework => (string)AppContext.GetData("Mono.Linker.Tests.TargetFramework")!;

        public static string TargetFrameworkMoniker => (string)AppContext.GetData("Mono.Linker.Tests.TargetFrameworkMoniker")!;

        public static string TargetFrameworkMonikerDisplayName => (string)AppContext.GetData("Mono.Linker.Tests.TargetFrameworkMonikerDisplayName")!;

        public static string GetTestsSourceRootDirectory([CallerFilePath] string? thisFile = null) =>
            Path.GetFullPath((string)AppContext.GetData("Mono.Linker.Tests.LinkerTestDir")!);

        public static string GetTestAssemblyRoot(string assemblyName)
        {
            string artifactsBinDirectory = (string)AppContext.GetData("Mono.Linker.Tests.ArtifactsBinDir")!;
            string configuration = (string)AppContext.GetData("Mono.Linker.Tests.Configuration")!;

            return Path.GetFullPath(Path.Combine(artifactsBinDirectory, assemblyName, configuration, TargetFramework));
        }
    }
}
