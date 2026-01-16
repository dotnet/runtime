#!/usr/bin/env python3
"""
Script to migrate test exclusions from issues.targets to individual test files and projects.
"""

import os
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
from typing import List, Dict, Tuple, Optional
from dataclasses import dataclass

@dataclass
class ExclusionItem:
    """Represents an ExcludeList item from issues.targets"""
    path: str
    issue: str
    condition: str
    itemgroup_line: int

@dataclass
class ReplacementMapping:
    """Represents the replacement attributes/properties for a given condition"""
    condition: str
    cs_attribute: str
    il_attribute: str
    project_property: str

# Mapping table from the problem statement
REPLACEMENT_MAPPINGS = [
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != ''",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Any)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(-1 /* Any */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", TestRuntimes.CoreCLR)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = { string(\'<Issue>\'), int32(1 /* CoreCLR */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsWindows)' != 'true'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.AnyUnix)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(32762 /* AnyUnix */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsWindows)' != 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.AnyUnix, runtimes: TestRuntimes.CoreCLR)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms, [Microsoft.DotNet.XUnitExtensions]Xunit.TargetFrameworkMonikers, [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = { string(\'<Issue>\') int32(32762 /* AnyUnix */) int32(-1 /* Any */) int32(1 /* CoreCLR */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TargetArchitecture)' == 'arm' or '$(AltJitArch)' == 'arm') and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsArmProcess))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsArmProcess\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TargetArchitecture)' == 'arm64' or '$(AltJitArch)' == 'arm64') and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsArm64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsArm64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsWindows)' == 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Windows)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(1 /* Windows */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetArchitecture)' == 'x64' and '$(TargetsWindows)' == 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsWindows\' \'IsX64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetArchitecture)' == 'x86' and '$(TargetsWindows)' == 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsX86Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsWindows\' \'IsX86Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TargetArchitecture)' == 'arm64' or '$(AltJitArch)' == 'arm64') and '$(TargetsWindows)' == 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsWindows), nameof(PlatformDetection.IsArm64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsWindows\' \'IsArm64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetArchitecture)' == 'x64' and '$(TargetsWindows)' != 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsX64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsNotWindows\' \'IsX64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TargetArchitecture)' == 'arm64' or '$(AltJitArch)' == 'arm64') and '$(TargetsWindows)' != 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsArm64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsNotWindows\' \'IsArm64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TargetArchitecture)' == 'arm' or '$(AltJitArch)' == 'arm') and '$(TargetsWindows)' != 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsNotWindows), nameof(PlatformDetection.IsArmProcess))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsNotWindows\' \'IsArmProcess\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsOSX)' == 'true' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.OSX, runtimes: TestRuntimes.CoreCLR)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms, [Microsoft.DotNet.XUnitExtensions]Xunit.TargetFrameworkMonikers, [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = { string(\'<Issue>\') int32(4 /* OSX */) int32(-1 /* Any */) int32(1 /* CoreCLR */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsOSX)' == 'true' and '$(TargetArchitecture)' == 'x64' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsOSX), nameof(PlatformDetection.IsX64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsOSX\' \'IsX64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsOSX)' == 'true' and '$(TargetArchitecture)' == 'arm64' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsOSX), nameof(PlatformDetection.IsArm64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsOSX\' \'IsArm64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TestBuildMode)' == 'crossgen2' or '$(TestBuildMode)' == 'crossgen') and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute="",
        il_attribute="",
        project_property='<CrossGenTest>false</Crossgen2> <!-- <Issue> -->'
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TestBuildMode)' == 'crossgen2' and '$(RuntimeFlavor)' == 'coreclr' and '$(TargetArchitecture)' == 'x86'",
        cs_attribute="",
        il_attribute="",
        project_property='<CrossGenTest Condition="\'$(TargetArchitecture)\' == \'x86\'">false</Crossgen2> <!-- <Issue> -->'
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TestBuildMode)' == 'crossgen2' or '$(TestBuildMode)' == 'crossgen') and '$(RuntimeFlavor)' == 'coreclr' and ('$(TargetArchitecture)' == 'arm' or '$(AltJitArch)' == 'arm')",
        cs_attribute="",
        il_attribute="",
        project_property='<CrossGenTest Condition="\'$(TargetArchitecture)\' == \'arm\'">false</Crossgen2> <!-- <Issue> -->'
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and ('$(TestBuildMode)' == 'crossgen2' or '$(TestBuildMode)' == 'crossgen') and '$(RuntimeFlavor)' == 'coreclr' and ('$(TargetArchitecture)' == 'arm64' or '$(TargetArchitecture)' == 'arm' or '$(AltJitArch)' == 'arm')",
        cs_attribute="",
        il_attribute="",
        project_property='<CrossGenTest Condition="\'$(TargetArchitecture)\' == \'arm\' or \'$(TargetArchitecture)\' == \'arm64\'">false</Crossgen2> <!-- <Issue> -->'
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TestBuildMode)' == 'nativeaot' and '$(RuntimeFlavor)' == 'coreclr'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(Utilities), nameof(Utilities.IsNativeAot))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.Utilities) string[1] (\'IsNativeAot\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TestBuildMode)' == 'nativeaot' and '$(RuntimeFlavor)' == 'coreclr' and '$(TargetArchitecture)' == 'x86'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(Utilities), nameof(Utilities.IsNativeAot), nameof(Utilities.IsX86))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.Utilities) string[2] (\'IsNativeAot\' \'IsX86\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono'",
        cs_attribute='[ActiveIssue("<Issue>", TestRuntimes.Mono)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = { string(\'<Issue>\'), int32(2 /* Mono */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(TargetsWindows)' == 'true'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Windows, runtimes: TestRuntimes.Mono)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms, [Microsoft.DotNet.XUnitExtensions]Xunit.TargetFrameworkMonikers, [Microsoft.DotNet.XUnitExtensions]Xunit.TestRuntimes) = { string(\'<Issue>\') int32(1 /* Windows */) int32(-1 /* Any */) int32(2 /* Mono */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="('$(RuntimeFlavor)' == 'mono' and '$(RuntimeVariant)' == 'monointerpreter') or '$(TargetsAppleMobile)' == 'true'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsMonoInterpreter\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and ('$(RuntimeVariant)' == 'llvmaot' or '$(TargetsAppleMobile)' == 'true')",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoLLVMAOT))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsMonoLLVMAOT\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and ('$(RuntimeVariant)' == 'llvmfullaot' or '$(RuntimeVariant)' == 'minifullaot' or '$(TargetsAppleMobile)' == 'true')",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoFULLAOT))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsMonoFULLAOT\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and ('$(RuntimeVariant)' == 'llvmfullaot' or '$(RuntimeVariant)' == 'llvmaot' or '$(RuntimeVariant)' == 'minifullaot' or '$(TargetsAppleMobile)' == 'true')",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoAnyAOT))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsMonoAnyAOT\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(RuntimeVariant)' == 'llvmfullaot'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoLLVMFULLAOT))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsMonoLLVMFULLAOT\') }',
        project_property='<MonoAotIncompatible>true</MonoAotIncompatible>  <!-- <Issue> -->'
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(RuntimeVariant)' == 'llvmfullaot' and '$(TargetArchitecture)' == 'x64'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoLLVMFULLAOT), nameof(PlatformDetection.IsX64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsMonoLLVMFULLAOT\' \'IsX64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(RuntimeVariant)' == 'minijit' and '$(TargetArchitecture)' == 'x64'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMiniJIT), nameof(PlatformDetection.IsX64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsMonoMiniJIT\' \'IsX64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(RuntimeVariant)' == 'minijit' and '$(TargetArchitecture)' == 'arm64'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMiniJIT), nameof(PlatformDetection.IsArm64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsMonoMiniJIT\' \'IsArm64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(TargetArchitecture)' == 'arm64' and '$(TargetsWindows)' != 'true'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMiniJIT), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsNotWindows))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[3] (\'IsMonoMiniJIT\' \'IsArm64Process\' \'IsNotWindows\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and ('$(TargetArchitecture)' == 'arm64') and '$(TargetsWindows)' != 'true' and '$(RuntimeVariant)' == 'monointerpreter'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsNotWindows))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[3] (\'IsMonoInterpreter\' \'IsArm64Process\' \'IsNotWindows\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(TargetArchitecture)' == 'arm64' and '$(TargetsOSX)' == 'true' and '$(RuntimeVariant)' == 'monointerpreter'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoInterpreter), nameof(PlatformDetection.IsArm64Process), nameof(PlatformDetection.IsOSX))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[3] (\'IsMonoInterpreter\' \'IsArm64Process\' \'IsOSX\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetArchitecture)' == 'wasm' or '$(TargetsAppleMobile)' == 'true'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(3456 /* Browser | iOS | tvOS | MacCatalyst */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(XunitTestBinBase)' != '' and '$(TargetsAppleMobile)' == 'true' and '$(TestBuildMode)' == 'nativeaot'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(2432 /* iOS | tvOS | MacCatalyst */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetArchitecture)' == 'wasm'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Browser)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(1024 /* Browser */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetOS)' == 'android'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Android)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(512 /* Android */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetOS)' == 'android' And '$(TargetArchitecture)' == 'arm64'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroid), nameof(PlatformDetection.IsArm64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsAndroid\' \'IsArm64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetsAppleMobile)' == 'true'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(2432 /* iOS | tvOS | MacCatalyst */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetArchitecture)' == 'wasm' or '$(TargetsMobile)' == 'true'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Browser | TestPlatforms.iOS | TestPlatforms.tvOS | TestPlatforms.MacCatalyst | TestPlatforms.Android)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(3968 /* Browser | iOS | tvOS | MacCatalyst | Android */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetOS)' == 'tvos'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.tvOS)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(256 /* tvOS */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetOS)' == 'iossimulator' or '$(TargetOS)' == 'tvossimulator'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsSimulator))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsSimulator\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(TargetOS)' == 'linux'",
        cs_attribute='[ActiveIssue("<Issue>", TestPlatforms.Linux)]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, [Microsoft.DotNet.XUnitExtensions]Xunit.TestPlatforms) = { string(\'<Issue>\'), int32(2 /* Linux */) }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(TargetArchitecture)' == 'riscv64'",
        cs_attribute='[ActiveIssue("<Issue>", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoRuntime), nameof(PlatformDetection.IsRiscv64Process))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[2] (\'IsMonoRuntime\' \'IsRiscv64Process\') }',
        project_property=""
    ),
    ReplacementMapping(
        condition="'$(RuntimeFlavor)' == 'mono' and '$(RuntimeVariant)' == 'minifullaot'",
        cs_attribute='[ActiveIssue("https://github.com/dotnet/runtime/issues/90427", typeof(PlatformDetection), nameof(PlatformDetection.IsMonoMINIFULLAOT))]',
        il_attribute='.custom instance void [Microsoft.DotNet.XUnitExtensions]Xunit.ActiveIssueAttribute::.ctor(string, class [mscorlib]System.Type, string[]) = {string(\'<Issue>\') type([TestLibrary]TestLibrary.PlatformDetection) string[1] (\'IsMonoMINIFULLAOT\') }',
        project_property=""
    ),
]


