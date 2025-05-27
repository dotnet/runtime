// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

// Values are similar to System.Runtime.InteropServices.Architecture
public enum RuntimeInfoArchitecture : uint
{
    Unknown = 0,
    X86,
    X64,
    Arm,
    Arm64,
    Wasm,
    S390x,
    LoongArch64,
    Armv6,
    Ppc64le,
    RiscV64,
}

public enum RuntimeInfoOperatingSystem : uint
{
    Unknown = 0,
    Windows,
    Unix,
}

public interface IRuntimeInfo : IContract
{
    static string IContract.Name { get; } = nameof(RuntimeInfo);
    RuntimeInfoArchitecture GetTargetArchitecture() => throw new NotImplementedException();
    RuntimeInfoOperatingSystem GetTargetOperatingSystem() => throw new NotImplementedException();
}

public readonly struct RuntimeInfo : IRuntimeInfo
{

}
