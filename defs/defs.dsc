// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

/**
 * Repo-level definitions — pure label constants and external package map.
 *
 * External packages (NuGet, SDK) use @pkg//path:file labels resolved
 * against the EXTERNAL_PACKAGES map. Assembly imports are created
 * from labels via csharp_import in BUILD.dsc.
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
    .add("xunit.abstractions", importFrom("xunit.abstractions").Contents.all)
    .add("Microsoft.CodeAnalysis.Common", importFrom("Microsoft.CodeAnalysis.Common").Contents.all)
    .add("Microsoft.CodeAnalysis.CSharp", importFrom("Microsoft.CodeAnalysis.CSharp").Contents.all);

// ============================================================================
//  XUNIT_RUNTIME_DEPS — runtime files staged beside executable tests
//  These are source artifacts resolved from external packages.
// ============================================================================

@@public
export const XUNIT_RUNTIME_DEPS: Rules.Artifact[] = [
    Rules.sourceArtifact(EXTERNAL_PACKAGES.get("Microsoft.DotNet.XUnitAssert").assertExistence(r`lib/net10.0/xunit.assert.dll`)),
    Rules.sourceArtifact(EXTERNAL_PACKAGES.get("xunit.extensibility.core").assertExistence(r`lib/netstandard1.1/xunit.core.dll`)),
    Rules.sourceArtifact(EXTERNAL_PACKAGES.get("Microsoft.DotNet.XUnitExtensions").assertExistence(r`lib/net10.0/Microsoft.DotNet.XUnitExtensions.dll`)),
    Rules.sourceArtifact(EXTERNAL_PACKAGES.get("xunit.abstractions").assertExistence(r`lib/netstandard1.0/xunit.abstractions.dll`)),
];

//  CORE_ROOT paths used by BuildXL-backed CoreCLR test execution.
//  Match the normal CoreCLR test flow: Checked runtime + Release libraries.
// ============================================================================

@@public
export const CORE_ROOT_DIR: Directory =
    d`${Context.getMount("SourceRoot").path}/artifacts/tests/coreclr/linux.x64.Checked/Tests/Core_Root`;

@@public
export const CORE_ROOT_CORERUN: Rules.Artifact =
    Rules.sourceArtifact(f`${CORE_ROOT_DIR}/corerun`);

@@public
export const CORE_ROOT_ILASM: Rules.Artifact =
    Rules.sourceArtifact(f`${CORE_ROOT_DIR}/ilasm`);
