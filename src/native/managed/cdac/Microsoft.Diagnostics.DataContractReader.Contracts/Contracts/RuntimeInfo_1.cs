// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class RuntimeInfo_1 : IRuntimeInfo
{
    private readonly Target _target;

    private RuntimeInfoArchitecture? _architecture;
    private RuntimeInfoOperatingSystem? _operatingSystem;
    private RuntimeInfoRuntimeFlavor? _runtimeFlavor;
    private uint? _recommendedReaderVersion;

    public RuntimeInfo_1(Target target)
    {
        _target = target;
    }

    public void Flush(FlushScope scope)
    {
        _architecture = null;
        _operatingSystem = null;
        _runtimeFlavor = null;
        _recommendedReaderVersion = null;
    }

    RuntimeInfoArchitecture IRuntimeInfo.GetTargetArchitecture()
        => _architecture ??= ReadArchitecture();

    RuntimeInfoOperatingSystem IRuntimeInfo.GetTargetOperatingSystem()
        => _operatingSystem ??= ReadOperatingSystem();

    RuntimeInfoRuntimeFlavor IRuntimeInfo.GetRuntimeFlavor()
        => _runtimeFlavor ??= ReadRuntimeFlavor();

    uint IRuntimeInfo.GetCurrentReaderVersion() => 1;

    uint IRuntimeInfo.GetRecommendedReaderVersion()
        => _recommendedReaderVersion ??= ReadRecommendedReaderVersion();

    private RuntimeInfoArchitecture ReadArchitecture()
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

    private RuntimeInfoOperatingSystem ReadOperatingSystem()
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

    private RuntimeInfoRuntimeFlavor ReadRuntimeFlavor()
    {
        if (_target.TryReadGlobalString(Constants.Globals.RuntimeFlavor, out string? flavor))
        {
            if (Enum.TryParse(flavor, ignoreCase: true, out RuntimeInfoRuntimeFlavor parsedFlavor))
            {
                return parsedFlavor;
            }
        }

        return RuntimeInfoRuntimeFlavor.Unknown;
    }

    private uint ReadRecommendedReaderVersion()
    {
        _target.TryReadGlobal(Constants.Globals.RecommendedReaderVersion, out uint? runtimeVersion);
        return runtimeVersion ?? 0;
    }
}
