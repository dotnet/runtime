// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// When applied to a test method, causes the test to be skipped for
/// the specified runtime version. Checked by
/// <see cref="DumpTestBase.InitializeDumpTest"/> before each test runs.
/// Multiple attributes can be stacked on a single method.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnVersionAttribute : Attribute
{
    public string Version { get; }
    public string Reason { get; }

    public SkipOnVersionAttribute(string version, string reason)
    {
        Version = version;
        Reason = reason;
    }
}
