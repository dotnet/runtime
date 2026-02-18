// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Apply to a test method to skip it for specific runtime versions.
/// Evaluated automatically by <see cref="CheckSkipOnRuntimeVersionAttribute"/>.
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
/// Apply to a test method to skip it when the dump's target OS matches.
/// The OS is determined from the dump via the cDAC RuntimeInfo contract.
/// Evaluated automatically by <see cref="CheckSkipOnRuntimeVersionAttribute"/>.
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