def normalize_condition(condition: str) -> str:
    """Normalize a condition string for comparison."""
    # Remove extra whitespace
    condition = ' '.join(condition.split())
    return condition.strip()


def parse_issues_targets(filepath: str) -> List[ExclusionItem]:
    """Parse the issues.targets file and extract exclusion items with their conditions."""
    exclusions = []
    
    with open(filepath, 'r') as f:
        content = f.read()
    
    # Parse XML
    root = ET.fromstring(content)
    
    # Find all ItemGroup elements
    for itemgroup in root.findall('.//{http://schemas.microsoft.com/developer/msbuild/2003}ItemGroup'):
        condition = itemgroup.get('Condition', '')
        
        # Find all ExcludeList items in this ItemGroup
        for exclude in itemgroup.findall('.//{http://schemas.microsoft.com/developer/msbuild/2003}ExcludeList'):
            path = exclude.get('Include', '')
            issue_elem = exclude.find('.//{http://schemas.microsoft.com/developer/msbuild/2003}Issue')
            issue = issue_elem.text if issue_elem is not None and issue_elem.text else ''
            
            if path:
                exclusions.append(ExclusionItem(
                    path=path,
                    issue=issue,
                    condition=normalize_condition(condition),
                    itemgroup_line=0  # We'll need this for later
                ))
    
    return exclusions


