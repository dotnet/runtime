// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// When applied to a test method, controls whether the test runs based on
/// the OS where the dumps were generated. Checked by
/// <see cref="DumpTestBase.InitializeDumpTest"/> before each test runs.
/// Multiple attributes can be stacked on a single method.
///
/// There are two modes:
/// <list type="bullet">
///   <item><b>Exclude (default):</b> <c>[SkipOnOS("linux", "reason")]</c> — skip when
///     the dump OS matches.</item>
///   <item><b>Include-only:</b> <c>[SkipOnOS(IncludeOnly = "windows", Reason = "reason")]</c>
///     — skip when the dump OS does <em>not</em> match.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnOSAttribute : Attribute
{
    /// <summary>
    /// The OS to exclude. When set, the test is skipped if the dump OS matches.
    /// </summary>
    public string? Os { get; }

    /// <summary>
    /// When set, the test is skipped if the dump OS does <em>not</em> match this value.
    /// Mutually exclusive with <see cref="Os"/>.
    /// </summary>
    public string? IncludeOnly { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Creates an exclude-mode attribute: skip when the dump OS equals <paramref name="os"/>.
    /// </summary>
    public SkipOnOSAttribute(string os, string reason)
    {
        Os = os;
        Reason = reason;
    }

    /// <summary>
    /// Creates an attribute using named properties only (for include-only mode).
    /// Usage: <c>[SkipOnOS(IncludeOnly = "windows", Reason = "...")]</c>
    /// </summary>
    public SkipOnOSAttribute()
    {
    }
}
