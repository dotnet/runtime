// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal struct RuntimeInfo_1 : IRuntimeInfo
{
    internal readonly Target _target;

    public RuntimeInfo_1(Target target)
    {
        _target = target;
    }

    readonly RuntimeInfoArchitecture IRuntimeInfo.GetTargetArchitecture()
    {
        if (_target.TryReadGlobalString(Constants.Globals.Architecture, out string? arch))
        {
            if (Enum.TryParse(arch, ignoreCase: true, out RuntimeInfoArchitecture parsedArch))
            {
                return parsedArch;
            }
        }

        return RuntimeInfoArchitecture.Unknown;
    }

    readonly RuntimeInfoOperatingSystem IRuntimeInfo.GetTargetOperatingSystem()
    {
        if (_target.TryReadGlobalString(Constants.Globals.OperatingSystem, out string? os))
        {
            if (Enum.TryParse(os, ignoreCase: true, out RuntimeInfoOperatingSystem parsedOS))
            {
                return parsedOS;
            }
        }

        return RuntimeInfoOperatingSystem.Unknown;
    }
}
