// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Marker attribute for documenting which runtime versions a test should skip.
/// Skip logic is evaluated by calling <see cref="DumpTestBase.SkipIfVersion"/>
/// at the start of the test method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnRuntimeVersionAttribute : Attribute
{
    public string Version { get; }
    public string Reason { get; }

    public SkipOnRuntimeVersionAttribute(string version, string reason)
    {
        Version = version;
        Reason = reason;
    }
}

/// <summary>
/// Marker attribute for documenting which target OS a test should skip.
/// Skip logic is evaluated by calling <see cref="DumpTestBase.SkipIfTargetOS"/>
/// at the start of the test method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnTargetOSAttribute : Attribute
{
    public string OperatingSystem { get; }
    public string Reason { get; }

    /// <param name="operatingSystem">The target OS name to skip: "Windows", "Unix", or "Browser".</param>
    /// <param name="reason">Explanation for why the test is skipped on this OS.</param>
    public SkipOnTargetOSAttribute(string operatingSystem, string reason)
    {
        OperatingSystem = operatingSystem;
        Reason = reason;
    }
}
