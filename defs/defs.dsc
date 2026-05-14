// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Repo-level definitions — pure label constants and NuGet package refs.
 *
 * Framework refs use labels that encode workspace-relative paths.
 * NuGet packages use importFrom() — resolved by the NuGet resolver
 * declared in config.dsc.
 */

import * as Rules from "Sdk.Rules";

// ============================================================================
//  CORE_ROOT_REFPACK_DEPS — framework refs available in this repo
//  Convention: artifacts/bin/<Name>/ref/Release/net11.0/<Name>.dll
// ============================================================================

function refLabel(name: string): Rules.Label {
    return `//artifacts/bin/${name}/ref/Release/net11.0:${name}.dll`;
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
//  XUNIT_DEPS — xunit compile-time refs (from NuGet resolver)
// ============================================================================

@@public
export const XUNIT_DEPS: File[] = [
    importFrom("Microsoft.DotNet.XUnitAssert").Contents.all.getFile(r`lib/net10.0/xunit.assert.dll`),
    importFrom("xunit.extensibility.core").Contents.all.getFile(r`lib/netstandard1.1/xunit.core.dll`),
    importFrom("Microsoft.DotNet.XUnitExtensions").Contents.all.getFile(r`lib/net10.0/Microsoft.DotNet.XUnitExtensions.dll`),
    importFrom("xunit.abstractions").Contents.all.getFile(r`lib/netstandard1.0/xunit.abstractions.dll`)
];

//  XUNIT_RUNTIME_DEPS — runtime files staged beside executable tests
// ============================================================================

@@public
export const XUNIT_RUNTIME_DEPS: File[] = [
    ...XUNIT_DEPS
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
];

// ============================================================================
//  CORECLR_TEST_COMMON_REFS — file-based refs (NuGet packages)
// ============================================================================

@@public
export const CORECLR_TEST_COMMON_REFS: File[] = [
    ...XUNIT_DEPS
];
