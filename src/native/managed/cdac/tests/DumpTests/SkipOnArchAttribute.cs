// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// <see cref="DumpTestBase.InitializeDumpTest"/> before each test runs.
/// Multiple attributes can be stacked on a single method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnArchAttribute : Attribute
{
    public string? Arch { get; }

    public string? IncludeOnly { get; set; }

    public string Reason { get; set; } = string.Empty;

    public SkipOnArchAttribute(string arch, string reason)
    {
        Arch = arch;
        Reason = reason;
    }

    public SkipOnArchAttribute()
    {
    }
}
