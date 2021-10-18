// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.PortableExecutable;
using ILTrim;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmerDriver
    {
        public void Trim (TrimmerOptions options)
        {
            TrimmerSettings settings = new TrimmerSettings(
                LibraryMode: options.IsLibraryMode,
                FeatureSwitches: options.FeatureSwitches);
            Trimmer.TrimAssembly(
                options.InputPath,
                options.AdditionalLinkAssemblies,
                options.OutputDirectory,
                options.ReferencePaths,
                settings);
        }
    }
}
