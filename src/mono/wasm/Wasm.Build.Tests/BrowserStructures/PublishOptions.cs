// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public record PublishOptions : MSBuildOptions
{
    public bool BuildOnlyAfterPublish { get; init; }
    public bool ExpectRelinkDirWhenPublishing { get; init; }

    public PublishOptions(
        bool IsPublish                                              = true,
        bool AOT                                                    = false,
        NativeFilesType ExpectedFileType                            = NativeFilesType.FromRuntimePack,
        string? TargetFramework                                     = null,
        GlobalizationMode GlobalizationMode                         = GlobalizationMode.Sharded,
        string CustomIcuFile                                        = "",
        bool UseCache                                               = true,
        bool ExpectSuccess                                          = true,
        bool AssertAppBundle                                        = true,
        string Label                                                = "",
        bool WarnAsError                                            = true,
        RuntimeVariant RuntimeType                                  = RuntimeVariant.SingleThreaded,
        IDictionary<string, string>? ExtraBuildEnvironmentVariables = null,
        string BootConfigFileName                                   = "dotnet.boot.js",
        string NonDefaultFrameworkDir                               = "",
        string ExtraMSBuildArgs                                     = "",
        bool BuildOnlyAfterPublish                                  = true,
        bool ExpectRelinkDirWhenPublishing                          = false,
        bool WasmPerfTracing                                     = false
    ) : base(
        IsPublish,
        TargetFramework ?? BuildTestBase.DefaultTargetFramework,
        AOT,
        ExpectedFileType,
        GlobalizationMode,
        CustomIcuFile,
        UseCache,
        ExpectSuccess,
        AssertAppBundle,
        Label,
        WarnAsError,
        RuntimeType,
        ExtraBuildEnvironmentVariables,
        BootConfigFileName,
        NonDefaultFrameworkDir,
        ExtraMSBuildArgs,
        WasmPerfTracing
    )
    {
        this.IsPublish = IsPublish;
        this.BuildOnlyAfterPublish = BuildOnlyAfterPublish;
        this.ExpectRelinkDirWhenPublishing = ExpectRelinkDirWhenPublishing;
    }
}
