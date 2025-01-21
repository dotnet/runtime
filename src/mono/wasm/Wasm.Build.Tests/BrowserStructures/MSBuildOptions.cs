// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests;

public abstract record MSBuildOptions
(
    bool                            IsPublish,
    bool                            AOT                             = false,
    NativeFilesType                 ExpectedFileType                = NativeFilesType.FromRuntimePack,
    string                          TargetFramework                 = BuildTestBase.DefaultTargetFramework,
    GlobalizationMode               GlobalizationMode               = GlobalizationMode.Sharded,
    string                          CustomIcuFile                   = "",
    bool                            UseCache                        = true,
    bool                            ExpectSuccess                   = true,
    bool                            AssertAppBundle                 = true,
    string                          Label                           = "",
    bool                            WarnAsError                     = true,
    RuntimeVariant                  RuntimeType                     = RuntimeVariant.SingleThreaded,
    IDictionary<string, string>?    ExtraBuildEnvironmentVariables  = null,
    string                          BootConfigFileName              = "blazor.boot.json",
    string                          NonDefaultFrameworkDir          = "",
    string                          ExtraMSBuildArgs                = ""
);
