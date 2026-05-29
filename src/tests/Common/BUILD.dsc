// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

import * as CSharp from "Sdk.Rules.CSharp";
import * as Rules from "Sdk.Rules";
import * as Defs from "Defs";

export const tc = CSharp.csharpToolchain({
    name: "dotnet-sdk",
    contents: importFrom("DotNetSdk").extracted,
    sdkVersion: "11.0.100-preview.5.26227.104",
    externalPackages: Defs.EXTERNAL_PACKAGES,
});

// ============================================================================
//  Imported providers — wrap pre-built DLL labels into a Target via
//  import_dll so they flow through deps like compiled targets.
// ============================================================================

function importRef(name: string): Rules.Target {
    return tc.import_dll({
        name: name,
        dll: `@Microsoft.NETCore.App.Ref//ref/net11.0:${name}.dll`,
    });
}

export const REFPACK_IMPORTS: Rules.Target[] = [
    importRef("Microsoft.Win32.Primitives"),
    importRef("System.Collections"),
    importRef("System.Collections.Concurrent"),
    importRef("System.Collections.Immutable"),
    importRef("System.Collections.NonGeneric"),
    importRef("System.Collections.Specialized"),
    importRef("System.ComponentModel"),
    importRef("System.ComponentModel.Primitives"),
    importRef("System.Console"),
    importRef("System.Diagnostics.FileVersionInfo"),
    importRef("System.Diagnostics.Process"),
    importRef("System.Diagnostics.Tracing"),
    importRef("System.IO.MemoryMappedFiles"),
    importRef("System.Linq"),
    importRef("System.Memory"),
    importRef("System.Numerics.Vectors"),
    importRef("System.ObjectModel"),
    importRef("System.Reflection.Emit"),
    importRef("System.Reflection.Emit.ILGeneration"),
    importRef("System.Reflection.Emit.Lightweight"),
    importRef("System.Reflection.Metadata"),
    importRef("System.Reflection.Primitives"),
    importRef("System.Reflection.TypeExtensions"),
    importRef("System.Runtime"),
    importRef("System.Runtime.InteropServices"),
    importRef("System.Runtime.Intrinsics"),
    importRef("System.Runtime.Loader"),
    importRef("System.Runtime.Numerics"),
    importRef("System.Runtime.Serialization.Primitives"),
    importRef("System.Security.Cryptography"),
    importRef("System.Text.Encoding.Extensions"),
    importRef("System.Text.Encodings.Web"),
    importRef("System.Text.RegularExpressions"),
    importRef("System.Threading"),
    importRef("System.Threading.Overlapped"),
    importRef("System.Threading.Tasks.Parallel"),
    importRef("System.Threading.Thread"),
    importRef("System.Threading.ThreadPool"),
];

export const XUNIT_IMPORTS: Rules.Target[] = [
    tc.import_dll({ name: "xunit.assert", dll: "@Microsoft.DotNet.XUnitAssert//lib/net10.0:xunit.assert.dll" }),
    tc.import_dll({ name: "xunit.core", dll: "@xunit.extensibility.core//lib/netstandard1.1:xunit.core.dll" }),
    tc.import_dll({ name: "XUnitExtensions", dll: "@Microsoft.DotNet.XUnitExtensions//lib/net10.0:Microsoft.DotNet.XUnitExtensions.dll" }),
    tc.import_dll({ name: "xunit.abstractions", dll: "@xunit.abstractions//lib/netstandard1.0:xunit.abstractions.dll" }),
];

const systemTextJson = tc.import_dll({ name: "System.Text.Json", dll: "@Microsoft.NETCore.App.Ref//ref/net11.0:System.Text.Json.dll" });
const systemXmlReaderWriter = tc.import_dll({ name: "System.Xml.ReaderWriter", dll: "@Microsoft.NETCore.App.Ref//ref/net11.0:System.Xml.ReaderWriter.dll" });
const codeAnalysisCommon = tc.import_dll({ name: "Microsoft.CodeAnalysis", dll: "@Microsoft.CodeAnalysis.Common//lib/net9.0:Microsoft.CodeAnalysis.dll" });
const codeAnalysisCSharp = tc.import_dll({ name: "Microsoft.CodeAnalysis.CSharp", dll: "@Microsoft.CodeAnalysis.CSharp//lib/net9.0:Microsoft.CodeAnalysis.CSharp.dll" });

export const COMMON_TEST_IMPORTS: Rules.Target[] = [
    ...REFPACK_IMPORTS,
    ...XUNIT_IMPORTS,
];

// ============================================================================
//  Test support libraries
// ============================================================================

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
    deps: [
        ...REFPACK_IMPORTS,
        ...XUNIT_IMPORTS,
        systemTextJson,
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
    deps: [
        ...REFPACK_IMPORTS,
        systemXmlReaderWriter,
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
    deps: [
        ...REFPACK_IMPORTS,
        codeAnalysisCommon,
        codeAnalysisCSharp,
    ],
});
