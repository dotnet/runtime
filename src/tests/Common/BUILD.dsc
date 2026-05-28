// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as CSharp from "Sdk.Rules.CSharp";
import * as Defs from "Defs";

export const tc = CSharp.csharpToolchain({
    name: "dotnet-sdk",
    contents: importFrom("DotNetSdk").extracted,
    sdkVersion: "11.0.100-preview.5.26227.104",
    externalPackages: Defs.EXTERNAL_PACKAGES,
});

export const testLibrary = tc.csharp_library({
    name: "TestLibrary",
    tfm: "net11.0",
    useSharedCompilation: true,
    disableImplicitFrameworkRefs: true,
    srcs: [
        "CoreCLRTestLibrary/AssertExtensions.cs",
        "CoreCLRTestLibrary/CoreclrTestWrapperLib.cs",
        "CoreCLRTestLibrary/CoreClrConfigurationDetection.cs",
        "CoreCLRTestLibrary/Generator.cs",
        "CoreCLRTestLibrary/HostPolicyMock.cs",
        "CoreCLRTestLibrary/Logging.cs",
        "CoreCLRTestLibrary/PlatformDetection.cs",
        "CoreCLRTestLibrary/TestFramework.cs",
        "CoreCLRTestLibrary/Utilities.cs",
        "CoreCLRTestLibrary/Vectors.cs",
        "CoreCLRTestLibrary/XPlatformUtils.cs",
    ],
    refs: [
        ...Defs.CORE_ROOT_REFPACK_DEPS,
        ...Defs.XUNIT_DEPS,
        "@Microsoft.NETCore.App.Ref//ref/net11.0:System.Text.Json.dll",
    ],
    allowUnsafe: true,
    nowarn: [
        "CS0419",
        "CS1572",
        "CS1574",
        "CS1710",
        "CS3001",
        "CS3002",
        "CS3003",
    ],
});

export const xunitWrapperLibrary = tc.csharp_library({
    name: "XUnitWrapperLibrary",
    tfm: "net11.0",
    useSharedCompilation: true,
    disableImplicitFrameworkRefs: true,
    srcs: [
        "XUnitWrapperLibrary/Help.cs",
        "XUnitWrapperLibrary/TestFilter.cs",
        "XUnitWrapperLibrary/TestOutputRecorder.cs",
        "XUnitWrapperLibrary/TestSummary.cs",
    ],
    refs: [
        ...Defs.CORE_ROOT_REFPACK_DEPS,
        "@Microsoft.NETCore.App.Ref//ref/net11.0:System.Xml.ReaderWriter.dll",
    ],
    allowUnsafe: true,
});

export const xunitWrapperGenerator = tc.csharp_library({
    name: "XUnitWrapperGenerator",
    tfm: "net11.0",
    useSharedCompilation: true,
    disableImplicitFrameworkRefs: true,
    srcs: [
        "XUnitWrapperGenerator/CodeBuilder.cs",
        "XUnitWrapperGenerator/Descriptors.cs",
        "XUnitWrapperGenerator/ImmutableDictionaryValueComparer.cs",
        "XUnitWrapperGenerator/ITestInfo.cs",
        "XUnitWrapperGenerator/OptionsHelper.cs",
        "XUnitWrapperGenerator/RoslynUtils.cs",
        "XUnitWrapperGenerator/RuntimeConfiguration.cs",
        "XUnitWrapperGenerator/RuntimeTestModes.cs",
        "XUnitWrapperGenerator/SymbolExtensions.cs",
        "XUnitWrapperGenerator/TargetFrameworkMonikers.cs",
        "XUnitWrapperGenerator/TestPlatforms.cs",
        "XUnitWrapperGenerator/TestRuntimes.cs",
        "XUnitWrapperGenerator/XUnitWrapperGenerator.cs",
        "XUnitWrapperLibrary/TestFilter.cs",
    ],
    refs: [
        ...Defs.CORE_ROOT_REFPACK_DEPS,
        "@Microsoft.CodeAnalysis.Common//lib/net9.0:Microsoft.CodeAnalysis.dll",
        "@Microsoft.CodeAnalysis.CSharp//lib/net9.0:Microsoft.CodeAnalysis.CSharp.dll",
    ],
});