def resolve_path(exclude_path: str, repo_root: str) -> Tuple[str, str]:
    """
    Convert an ExcludeList path to the actual source directory.
    Phase 1 of the problem statement.
    Returns (directory_path, project_name_hint)
    """
    # Replace $(XunitTestBinBase) with src/tests
    path = exclude_path.replace('$(XunitTestBinBase)', 'src/tests')
    
    # Remove glob patterns at the end
    original_path = path
    path = re.sub(r'/\*\*?$', '', path)
    
    # The last component is usually the project name
    project_name_hint = os.path.basename(path)
    
    return (os.path.join(repo_root, path), project_name_hint)


def find_project_files(directory: str, project_name_hint: str = None) -> List[str]:
    """
    Find project files (.csproj, .ilproj, .fsproj) in the given directory.
    Implements Phase 2 of the problem statement.
    """
    project_files = []
    
    # First check if the directory exists
    if not os.path.exists(directory):
        # Try parent directory with project name hint
        if project_name_hint:
            parent = os.path.dirname(directory)
            if os.path.exists(parent):
                for ext in ['.csproj', '.ilproj', '.fsproj']:
                    candidate = os.path.join(parent, project_name_hint + ext)
                    if os.path.exists(candidate):
                        return [candidate]
        return []
    
    # Approach A: Recursive search
    for root, dirs, files in os.walk(directory):
        for file in files:
            if file.endswith(('.csproj', '.ilproj', '.fsproj')):
                project_files.append(os.path.join(root, file))
    
    if project_files:
        return project_files
    
    # Approach B: Specific file match
    # Check in the directory itself
    for ext in ['.csproj', '.ilproj', '.fsproj']:
        candidate = os.path.join(directory, project_name_hint + ext) if project_name_hint else None
        if candidate and os.path.exists(candidate):
            return [candidate]
    
    # Check in parent directory
    if project_name_hint:
        parent = os.path.dirname(directory)
        for ext in ['.csproj', '.ilproj', '.fsproj']:
            candidate = os.path.join(parent, project_name_hint + ext)
            if os.path.exists(candidate):
                return [candidate]
    
    return []


