// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Microsoft.Diagnostics.DataContractReader.Data;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct RuntimeInfo_1 : IRuntimeInfo
{
    private const string ArchitectureGlobalName = "Architecture";
    private const string OperatingSystemGlobalName = "OperatingSystem";

    internal readonly Target _target;

    public RuntimeInfo_1(Target target)
    {
        _target = target;
    }

    RuntimeInfoArchitecture IRuntimeInfo.GetTargetArchitecture()
    {
        if (_target.TryReadGlobal<uint>(ArchitectureGlobalName, out uint? arch))
        {
            return (RuntimeInfoArchitecture)arch;
        }

        return RuntimeInfoArchitecture.Unknown;
    }

    RuntimeInfoOperatingSystem IRuntimeInfo.GetTargetOperatingSystem()
    {
        if (_target.TryReadGlobal<uint>(OperatingSystemGlobalName, out uint? os))
        {
            return (RuntimeInfoOperatingSystem)os;
        }

        return RuntimeInfoOperatingSystem.Unknown;
    }
}
