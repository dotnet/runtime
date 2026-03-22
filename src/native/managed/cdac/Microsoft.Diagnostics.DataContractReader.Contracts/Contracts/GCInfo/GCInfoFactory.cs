// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.Diagnostics.DataContractReader.Contracts.GCInfoHelpers;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

public sealed class GCInfoFactory : IContractFactory<IGCInfo>
{
    IGCInfo IContractFactory<IGCInfo>.CreateContract(Target target, int version)
    {
        RuntimeInfoArchitecture arch = target.Contracts.RuntimeInfo.GetTargetArchitecture();
        return (version, arch) switch
        {
            (1, RuntimeInfoArchitecture.X64) => new GCInfo_1<AMD64GCInfoTraits>(target),
            (1, RuntimeInfoArchitecture.Arm64) => new GCInfo_1<ARM64GCInfoTraits>(target),
            (1, RuntimeInfoArchitecture.Arm) => new GCInfo_1<ARMGCInfoTraits>(target),
            (1, RuntimeInfoArchitecture.LoongArch64) => new GCInfo_1<LoongArch64GCInfoTraits>(target),
            (1, RuntimeInfoArchitecture.RiscV64) => new GCInfo_1<RISCV64GCInfoTraits>(target),
            _ => default(GCInfo),
        };
    }
}
