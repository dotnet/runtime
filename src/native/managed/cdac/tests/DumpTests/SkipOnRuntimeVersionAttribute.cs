// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// Apply to a test method to skip it for specific runtime versions.
/// Throw <see cref="SkipTestException"/> at runtime when the condition matches.
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
