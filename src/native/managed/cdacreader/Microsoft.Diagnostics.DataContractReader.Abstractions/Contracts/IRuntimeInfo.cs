// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public enum RuntimeInfoArchitecture : uint
{
    Unknown = 0,
    X86,
    Arm32,
    Amd64,
    Arm64,
    LoongArch64,
    RISCV,
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
    public virtual RuntimeInfoArchitecture GetTargetArchitecture() => throw new NotImplementedException();
    public virtual RuntimeInfoOperatingSystem GetTargetOperatingSystem() => throw new NotImplementedException();
}

public readonly struct RuntimeInfo : IRuntimeInfo
{
    RuntimeInfoArchitecture IRuntimeInfo.GetTargetArchitecture() => RuntimeInfoArchitecture.Unknown;
    RuntimeInfoOperatingSystem IRuntimeInfo.GetTargetOperatingSystem() => RuntimeInfoOperatingSystem.Unknown;
}
