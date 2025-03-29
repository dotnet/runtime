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

    RuntimeInfoArchitecture IRuntimeInfo.GetTargetArchitecture()
    {
        if (_target.TryReadGlobal(Constants.Globals.Architecture, out uint? arch))
        {
            if (Enum.IsDefined(typeof(RuntimeInfoArchitecture), arch))
            {
                return (RuntimeInfoArchitecture)arch;
            }
        }

        return RuntimeInfoArchitecture.Unknown;
    }

    RuntimeInfoOperatingSystem IRuntimeInfo.GetTargetOperatingSystem()
    {
        if (_target.TryReadGlobal(Constants.Globals.OperatingSystem, out uint? os))
        {
            if (Enum.IsDefined(typeof(RuntimeInfoOperatingSystem), os))
            {
                return (RuntimeInfoOperatingSystem)os;
            }
        }

        return RuntimeInfoOperatingSystem.Unknown;
    }
}
