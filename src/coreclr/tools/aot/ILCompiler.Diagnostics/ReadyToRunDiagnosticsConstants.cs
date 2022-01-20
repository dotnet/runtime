// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.ReadyToRunDiagnosticsConstants;

public enum PerfMapPseudoRVAToken : uint
{
    OutputSignature = 0xFFFFFFFF,
    FormatVersion = 0xFFFFFFFE,
    TargetOS = 0xFFFFFFFD,
    TargetArchitecture = 0xFFFFFFFC,
    TargetABI = 0xFFFFFFFB,
}

public enum PerfMapArchitectureToken : uint
{
    Unknown = 0,
    ARM = 1,
    ARM64 = 2,
    X64 = 3,
    X86 = 4,
}

public enum PerfMapOSToken : uint
{
    Unknown = 0,
    Windows = 1,
    Linux = 2,
    OSX = 3,
    FreeBSD = 4,
    NetBSD = 5,
    SunOS = 6,
}

public enum PerfMapAbiToken : uint
{
    Unknown = 0,
    Default = 1,
    Armel = 2,
}