def get_replacement_for_condition(condition: str) -> Optional[ReplacementMapping]:
    """Find the replacement mapping for a given condition."""
    normalized = normalize_condition(condition)
    
    for mapping in REPLACEMENT_MAPPINGS:
        if normalize_condition(mapping.condition) == normalized:
            return mapping
    
    return None


def find_test_methods_cs(content: str) -> List[Tuple[int, str]]:
    """
    Find all methods with [Fact] or [Theory] attributes in C# code.
    Returns list of (line_number, method_signature) tuples.
    """
    lines = content.split('\n')
    test_methods = []
    
    i = 0
    while i < len(lines):
        line = lines[i].strip()
        
        # Look for [Fact] or [Theory]
        if '[Fact]' in line or '[Theory]' in line:
            # Find the method signature (might be on next line)
            j = i + 1
            while j < len(lines) and lines[j].strip().startswith('['):
                j += 1
            
            if j < len(lines):
                method_line = lines[j].strip()
                # Extract method name
                match = re.search(r'\s+(\w+\s+)+(\w+)\s*\(', method_line)
                if match:
                    test_methods.append((i, method_line))
        
        i += 1
    
    return test_methods


def add_cs_attribute(filepath: str, attribute: str):
    """Add an attribute to all [Fact] and [Theory] methods in a C# file."""
    with open(filepath, 'r') as f:
        content = f.read()
    
    lines = content.split('\n')
    modified = False
    new_lines = []
    i = 0
    
    # Track if we need to add using directives
    needs_xunit_using = 'using Xunit;' not in content
    needs_testlibrary_using = 'using TestLibrary;' not in content
    
    # Extract the attribute name for checking
    attr_name_match = re.search(r'\[(\w+)', attribute)
    attr_name = attr_name_match.group(1) if attr_name_match else None
    
    while i < len(lines):
        line = lines[i]
        stripped = line.strip()
        
        # Check if this line has [Fact] or [Theory]
        if '[Fact]' in stripped or '[Theory]' in stripped:
            # Check if the attribute is already present on nearby lines
            has_attribute = False
            for check_idx in range(max(0, i - 5), min(len(lines), i + 5)):
                if attr_name and attr_name in lines[check_idx]:
                    has_attribute = True
                    break
            
            if not has_attribute:
                # Add the attribute before this line with the same indentation
                indent = len(line) - len(line.lstrip())
                new_lines.append(' ' * indent + attribute)
                modified = True
        
        new_lines.append(line)
        i += 1
    
    if modified:
        # Add using directives at the top if needed
        final_lines = []
        using_section_end = 0
        
        # Find the end of the using section
        for idx, line in enumerate(new_lines):
            stripped = line.strip()
            if stripped.startswith('using '):
                using_section_end = idx + 1
            elif stripped and not stripped.startswith('//') and not stripped.startswith('/*'):
                # First non-comment, non-using line
                if using_section_end == 0:
                    using_section_end = idx
                break
        
        # Insert new using directives
        insert_idx = using_section_end
        final_lines = new_lines[:insert_idx]
        
        if needs_xunit_using and 'using Xunit;' not in '\n'.join(final_lines):
            final_lines.append('using Xunit;')
        
        if needs_testlibrary_using and 'using TestLibrary;' not in '\n'.join(final_lines):
            final_lines.append('using TestLibrary;')
        
        final_lines.extend(new_lines[insert_idx:])
        
        with open(filepath, 'w') as f:
            f.write('\n'.join(final_lines))
        
        return True
    
    return False


