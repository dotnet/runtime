// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Threading;

namespace Microsoft.Diagnostics.DataContractReader.Contracts;

internal sealed class RuntimeInfo_1 : IRuntimeInfo
{
    private readonly Target _target;

    private Lazy<RuntimeInfoArchitecture> _architecture;
    private Lazy<RuntimeInfoOperatingSystem> _operatingSystem;
    private Lazy<RuntimeInfoRuntimeFlavor> _runtimeFlavor;
    private Lazy<string> _runtimeProductVersion;
    private Lazy<uint> _recommendedReaderVersion;

    public RuntimeInfo_1(Target target)
    {
        _target = target;
        _architecture = new(ReadArchitecture);
        _operatingSystem = new(ReadOperatingSystem);
        _runtimeFlavor = new(ReadRuntimeFlavor);
        _runtimeProductVersion = new(() => _target.ReadGlobalString(Constants.Globals.RuntimeProductVersionString));
        _recommendedReaderVersion = new(ReadRecommendedReaderVersion);
    }

    public void Flush(FlushScope scope)
    {
        Volatile.Write(ref _architecture, new(ReadArchitecture));
        Volatile.Write(ref _operatingSystem, new(ReadOperatingSystem));
        Volatile.Write(ref _runtimeFlavor, new(ReadRuntimeFlavor));
        Volatile.Write(ref _runtimeProductVersion, new(() => _target.ReadGlobalString(Constants.Globals.RuntimeProductVersionString)));
        Volatile.Write(ref _recommendedReaderVersion, new(ReadRecommendedReaderVersion));
    }

    RuntimeInfoArchitecture IRuntimeInfo.GetTargetArchitecture()
        => Volatile.Read(ref _architecture).Value;

    RuntimeInfoOperatingSystem IRuntimeInfo.GetTargetOperatingSystem()
        => Volatile.Read(ref _operatingSystem).Value;

    RuntimeInfoRuntimeFlavor IRuntimeInfo.GetRuntimeFlavor()
        => Volatile.Read(ref _runtimeFlavor).Value;

    string IRuntimeInfo.GetRuntimeProductVersion()
        => Volatile.Read(ref _runtimeProductVersion).Value;

    uint IRuntimeInfo.GetCurrentReaderVersion() => 1;

    uint IRuntimeInfo.GetRecommendedReaderVersion()
        => Volatile.Read(ref _recommendedReaderVersion).Value;

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
