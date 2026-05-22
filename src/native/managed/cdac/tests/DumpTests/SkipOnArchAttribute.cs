// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Diagnostics.DataContractReader.DumpTests;

/// <summary>
/// When applied to a test method, controls whether the test runs based on
/// the architecture where the dumps were generated. Checked by
/// <see cref="DumpTestBase.InitializeDumpTest"/> before each test runs.
/// Multiple attributes can be stacked on a single method.
///
/// There are two modes:
/// <list type="bullet">
///   <item><b>Exclude (default):</b> <c>[SkipOnArch("x86", "reason")]</c> — skip when
///     the dump arch matches.</item>
///   <item><b>Include-only:</b> <c>[SkipOnArch(IncludeOnly = "x64", Reason = "reason")]</c>
///     — skip when the dump arch does <em>not</em> match.</item>
/// </list>
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class SkipOnArchAttribute : Attribute
{
    /// <summary>
    /// The arch to exclude. When set, the test is skipped if the dump arch matches.
    /// </summary>
    public string? Arch { get; }

    /// <summary>
    /// When set, the test is skipped if the dump arch does <em>not</em> match this value.
    /// Mutually exclusive with <see cref="Arch"/>.
    /// </summary>
    public string? IncludeOnly { get; set; }

    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// Creates an exclude-mode attribute: skip when the dump arch equals <paramref name="arch"/>.
    /// </summary>
    public SkipOnArchAttribute(string arch, string reason)
    {
        Arch = arch;
        Reason = reason;
    }

    /// <summary>
    /// Creates an attribute using named properties only (for include-only mode).
    /// Usage: <c>[SkipOnArch(IncludeOnly = "x64", Reason = "...")]</c>
    /// </summary>
    public SkipOnArchAttribute()
    {
    }
}