def ensure_testlibrary_reference(project_file: str) -> bool:
    """Ensure the project file has a reference to TestLibrary."""
    with open(project_file, 'r') as f:
        content = f.read()
    
    if '$(TestLibraryProjectPath)' in content:
        return False
    
    # Parse and add the reference
    try:
        tree = ET.parse(project_file)
        root = tree.getroot()
        
        # Find or create ItemGroup
        itemgroups = root.findall('{http://schemas.microsoft.com/developer/msbuild/2003}ItemGroup')
        if not itemgroups:
            itemgroup = ET.SubElement(root, 'ItemGroup')
        else:
            itemgroup = itemgroups[0]
        
        # Add ProjectReference
        proj_ref = ET.SubElement(itemgroup, 'ProjectReference')
        proj_ref.set('Include', '$(TestLibraryProjectPath)')
        
        # Write back
        tree.write(project_file, encoding='utf-8', xml_declaration=True)
        return True
    except:
        return False


def add_il_attribute(filepath: str, attribute: str):
    """Add an attribute to all methods with [Fact] or [Theory] in an IL file."""
    # This is complex - for now, just report that it needs manual intervention
    print(f"  IL file needs manual update: {filepath}")
    print(f"  Add: {attribute}")
    return False


def add_project_property(project_file: str, property_xml: str):
    """Add a property to a project file."""
    with open(project_file, 'r') as f:
        content = f.read()
    
    # Check if property already exists
    property_name = re.search(r'<(\w+)', property_xml)
    if property_name and property_name.group(1) in content:
        return False
    
    try:
        tree = ET.parse(project_file)
        root = tree.getroot()
        
        # Find or create PropertyGroup
        propertygroups = root.findall('{http://schemas.microsoft.com/developer/msbuild/2003}PropertyGroup')
        if not propertygroups:
            propgroup = ET.SubElement(root, 'PropertyGroup')
        else:
            propgroup = propertygroups[0]
        
        # Parse and add the property
        # This is simplified - full XML parsing would be better
        lines = content.split('\n')
        for i, line in enumerate(lines):
            if '<PropertyGroup>' in line and not 'Condition' in line:
                lines.insert(i + 1, '    ' + property_xml)
                break
        
        with open(project_file, 'w') as f:
            f.write('\n'.join(lines))
        
        return True
    except Exception as e:
        print(f"  Error adding property to {project_file}: {e}")
        return False


