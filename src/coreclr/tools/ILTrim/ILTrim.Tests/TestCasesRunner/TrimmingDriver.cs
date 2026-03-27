// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILCompiler;

namespace Mono.Linker.Tests.TestCasesRunner
{
    public class TrimmingDriver
    {
        public TrimmingResults Trim(TrimmerOptions options, TrimmingCustomizations? customizations, TrimmingTestLogger logger)
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
            return new TrimmingResults(0);
        }
    }
}
