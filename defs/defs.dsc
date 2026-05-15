// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Repo-level definitions — pure label constants and external package map.
 *
 * Framework refs use labels that encode workspace-relative paths.
 * External packages (NuGet, SDK) use @pkg//path:file labels resolved
 * against the EXTERNAL_PACKAGES map.
 */

import * as Rules from "Sdk.Rules";

// ============================================================================
//  EXTERNAL_PACKAGES — StaticDirectory map for @pkg label resolution
// ============================================================================

@@public
export const EXTERNAL_PACKAGES: Map<string, StaticDirectory> = Map.empty<string, StaticDirectory>()
    .add("DotNetSdk", importFrom("DotNetSdk").extracted)
    .add("Microsoft.NETCore.App.Ref", importFrom("Microsoft.NETCore.App.Ref").Contents.all)
    .add("Microsoft.DotNet.XUnitAssert", importFrom("Microsoft.DotNet.XUnitAssert").Contents.all)
    .add("xunit.extensibility.core", importFrom("xunit.extensibility.core").Contents.all)
    .add("Microsoft.DotNet.XUnitExtensions", importFrom("Microsoft.DotNet.XUnitExtensions").Contents.all)
    .add("xunit.abstractions", importFrom("xunit.abstractions").Contents.all);

// ============================================================================
//  CORE_ROOT_REFPACK_DEPS — framework refs from Microsoft.NETCore.App.Ref
//  Convention: @Microsoft.NETCore.App.Ref//ref/net11.0:<Name>.dll
// ============================================================================

function refLabel(name: string): Rules.Label {
    return `@Microsoft.NETCore.App.Ref//ref/net11.0:${name}.dll`;
}

@@public
export const CORE_ROOT_REFPACK_DEPS: Rules.Label[] = [
    refLabel("Microsoft.Win32.Primitives"),
    refLabel("System.Collections"),
    refLabel("System.Collections.Concurrent"),
    refLabel("System.Collections.Immutable"),
    refLabel("System.Collections.NonGeneric"),
    refLabel("System.Collections.Specialized"),
    refLabel("System.ComponentModel"),
    refLabel("System.ComponentModel.Primitives"),
    refLabel("System.Console"),
    refLabel("System.Diagnostics.FileVersionInfo"),
    refLabel("System.Diagnostics.Process"),
    refLabel("System.Diagnostics.Tracing"),
    refLabel("System.IO.MemoryMappedFiles"),
    refLabel("System.Linq"),
    refLabel("System.Memory"),
    refLabel("System.Numerics.Vectors"),
    refLabel("System.ObjectModel"),
    refLabel("System.Reflection.Emit"),
    refLabel("System.Reflection.Emit.ILGeneration"),
    refLabel("System.Reflection.Emit.Lightweight"),
    refLabel("System.Reflection.Metadata"),
    refLabel("System.Reflection.Primitives"),
    refLabel("System.Reflection.TypeExtensions"),
    refLabel("System.Runtime"),
    refLabel("System.Runtime.InteropServices"),
    refLabel("System.Runtime.Intrinsics"),
    refLabel("System.Runtime.Loader"),
    refLabel("System.Runtime.Numerics"),
    refLabel("System.Runtime.Serialization.Primitives"),
    refLabel("System.Security.Cryptography"),
    refLabel("System.Text.Encoding.Extensions"),
    refLabel("System.Text.Encodings.Web"),
    refLabel("System.Text.RegularExpressions"),
    refLabel("System.Threading"),
    refLabel("System.Threading.Overlapped"),
    refLabel("System.Threading.Tasks.Parallel"),
    refLabel("System.Threading.Thread"),
    refLabel("System.Threading.ThreadPool")
];

// ============================================================================
//  XUNIT_DEPS — xunit compile-time refs (label-based)
// ============================================================================

@@public
export const XUNIT_DEPS: Rules.Label[] = [
    "@Microsoft.DotNet.XUnitAssert//lib/net10.0:xunit.assert.dll",
    "@xunit.extensibility.core//lib/netstandard1.1:xunit.core.dll",
    "@Microsoft.DotNet.XUnitExtensions//lib/net10.0:Microsoft.DotNet.XUnitExtensions.dll",
    "@xunit.abstractions//lib/netstandard1.0:xunit.abstractions.dll"
];

// ============================================================================
//  XUNIT_RUNTIME_DEPS — runtime files staged beside executable tests
//  These are resolved File objects (not labels) because they are staged
//  directly into the test output directory by the test runner.
// ============================================================================

@@public
export const XUNIT_RUNTIME_DEPS: File[] = [
    EXTERNAL_PACKAGES.get("Microsoft.DotNet.XUnitAssert").assertExistence(r`lib/net10.0/xunit.assert.dll`),
    EXTERNAL_PACKAGES.get("xunit.extensibility.core").assertExistence(r`lib/netstandard1.1/xunit.core.dll`),
    EXTERNAL_PACKAGES.get("Microsoft.DotNet.XUnitExtensions").assertExistence(r`lib/net10.0/Microsoft.DotNet.XUnitExtensions.dll`),
    EXTERNAL_PACKAGES.get("xunit.abstractions").assertExistence(r`lib/netstandard1.0/xunit.abstractions.dll`),
];

//  CORE_ROOT paths used by BuildXL-backed CoreCLR test execution
// ============================================================================

@@public
export const CORE_ROOT_DIR: Directory =
    d`${Context.getMount("SourceRoot").path}/artifacts/tests/coreclr/linux.x64.Release/Tests/Core_Root`;

@@public
export const CORE_ROOT_CORERUN: File =
    f`${CORE_ROOT_DIR}/corerun`;

// ============================================================================
//  CORECLR_TEST_COMMON_DEPS — label-based deps baked into coreclr_test
// ============================================================================

@@public
export const CORECLR_TEST_COMMON_DEPS: Rules.Label[] = [
    ...CORE_ROOT_REFPACK_DEPS,
    ...XUNIT_DEPS
];