def migrate_exclusion(exclusion: ExclusionItem, repo_root: str, dry_run: bool = True):
    """Migrate a single exclusion item."""
    print(f"\nProcessing: {exclusion.path}")
    print(f"  Issue: {exclusion.issue}")
    print(f"  Condition: {exclusion.condition}")
    
    # Resolve path
    resolved_path, project_name_hint = resolve_path(exclusion.path, repo_root)
    print(f"  Resolved path: {resolved_path}")
    print(f"  Project name hint: {project_name_hint}")
    
    # Find project files
    project_files = find_project_files(resolved_path, project_name_hint)
    
    if not project_files:
        print(f"  WARNING: No project files found!")
        return
    
    print(f"  Found {len(project_files)} project file(s):")
    for pf in project_files:
        print(f"    - {pf}")
    
    # Get replacement mapping
    mapping = get_replacement_for_condition(exclusion.condition)
    
    if not mapping:
        print(f"  WARNING: No replacement mapping found for condition!")
        return
    
    # Apply replacements
    for project_file in project_files:
        project_dir = os.path.dirname(project_file)
        
        # Parse the project file to find which source files it includes
        cs_files = []
        il_files = []
        
        try:
            tree = ET.parse(project_file)
            root = tree.getroot()
            
            # Try both with and without namespace
            compile_elems = (
                root.findall('.//{http://schemas.microsoft.com/developer/msbuild/2003}Compile') +
                root.findall('.//Compile')
            )
            
            for compile_elem in compile_elems:
                include = compile_elem.get('Include', '')
                if include:
                    full_path = os.path.join(project_dir, include)
                    if include.endswith('.cs'):
                        if os.path.exists(full_path):
                            cs_files.append(full_path)
                    elif include.endswith('.il'):
                        if os.path.exists(full_path):
                            il_files.append(full_path)
        except Exception as e:
            print(f"  Warning: Could not parse project file: {e}")
            # If we can't parse the project file, fall back to directory scanning
            for root, dirs, files in os.walk(project_dir):
                for file in files:
                    if file.endswith('.cs'):
                        cs_files.append(os.path.join(root, file))
                    elif file.endswith('.il'):
                        il_files.append(os.path.join(root, file))
        
        # Apply C# attributes
        if mapping.cs_attribute and cs_files:
            attribute = mapping.cs_attribute.replace('<Issue>', exclusion.issue)
            print(f"  C# attribute: {attribute}")
            
            if not dry_run:
                for cs_file in cs_files:
                    if add_cs_attribute(cs_file, attribute):
                        print(f"    Modified: {cs_file}")
                    ensure_testlibrary_reference(project_file)
        
        # Apply IL attributes
        if mapping.il_attribute and il_files:
            attribute = mapping.il_attribute.replace('<Issue>', exclusion.issue)
            print(f"  IL attribute: {attribute}")
            
            if not dry_run:
                for il_file in il_files:
                    add_il_attribute(il_file, attribute)
        
        # Apply project properties
        if mapping.project_property:
            prop = mapping.project_property.replace('<Issue>', exclusion.issue)
            print(f"  Project property: {prop}")
            
            if not dry_run:
                if add_project_property(project_file, prop):
                    print(f"    Modified project: {project_file}")


def main():
    repo_root = '/home/runner/work/runtime/runtime'
    issues_targets = os.path.join(repo_root, 'src/tests/issues.targets')
    
    print("Parsing issues.targets...")
    exclusions = parse_issues_targets(issues_targets)
    
    print(f"Found {len(exclusions)} exclusion items")
    
    # Process first few for testing
    dry_run = len(sys.argv) < 2 or sys.argv[1] != '--apply'
    
    if dry_run:
        print("\n=== DRY RUN MODE ===")
        print("Run with --apply to actually modify files\n")
    
    limit = 10 if dry_run else len(exclusions)
    
    for i, exclusion in enumerate(exclusions[:limit]):
        migrate_exclusion(exclusion, repo_root, dry_run)
    
    print(f"\nProcessed {min(limit, len(exclusions))} of {len(exclusions)} items")


if __name__ == '__main__':
    main()
