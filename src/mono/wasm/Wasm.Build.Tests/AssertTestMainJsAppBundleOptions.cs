// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

namespace Wasm.Build.Tests;

public record AssertTestMainJsAppBundleOptions
(
   string BundleDir,
   string ProjectName,
   string Config,
   string MainJS,
   bool HasV8Script,
   string TargetFramework,
   GlobalizationMode? GlobalizationMode,
   string PredefinedIcudt = "",
   bool UseWebcil = true,
   bool DotnetWasmFromRuntimePack = true,
   bool IsBrowserProject = true,
   bool IsPublish = false
);
