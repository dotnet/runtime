// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum RuntimeInfoArchitecture : uint
{
    Unknown = 0,
    X86,
    Arm32,
    X64,
    Arm64,
    LoongArch64,
    RISCV,
}

public enum RuntimeInfoOperatingSystem : uint
{
    Unknown = 0,
    Win,
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
