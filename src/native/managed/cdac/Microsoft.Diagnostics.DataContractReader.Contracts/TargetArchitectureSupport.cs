// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal static class TargetArchitectureSupport
{
    private const string ArmSwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.Arm";
    private const string Arm64SwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.Arm64";
    private const string LoongArch64SwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.LoongArch64";
    private const string RiscV64SwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.RiscV64";
    private const string WasmSwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.Wasm";
    private const string X64SwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.X64";
    private const string X86SwitchName = "Microsoft.Diagnostics.DataContractReader.Architecture.X86";

    [FeatureSwitchDefinition(ArmSwitchName)]
    public static bool IsArmSupported { get; } = IsEnabled(ArmSwitchName);

    [FeatureSwitchDefinition(Arm64SwitchName)]
    public static bool IsArm64Supported { get; } = IsEnabled(Arm64SwitchName);

    [FeatureSwitchDefinition(LoongArch64SwitchName)]
    public static bool IsLoongArch64Supported { get; } = IsEnabled(LoongArch64SwitchName);

    [FeatureSwitchDefinition(RiscV64SwitchName)]
    public static bool IsRiscV64Supported { get; } = IsEnabled(RiscV64SwitchName);

    [FeatureSwitchDefinition(WasmSwitchName)]
    public static bool IsWasmSupported { get; } = IsEnabled(WasmSwitchName);

    [FeatureSwitchDefinition(X64SwitchName)]
    public static bool IsX64Supported { get; } = IsEnabled(X64SwitchName);

    [FeatureSwitchDefinition(X86SwitchName)]
    public static bool IsX86Supported { get; } = IsEnabled(X86SwitchName);

    private static bool IsEnabled(string switchName) =>
        !AppContext.TryGetSwitch(switchName, out bool isEnabled) || isEnabled;
}
