// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as CSharp from "Sdk.Rules.CSharp";
import * as Defs from "Defs";

const dotnetSdk = importFrom("DotNetSdk").extracted;
const sdkVersion = "11.0.100-preview.5.26227.104";

function sdkFile(path: string): File {
    return dotnetSdk.assertExistence(r`sdk/${sdkVersion}/${path}`);
}

const roslynDeps = [
    sdkFile("Microsoft.CodeAnalysis.dll"),
    sdkFile("Microsoft.CodeAnalysis.CSharp.dll"),
];

@@public
export const csharpToolchain = CSharp.csharpToolchainFromContents({
    name: "dotnet-sdk",
    contents: dotnetSdk,
    compilerPath: `sdk/${sdkVersion}/Roslyn/bincore/csc.dll`,
});

@@public
export const testLibrary = CSharp.csharp_library({
    name: "TestLibrary",
    toolchain: csharpToolchain,
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
        "//artifacts/bin/System.Text.Json/ref/Release/net11.0:System.Text.Json.dll",
    ],
    fileRefs: Defs.XUNIT_DEPS,
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

@@public
export const xunitWrapperLibrary = CSharp.csharp_library({
    name: "XUnitWrapperLibrary",
    toolchain: csharpToolchain,
    srcs: [
        "XUnitWrapperLibrary/Help.cs",
        "XUnitWrapperLibrary/TestFilter.cs",
        "XUnitWrapperLibrary/TestOutputRecorder.cs",
        "XUnitWrapperLibrary/TestSummary.cs",
    ],
    refs: [
        ...Defs.CORE_ROOT_REFPACK_DEPS,
        "//artifacts/bin/System.Xml.ReaderWriter/ref/Release/net11.0:System.Xml.ReaderWriter.dll",
    ],
    allowUnsafe: true,
});

@@public
export const xunitWrapperGenerator = CSharp.csharp_library({
    name: "XUnitWrapperGenerator",
    toolchain: csharpToolchain,
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
    refs: Defs.CORE_ROOT_REFPACK_DEPS,
    fileRefs: roslynDeps,
});
