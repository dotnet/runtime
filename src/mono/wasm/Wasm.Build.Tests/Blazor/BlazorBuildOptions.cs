// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


#nullable enable

// [assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly)]

namespace Wasm.Build.Tests
{
    public record BlazorBuildOptions
    (
        string Id,
        string Config,
        NativeFilesType ExpectedFileType,
        string TargetFramework = BuildTestBase.DefaultTargetFrameworkForBlazor,
        bool WarnAsError = true,
        bool ExpectRelinkDirWhenPublishing = false,
        bool ExpectFingerprintOnDotnetJs = false
    );
}
